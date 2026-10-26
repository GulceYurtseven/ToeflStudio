using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ToeflExam.Application.Writing;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Domain.Writing;
using ToeflExam.Infrastructure.Assessment;

namespace ToeflExam.Api.Controllers;

[ApiController]
[Route("api/practice/writing/{module}")]
[Authorize(Policy = "StudentApi")]
[EnableRateLimiting("practice")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class WritingPracticeController(WritingPracticeService service) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    /// <summary>Returns the published ordered path and its practice metadata.</summary>
    [HttpGet("path")]
    public Task<WritingPathDto> PathInfo(string module, CancellationToken ct) => service.PathAsync(WritingModules.Parse(module), ct);
    /// <summary>Starts or resumes the authenticated student's ordered practice run.</summary>
    [HttpPost("runs")]
    public Task<WritingRunDto> Start(string module, CancellationToken ct) => service.StartAsync(UserId, WritingModules.Parse(module), ct);
    /// <summary>Returns the authenticated student's saved run summaries.</summary>
    [HttpGet("runs")]
    public Task<WritingRunDto[]> History(string module, CancellationToken ct, int skip = 0, int take = 20)
        => service.HistoryAsync(UserId, WritingModules.Parse(module), skip, take, ct);
    /// <summary>Projects completed steps, scores and the next accessible activity.</summary>
    [HttpGet("runs/{runId:guid}")]
    public Task<WritingRunDto> Progress(string module, Guid runId, CancellationToken ct)
        => service.ProgressAsync(UserId, WritingModules.Parse(module), runId, ct);
    /// <summary>Returns the requested accessible activity for the authenticated student.</summary>
    [HttpGet("runs/{runId:guid}/steps/{position:int}")]
    public Task<WritingStepDto> Step(string module, Guid runId, int position, CancellationToken ct)
        => service.StepAsync(UserId, WritingModules.Parse(module), runId, position, ct);
    /// <summary>Validates and submits the current response through the practice service.</summary>
    [HttpPost("runs/{runId:guid}/steps/{position:int}/submissions")]
    [EnableRateLimiting("writing-scoring")]
    [RequestSizeLimit(65536)]
    public Task<WritingSubmission> Submit(string module, Guid runId, int position, WritingSubmitRequest request, CancellationToken ct)
        => service.SubmitAsync(UserId, WritingModules.Parse(module), runId, position, request.SubmissionId, request.Answer!, ct);
    /// <summary>Starts a new run after the previous path has been completed.</summary>
    [HttpPost("runs/{runId:guid}/restart")]
    public Task<WritingRunDto> Restart(string module, Guid runId, CancellationToken ct)
        => service.RestartAsync(UserId, WritingModules.Parse(module), runId, ct);
    /// <summary>Returns the completed assessment report while its temporary detail is available.</summary>
    [HttpGet("runs/{runId:guid}/steps/{position:int}/report")]
    public async Task<IActionResult> Report(string module, Guid runId, int position, CancellationToken ct)
    {
        var report = await service.ReportAsync(UserId, WritingModules.Parse(module), runId, position, ct);
        return Ok(report);
    }
}

public sealed class WritingSubmitRequest
{
    public Guid SubmissionId { get; init; }
    [Required(AllowEmptyStrings = true), MaxLength(12000)]
    public string? Answer { get; init; }
}

[ApiController]
[Route("api/writing")]
[Authorize(Policy = "StudentApi")]
[EnableRateLimiting("practice")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class WritingOverviewController(WritingPracticeService writing, BuildSentencePracticeService sentences,
    IOptions<GeminiOptions> gemini) : ControllerBase
{
    /// <summary>Reports AI assessment availability without exposing provider secrets.</summary>
    [HttpGet("configuration")]
    public IActionResult Configuration() => Ok(new
    {
        provider = "Gemini", model = gemini.Value.Model, configured = gemini.Value.IsConfigured,
        timeoutSeconds = gemini.Value.TimeoutSeconds, liveConnectionVerified = false,
        missingSettings = new[] {
            string.IsNullOrWhiteSpace(gemini.Value.ApiKey) ? "Gemini:ApiKey" : null,
            !gemini.Value.ValidModel ? "Gemini:Model" : null,
            gemini.Value.TimeoutSeconds is < 10 or > 180 ? "Gemini:TimeoutSeconds" : null
        }.Where(s => s is not null).ToArray()
    });

    /// <summary>Combines available modules with the student's current completion progress.</summary>
    [HttpGet]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        var user = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        // One scoped DbContext: EF queries must be awaited sequentially.
        var sentencePath = await sentences.GetPathAsync(ct);
        var sentenceRun = (await sentences.ListRunsAsync(user, 0, 1, ct)).FirstOrDefault();
        var modules = new List<object> { new { module = "build-sentence", title = sentencePath.Title,
            totalSteps = sentenceRun?.TotalSteps ?? sentencePath.TotalSteps, completedSteps = sentenceRun?.CompletedSteps ?? 0,
            completionPercent = sentenceRun?.CompletionPercent ?? 0, runId = sentenceRun?.Id } };
        foreach (var kind in Enum.GetValues<WritingTaskKind>())
        {
            var path = await writing.PathAsync(kind, ct);
            var run = (await writing.HistoryAsync(user, kind, 0, 1, ct)).FirstOrDefault();
            modules.Add(new { module = WritingModules.Slug(kind), title = path.Title,
                totalSteps = run?.TotalSteps ?? path.TotalSteps, completedSteps = run?.CompletedSteps ?? 0,
                completionPercent = run?.CompletionPercent ?? 0, runId = run?.Id });
        }
        return Ok(new { modules });
    }
}
