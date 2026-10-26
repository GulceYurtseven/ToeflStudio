using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ToeflExam.Api.Controllers;

/// <summary>Accepts bounded, CSRF-protected contact forms without storing message content.</summary>
[ApiController, Route("api/contact"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ContactController(IContactMailSender sender, IOptions<ContactMailOptions> options, ILogger<ContactController> logger) : ControllerBase
{
    /// <summary>Reports delivery availability so the form can explain missing server configuration.</summary>
    [HttpGet]
    public IActionResult Configuration() => Ok(new { configured = options.Value.IsConfigured });

    /// <summary>Validates header-safe input and returns success only after SMTP accepts the message.</summary>
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("contact"), RequestSizeLimit(32_768)]
    public async Task<IActionResult> Send(ContactRequest request, CancellationToken ct)
    {
        if (request.Subject.Any(char.IsControl) || request.Email.Any(char.IsControl) || request.Subject.Trim().Length < 3 || request.Message.Trim().Length < 10)
            return Problem(statusCode: 400, detail: "Enter a valid subject, reply email and message.");
        if (!options.Value.IsConfigured)
        {
            logger.LogWarning("Contact delivery is disabled: configure ContactMail SMTP settings on the server.");
            return Problem(statusCode: 503, detail: "Your message could not be sent. Please try again later. Your draft has been kept.");
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            await sender.SendAsync(request.Subject.Trim(), request.Message.Trim(), request.Email.Trim(), timeout.Token);
            return Ok(new { message = "Your message has been sent. Thank you for helping us improve." });
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Only the exception type is logged: SMTP exceptions may contain personal data or server details.
            logger.LogWarning("Contact delivery failed ({FailureType}).", ex.GetType().Name);
            return Problem(statusCode: 503, detail: "Your message could not be sent. Please try again later. Your draft has been kept.");
        }
    }

    public sealed record ContactRequest([Required, StringLength(120, MinimumLength = 3)] string Subject,
        [Required, StringLength(5000, MinimumLength = 10)] string Message,
        [Required, EmailAddress, MaxLength(256)] string Email);
}
