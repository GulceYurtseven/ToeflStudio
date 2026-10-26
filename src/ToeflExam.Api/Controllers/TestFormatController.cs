using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ToeflExam.Application.Assessment;

namespace ToeflExam.Api.Controllers;

[ApiController, Authorize(Policy = "StudentApi"), EnableRateLimiting("practice")]
[Route("api/test-format")]
public sealed class TestFormatController : ControllerBase
{
    /// <summary>Returns documented 2026 format metadata for a supported TOEFL section.</summary>
    [HttpGet("{section}")]
    public ActionResult<SectionFormat> Get(string section)
        => TestFormat2026.Section(section) is { } format ? Ok(format) : NotFound();
}
