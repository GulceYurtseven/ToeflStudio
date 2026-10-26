using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ToeflExam.Application.Learning.BuildSentence;

namespace ToeflExam.Api.Controllers;

[ApiController]
[Route("api/practice/build-sentence")]
[Authorize(Policy = "StudentApi")]
[EnableRateLimiting("practice")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BuildSentencePracticeController(BuildSentencePracticeService service) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated principal has no user identifier.");

    /// <summary>Returns the published ordered path and its practice metadata.</summary>
    [HttpGet("path")]
    public Task<PathDto> PathInfo(CancellationToken ct) => service.GetPathAsync(ct);

    /// <summary>Starts or resumes the authenticated student's ordered practice run.</summary>
    [HttpPost("runs")]
    public Task<RunDto> Start(CancellationToken ct) => service.StartAsync(UserId, ct);

    /// <summary>Returns the authenticated student's saved run summaries.</summary>
    [HttpGet("runs")]
    public Task<IReadOnlyList<RunDto>> History(CancellationToken ct, [FromQuery] int skip = 0, [FromQuery] int take = 20)
        => service.ListRunsAsync(UserId, skip, take, ct);

    /// <summary>Projects completed steps, scores and the next accessible activity.</summary>
    [HttpGet("runs/{runId:guid}")]
    public Task<RunDto> Progress(Guid runId, CancellationToken ct) => service.GetRunAsync(UserId, runId, ct);

    /// <summary>Returns the requested accessible activity for the authenticated student.</summary>
    [HttpGet("runs/{runId:guid}/steps/{position:int}")]
    public Task<StepDto> Step(Guid runId, int position, CancellationToken ct) => service.GetStepAsync(UserId, runId, position, ct);

    /// <summary>Validates and submits the current response through the practice service.</summary>
    [HttpPost("runs/{runId:guid}/steps/{position:int}/submissions")]
    [RequestSizeLimit(4096)]
    public Task<SubmissionDto> Submit(Guid runId, int position, SubmitBuildSentenceRequest request, CancellationToken ct)
        => service.SubmitAsync(UserId, runId, position, request.SubmissionId, request.TileIds!, ct);

    /// <summary>Starts a new run after the previous path has been completed.</summary>
    [HttpPost("runs/{runId:guid}/restart")]
    public Task<RunDto> Restart(Guid runId, CancellationToken ct) => service.RestartAsync(UserId, runId, ct);
}

public sealed class SubmitBuildSentenceRequest
{
    public Guid SubmissionId { get; init; }
    [Required, MaxLength(50)]
    public int[]? TileIds { get; init; }
}
