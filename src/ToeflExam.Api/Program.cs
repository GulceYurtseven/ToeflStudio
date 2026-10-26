using ToeflExam.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.BearerToken;
using System.Security.Claims;
using System.Threading.RateLimiting;
using ToeflExam.Api;
using ToeflExam.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// MVC antiforgery filters are required for the anonymous contact and external-login forms.
builder.Services.AddControllersWithViews();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<PracticeExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
});
builder.Services.Configure<ContactMailOptions>(builder.Configuration.GetSection("ContactMail"));
builder.Services.AddScoped<IContactMailSender, SmtpContactMailSender>();
// Persist token/cookie encryption keys across production restarts when a protected directory is supplied.
var keyDirectory = builder.Configuration["DataProtection:KeyDirectory"];
var protection = builder.Services.AddDataProtection().SetApplicationName("ToeflExam");
if (!string.IsNullOrWhiteSpace(keyDirectory)) protection.PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 10;
    options.Lockout.MaxFailedAccessAttempts = 5;
}).AddEntityFrameworkStores<ToeflExamDbContext>();
builder.Services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme,
    options => options.BearerTokenExpiration = TimeSpan.FromMinutes(30));
builder.Services.AddAuthorizationBuilder().AddPolicy("StudentApi", policy =>
    policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme).RequireAuthenticatedUser().RequireClaim(ClaimTypes.NameIdentifier));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("public-metadata", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("contact", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 3, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("practice", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("writing-scoring", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 6, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("speaking-scoring", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing") && !app.Environment.IsEnvironment("UiTesting")) app.UseHsts();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; media-src 'self' blob:; frame-src 'none'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
    context.Response.Headers["Permissions-Policy"] = "microphone=(self), camera=()";
    // Block cross-site browser mutations even on anonymous JSON endpoints; bearer APIs never use ambient cookies.
    if (context.Request.Path.StartsWithSegments("/api") && !HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsHead(context.Request.Method) && context.Request.Headers["Sec-Fetch-Site"] == "cross-site")
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }
    if (context.Request.Path.StartsWithSegments("/api")) context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = context.File.Name == "index.html"
            ? "no-cache" : "public,max-age=3600";
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// AuthController exposes only the supported bearer flows, preventing unintended cookie-auth API access.

app.MapControllers();
// Liveness only; this endpoint does not probe SQL Server.
app.MapHealthChecks("/health/live");

app.Run();

public partial class Program { }
