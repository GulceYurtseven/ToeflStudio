using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ToeflExam.Application.ContentBank;
using ToeflExam.Infrastructure.Persistence;

namespace ToeflExam.Api.Controllers;

[ApiController, Route("api/sessions"), Authorize(Policy = "StudentApi"), EnableRateLimiting("practice")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BankSessionsController(IBankSessionService service, IContentBank bank, ToeflExam.Infrastructure.Assessment.QuestionAudioFiles audioFiles) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    /// <summary>Returns the available practice paths and isolated exam forms.</summary>
    [HttpGet("catalog")]
    public IActionResult Catalog() => Ok(new
    {
        listening = BankSessionService.ListeningKinds.Select(kind => new { kind, activities = bank.Practice.Count(x => x.Section == "listening" && x.Kind == kind), questions = bank.Practice.Where(x => x.Section == "listening" && x.Kind == kind).Sum(x => x.QuestionCount) }),
        exams = new[] { "mock-1", "mock-2", "mock-3" }.Select((id, i) => new { id, title = $"TOEFL Practice Exam {i + 1}", questionsPerPath = 93, available = bank.Exams.Any(x => x.ExamId == id) }),
        limitations = BankSessionService.Limitations
    });
    /// <summary>Starts or resumes the authenticated student's ordered practice run.</summary>
    [HttpPost("{mode}/{form}")]
    public Task<BankProgress> Start(string mode, string form, CancellationToken ct) => service.StartAsync(UserId, mode, form, ct);
    /// <summary>Returns the authenticated student's saved run summaries.</summary>
    [HttpGet("{mode}/{form}/history")]
    public Task<BankProgress[]> History(string mode, string form, CancellationToken ct) => service.HistoryAsync(UserId, mode, form, ct);
    /// <summary>Starts a new run after the previous path has been completed.</summary>
    [HttpPost("{id:guid}/restart")]
    public Task<BankProgress> Restart(Guid id, CancellationToken ct) => service.RestartAsync(UserId, id, ct);
    /// <summary>Projects completed steps, scores and the next accessible activity.</summary>
    [HttpGet("{id:guid}")]
    public Task<BankProgress> Progress(Guid id, CancellationToken ct) => service.ProgressAsync(UserId, id, ct);
    /// <summary>Returns the requested accessible activity for the authenticated student.</summary>
    [HttpGet("{id:guid}/steps/{position:int}")]
    public Task<BankStepDto> Step(Guid id, int position, CancellationToken ct) => service.StepAsync(UserId, id, position, ct);
    /// <summary>Serves the audio belonging to an accessible question in this student's run.</summary>
    [HttpGet("{id:guid}/steps/{position:int}/audio")]
    public async Task<IActionResult> Audio(Guid id, int position, CancellationToken ct)
    {
        var name = await service.AudioAsync(UserId, id, position, ct);
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-f0-9]{32}\.wav$")) throw new InvalidOperationException("Invalid question audio.");
        var path = audioFiles.Resolve("Bank", name);
        if (!System.IO.File.Exists(path)) return Problem(statusCode: 503, detail: "Question audio is unavailable. Please try again later.");
        return PhysicalFile(path, "audio/wav", enableRangeProcessing: false);
    }
    /// <summary>Validates and submits the current response through the practice service.</summary>
    [HttpPost("{id:guid}/steps/{position:int}"), RequestSizeLimit(2100000)]
    public Task<BankResult> Submit(Guid id, int position, BankAnswer answer, CancellationToken ct) => service.SubmitAsync(UserId, id, position, answer, ct);
    /// <summary>Saves a draft inside the current reviewable module without finalizing it.</summary>
    [HttpPost("{id:guid}/steps/{position:int}/draft"), RequestSizeLimit(32_768)]
    public Task<BankProgress> SaveDraft(Guid id, int position, BankAnswer answer, CancellationToken ct) => service.SaveDraftAsync(UserId, id, position, answer, ct);
    /// <summary>Finalizes only the current reviewable module; previous and future modules cannot be edited.</summary>
    [HttpPost("{id:guid}/finish-phase"), RequestSizeLimit(262_144)]
    public Task<BankResult> FinishPhase(Guid id, BankPhaseSubmission submission, CancellationToken ct) => service.FinishPhaseAsync(UserId, id, submission, ct);
    /// <summary>Returns the authenticated owner's latest saved completed exam report.</summary>
    [HttpGet("{id:guid}/report")]
    public Task<ExamReport> Report(Guid id, CancellationToken ct) => service.ReportAsync(UserId, id, ct);
    /// <summary>Creates a cached AI study plan grounded in the student's completed exam.</summary>
    [HttpPost("{id:guid}/study-plan"), EnableRateLimiting("speaking-scoring")]
    public Task<ExamStudyPlan> StudyPlan(Guid id, CancellationToken ct) => service.StudyPlanAsync(UserId, id, ct);
    /// <summary>Processes the next pending AI assessment in a completed exam.</summary>
    [HttpPost("{id:guid}/evaluate"), EnableRateLimiting("speaking-scoring")]
    public Task<ExamReport> Evaluate(Guid id, CancellationToken ct) => service.EvaluateNextAsync(UserId, id, ct);
}
