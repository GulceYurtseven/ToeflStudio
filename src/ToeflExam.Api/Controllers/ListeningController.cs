using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ToeflExam.Application.Listening;

namespace ToeflExam.Api.Controllers;

[ApiController, Authorize(Policy = "StudentApi"), EnableRateLimiting("practice")]
[Route("api/listening")]
public sealed class ListeningController(ToeflExam.Application.ContentBank.IContentBank bank) : ControllerBase
{
    /// <summary>Combines available modules with the student's current completion progress.</summary>
    [HttpGet]
    public IActionResult Overview() => Ok(new { modules = ListeningCatalog.Modules().Select(m=>new{
        m.Module,m.Title,m.Description,m.Instructions,m.Skills,status="available",canStart=true,
        publishedActivities=bank.Practice.Count(x=>x.Section=="listening"&&x.Kind==m.Module)}), source = ListeningCatalog.Source });
}
