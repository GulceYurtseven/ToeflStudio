using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ToeflExam.Application.Vocabulary;

namespace ToeflExam.Api.Controllers;

/// <summary>Serves the complete reviewed vocabulary bank to authenticated students.</summary>
[ApiController, Authorize(Policy = "StudentApi"), EnableRateLimiting("practice")]
[Route("api/vocabulary")]
public sealed class VocabularyController(IVocabularyCatalog vocabulary) : ControllerBase
{
    /// <summary>Returns every entry once; search and random practice then operate locally without extra requests.</summary>
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public ActionResult<VocabularyCatalog> Get() => Ok(vocabulary.Catalog);
}
