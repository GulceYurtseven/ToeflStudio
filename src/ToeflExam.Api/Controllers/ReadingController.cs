using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ToeflExam.Application.Reading;

namespace ToeflExam.Api.Controllers;

[ApiController, Authorize(Policy = "StudentApi"), EnableRateLimiting("practice")]
[Route("api/reading")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReadingController(ReadingPracticeService service) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    /// <summary>Combines available modules with the student's current completion progress.</summary>
    [HttpGet]
    public async Task<IActionResult> Overview(CancellationToken ct) => Ok(new { modules = await service.OverviewAsync(UserId, ct) });
    /// <summary>Starts or resumes the authenticated student's ordered practice run.</summary>
    [HttpPost("{module}/runs")]
    public Task<ReadingRunDto> Start(string module, CancellationToken ct) => service.StartAsync(UserId, module, ct);
    /// <summary>Returns the authenticated student's saved run summaries.</summary>
    [HttpGet("{module}/runs")]
    public Task<ReadingRunDto[]> History(string module, CancellationToken ct, int skip = 0, int take = 10) => service.HistoryAsync(UserId, module, skip, take, ct);
    /// <summary>Projects completed steps, scores and the next accessible activity.</summary>
    [HttpGet("{module}/runs/{id:guid}")]
    public Task<ReadingRunDto> Progress(string module, Guid id, CancellationToken ct) => service.ProgressAsync(UserId, module, id, ct);
    /// <summary>Returns the requested accessible activity for the authenticated student.</summary>
    [HttpGet("{module}/runs/{id:guid}/steps/{position:int}")]
    public Task<ReadingStepDto> Step(string module, Guid id, int position, CancellationToken ct) => service.StepAsync(UserId, module, id, position, ct);
    /// <summary>Validates and submits the current response through the practice service.</summary>
    [HttpPost("{module}/runs/{id:guid}/steps/{position:int}/submissions"), RequestSizeLimit(16384)]
    public Task<ReadingSubmission> Submit(string module, Guid id, int position, ReadingRequest request, CancellationToken ct)
        => service.SubmitAsync(UserId, module, id, position, request.SubmissionId, request.Answers, ct);
    /// <summary>Starts a new run after the previous path has been completed.</summary>
    [HttpPost("{module}/runs/{id:guid}/restart")]
    public Task<ReadingRunDto> Restart(string module, Guid id, CancellationToken ct) => service.RestartAsync(UserId, module, id, ct);
}
public sealed record ReadingRequest(Guid SubmissionId, Dictionary<int, string>? Answers);
