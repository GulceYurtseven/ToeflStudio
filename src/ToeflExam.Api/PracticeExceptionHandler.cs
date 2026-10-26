using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Domain.Learning;
using ToeflExam.Application.Writing;

namespace ToeflExam.Api;

public sealed class PracticeExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    /// <summary>Maps expected practice failures to safe HTTP problem responses without exposing internal exceptions.</summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            ToeflExam.Application.Speaking.SpeakingReportExpiredException => 410,
            ToeflExam.Application.Speaking.SpeakingScoringException scoring => scoring.Unavailable ? 503 : 502,
            WritingReportExpiredException => 410,
            WritingScoringException scoring => scoring.Unavailable ? 503 : 502,
            PracticeNotFoundException => 404,
            PracticeRuleException or PracticeConflictException => 409,
            ArgumentException => 400,
            _ => 0
        };
        if (status == 0) return false;
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Title = status switch
                { 404 => "Record not found", 409 => "Action conflicts with the current state", 410 => "Detailed feedback expired",
                  502 or 503 => "Assessment could not be completed", _ => "Invalid request" },
                Detail = exception.Message }
        });
    }
}
