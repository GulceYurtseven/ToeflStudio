using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ToeflExam.Infrastructure.Persistence;
using ToeflExam.Infrastructure.Persistence.Entities;
namespace ToeflExam.Api.Controllers;

[ApiController, Route("api/settings"), Authorize(Policy = "StudentApi"), EnableRateLimiting("practice")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(16_384)]
public sealed class SettingsController(ToeflExamDbContext db, UserManager<IdentityUser> users) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    /// <summary>Loads the current account profile, study goals and membership capabilities.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await users.FindByIdAsync(UserId); if(user is null) return Unauthorized();
        var profile = await db.Set<StudentSettings>().AsNoTracking().SingleOrDefaultAsync(x => x.UserId == UserId, ct);
        return Ok(new { user.Email, username = user.UserName, hasPassword = await users.HasPasswordAsync(user), displayName = profile?.DisplayName ?? "", dailyPracticeMinutes = profile?.DailyPracticeMinutes ?? 20, targetTestDate = profile?.TargetTestDate, membership = "Free", membershipDescription = "Access to all four Studios and three Practice Exams." });
    }
    /// <summary>Updates only the authenticated student's display name and study goals.</summary>
    [HttpPost("profile")]
    public async Task<IActionResult> Profile(ProfileUpdate request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Any(char.IsControl)) return Problem(statusCode: 400, detail: "Enter a valid display name.");
        var profile = await db.Set<StudentSettings>().SingleOrDefaultAsync(x => x.UserId == UserId, ct);
        if(profile is null) { profile = new() { UserId = UserId }; db.Add(profile); }
        profile.DisplayName = request.DisplayName.Trim(); profile.DailyPracticeMinutes = request.DailyPracticeMinutes; profile.TargetTestDate = request.TargetTestDate;
        await db.SaveChangesAsync(ct); return NoContent();
    }
    /// <summary>Checks the current password and atomically updates credentials and the security stamp.</summary>
    [HttpPost("credentials"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Credentials(CredentialsUpdate request, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(UserId); if(user is null) return Unauthorized();
        if(!await users.CheckPasswordAsync(user, request.CurrentPassword)) return Problem(statusCode:400, detail:"The current password is incorrect.");
        if(string.IsNullOrWhiteSpace(request.NewEmail) && string.IsNullOrEmpty(request.NewPassword)) return Problem(statusCode:400, detail:"Enter a new email address or password.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if(!string.IsNullOrWhiteSpace(request.NewEmail) && !string.Equals(request.NewEmail.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var result = await users.SetEmailAsync(user, request.NewEmail.Trim()); if(!result.Succeeded) return Invalid(result);
        }
        if(!string.IsNullOrEmpty(request.NewPassword))
        {
            var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword); if(!result.Succeeded) return Invalid(result);
        }
        var stamp = await users.UpdateSecurityStampAsync(user); if(!stamp.Succeeded) return Invalid(stamp);
        await transaction.CommitAsync(ct); return Ok(new { requiresSignIn = true });
    }
    /// <summary>Creates a safe validation response for an unsuccessful operation.</summary>
    private IActionResult Invalid(IdentityResult result)
    { foreach(var error in result.Errors) ModelState.AddModelError(error.Code,error.Description); return ValidationProblem(ModelState); }
    public sealed record ProfileUpdate([Required, MaxLength(100)] string DisplayName, [Range(5,180)] int DailyPracticeMinutes, DateOnly? TargetTestDate);
    public sealed record CredentialsUpdate([Required, MaxLength(256)] string CurrentPassword, [EmailAddress, MaxLength(256)] string? NewEmail, [MinLength(10), MaxLength(256)] string? NewPassword);
}
