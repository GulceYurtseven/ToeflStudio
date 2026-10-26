using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ToeflExam.Infrastructure.Persistence;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Api.Controllers;

/// <summary>Issues Identity bearer tokens for password-authenticated identities.</summary>
[ApiController, Route("api/auth"), EnableRateLimiting("auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(16_384)]
public sealed class AuthController(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn,
    ToeflExamDbContext db, IOptionsMonitor<BearerTokenOptions> bearer, TimeProvider clock,
    IAntiforgery antiforgery) : ControllerBase
{
    public const string UsernamePattern = "^[A-Za-z][A-Za-z0-9_.-]{2,29}$";

    /// <summary>Creates a unique username and its profile atomically without altering legacy accounts.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(Registration request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var user = new IdentityUser(request.Username) { Email = request.Email.Trim() };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return Problem(statusCode: 400, detail: "Unable to create this account. Check your username, email and password, or sign in to your existing account.");
        db.Add(new StudentSettings { UserId = user.Id, DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username : request.DisplayName.Trim() });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok();
    }

    /// <summary>Resolves an email or username and applies Identity password hashing and lockout rules.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var identifier = request.Email.Trim(); // Keep the existing client contract; this field accepts either identifier.
        var user = identifier.Contains('@') ? await users.FindByEmailAsync(identifier) : await users.FindByNameAsync(identifier);
        if (user is null) return Unauthorized();
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded || await users.GetTwoFactorEnabledAsync(user)) return Unauthorized();
        return await IssueTokens(user);
    }

    /// <summary>Renews only unexpired refresh tickets whose security stamp is still valid.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        var ticket = bearer.Get(IdentityConstants.BearerScheme).RefreshTokenProtector.Unprotect(request.RefreshToken);
        if (ticket?.Properties.ExpiresUtc is not { } expires || expires <= clock.GetUtcNow()) return Unauthorized();
        var user = await signIn.ValidateSecurityStampAsync(ticket.Principal);
        return user is null || await users.IsLockedOutAsync(user) ? Unauthorized() : await IssueTokens(user);
    }

    /// <summary>Returns the authenticated profile so username login never changes the account's email identity.</summary>
    [Authorize(Policy = "StudentApi"), HttpGet("manage/info"), EnableRateLimiting("practice")]
    public async Task<IActionResult> Info(CancellationToken ct)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var profile = await db.Set<StudentSettings>().AsNoTracking().SingleOrDefaultAsync(x => x.UserId == user.Id, ct);
        return Ok(new { user.Email, username = user.UserName, displayName = profile?.DisplayName ?? "", isEmailConfirmed = user.EmailConfirmed });
    }

    /// <summary>Pairs a same-origin form token with an HttpOnly antiforgery cookie.</summary>
    [HttpGet("csrf"), EnableRateLimiting("public-metadata")]
    public IActionResult Csrf() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    /// <summary>Lets the framework serialize and protect access and refresh tokens without placing them in URLs.</summary>
    private async Task<IActionResult> IssueTokens(IdentityUser user)
    {
        signIn.AuthenticationScheme = IdentityConstants.BearerScheme;
        await signIn.SignInAsync(user, isPersistent: false);
        return new EmptyResult();
    }

    public sealed record Registration([Required, RegularExpression(UsernamePattern)] string Username,
        [Required, EmailAddress, MaxLength(256)] string Email, [Required, MinLength(10), MaxLength(256)] string Password,
        [StringLength(100, MinimumLength = 1)] string? DisplayName);
    public sealed record LoginRequest([Required, MaxLength(256)] string Email, [Required, MaxLength(256)] string Password);
    public sealed record RefreshRequest([Required, MaxLength(8192)] string RefreshToken);
}
