using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ToeflExam.Application.Speaking;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Domain.Speaking;
using ToeflExam.Infrastructure.Assessment;

namespace ToeflExam.Api.Controllers;

[ApiController]
[Route("api/practice/speaking/{module}")]
[Authorize(Policy = "StudentApi")]
[EnableRateLimiting("practice")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SpeakingPracticeController(SpeakingPracticeService service, QuestionAudioFiles audioFiles) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    /// <summary>Serves the audio belonging to an accessible question in this student's run.</summary>
    [HttpGet("runs/{runId:guid}/steps/{position:int}/audio")]
    public async Task<IActionResult> Audio(string module, Guid runId, int position, CancellationToken ct)
    {
        var file = await service.AudioFileAsync(UserId, SpeakingModules.Parse(module), runId, position, ct);
        if (!System.Text.RegularExpressions.Regex.IsMatch(file, @"^(?:[12]-[1-7]-v[1-9][0-9]*|[a-f0-9]{32})\.wav$"))
            throw new InvalidOperationException("Invalid speaking asset name.");
        var path = audioFiles.Resolve("Speaking", file);
        if (!System.IO.File.Exists(path)) return Problem(statusCode: 503, detail: "The question audio is missing from the server.");
        return PhysicalFile(path, "audio/wav", enableRangeProcessing: false);
    }
    /// <summary>Returns the published ordered path and its practice metadata.</summary>
    [HttpGet("path")]
    public Task<SpeakingPathDto> PathInfo(string module, CancellationToken ct) => service.PathAsync(SpeakingModules.Parse(module), ct);
    /// <summary>Starts or resumes the authenticated student's ordered practice run.</summary>
    [HttpPost("runs")]
    public Task<SpeakingRunDto> Start(string module, CancellationToken ct) => service.StartAsync(UserId, SpeakingModules.Parse(module), ct);
    /// <summary>Returns the authenticated student's saved run summaries.</summary>
    [HttpGet("runs")]
    public Task<SpeakingRunDto[]> History(string module, CancellationToken ct, int skip = 0, int take = 20)
        => service.HistoryAsync(UserId, SpeakingModules.Parse(module), skip, take, ct);
    /// <summary>Projects completed steps, scores and the next accessible activity.</summary>
    [HttpGet("runs/{runId:guid}")]
    public Task<SpeakingRunDto> Progress(string module, Guid runId, CancellationToken ct)
        => service.ProgressAsync(UserId, SpeakingModules.Parse(module), runId, ct);
    /// <summary>Returns the requested accessible activity for the authenticated student.</summary>
    [HttpGet("runs/{runId:guid}/steps/{position:int}")]
    public Task<SpeakingStepDto> Step(string module, Guid runId, int position, CancellationToken ct)
        => service.StepAsync(UserId, SpeakingModules.Parse(module), runId, position, ct);
    /// <summary>Validates and submits the current response through the practice service.</summary>
    [HttpPost("runs/{runId:guid}/steps/{position:int}/submissions")]
    [EnableRateLimiting("speaking-scoring")]
    [RequestSizeLimit(2100000)]
    public Task<SpeakingSubmission> Submit(string module, Guid runId, int position, SpeakingSubmitRequest request, CancellationToken ct)
        => service.SubmitAsync(UserId, SpeakingModules.Parse(module), runId, position, request.SubmissionId, request.AudioBase64!, request.Skip, ct);
    /// <summary>Starts a new run after the previous path has been completed.</summary>
    [HttpPost("runs/{runId:guid}/restart")]
    public Task<SpeakingRunDto> Restart(string module, Guid runId, CancellationToken ct)
        => service.RestartAsync(UserId, SpeakingModules.Parse(module), runId, ct);
    /// <summary>Returns the completed assessment report while its temporary detail is available.</summary>
    [HttpGet("runs/{runId:guid}/steps/{position:int}/report")]
    public async Task<IActionResult> Report(string module, Guid runId, int position, CancellationToken ct)
    {
        var report = await service.ReportAsync(UserId, SpeakingModules.Parse(module), runId, position, ct);
        return Ok(report);
    }
}

public sealed class SpeakingSubmitRequest
{
    public Guid SubmissionId { get; init; }
    [Required(AllowEmptyStrings = true), MaxLength(2000000)]
    public string? AudioBase64 { get; init; }
    public bool Skip { get; init; }
}

[ApiController, Route("api/speaking"), Authorize(Policy = "StudentApi"), EnableRateLimiting("practice")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SpeakingOverviewController(SpeakingPracticeService speaking, IOptions<GeminiOptions> gemini) : ControllerBase
{
    /// <summary>Combines available modules with the student's current completion progress.</summary>
    [HttpGet]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        var user = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var modules = new List<object>();
        foreach (var kind in Enum.GetValues<SpeakingTaskKind>())
        {
            var path = await speaking.PathAsync(kind, ct);
            var run = (await speaking.HistoryAsync(user, kind, 0, 1, ct)).FirstOrDefault();
            modules.Add(new { module = SpeakingModules.Slug(kind), title = path.Title,
                totalSteps = run?.TotalSteps ?? path.TotalSteps, completedSteps = run?.CompletedSteps ?? 0,
                completionPercent = run?.CompletionPercent ?? 0, runId = run?.Id });
        }
        return Ok(new { modules, configured = gemini.Value.IsConfigured, provider = "Gemini", model = gemini.Value.Model });
    }
}


