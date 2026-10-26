using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ToeflExam.Api;

namespace ToeflExam.IntegrationTests;

/// <summary>Exercises account identity, cookie isolation, contact validation and OAuth without live providers.</summary>
public sealed class ProductionFeaturesTests(SqlPracticeFixture fixture) : IClassFixture<SqlPracticeFixture>
{
    [SqlServerFact]
    public async Task Username_and_email_login_resolve_the_same_profile_and_refresh_is_revoked_after_password_change()
    {
        using var client = Client(fixture.Factory);
        var username = "learner_" + Guid.NewGuid().ToString("N")[..16];
        var email = username + "@example.test";
        const string password = "Production!Test932";
        (await client.PostAsJsonAsync("/api/auth/register", new { username, email, password, displayName = "Alex Learner" })).EnsureSuccessStatusCode();
        foreach (var identifier in new[] { username.ToUpperInvariant(), email.ToUpperInvariant() })
        {
            var login = await client.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = identifier, password });
            login.EnsureSuccessStatusCode();
            Assert.False(login.Headers.TryGetValues("Set-Cookie", out _));
            var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());
            var info = await client.GetFromJsonAsync<JsonElement>("/api/auth/manage/info");
            Assert.Equal(email, info.GetProperty("email").GetString());
            Assert.Equal(username, info.GetProperty("username").GetString());
            Assert.Equal("Alex Learner", info.GetProperty("displayName").GetString());
        }
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var refresh = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString();
        var renewed = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh });
        renewed.EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/settings/credentials", new { currentPassword = password, newPassword = "Updated!Production932" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email = "' OR 1=1;--", password })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", new { username = "<script>alert(1)</script>", email = "other@example.test", password })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", new { username, email = "other@example.test", password })).StatusCode);
    }

    [Fact]
    public async Task Contact_requires_csrf_and_rejects_header_injection_then_sends_only_valid_plain_text()
    {
        var mail = new RecordingMail();
        await using var factory = ContactFactory(mail);
        using var client = Client(factory);
        var body = new { subject = "A useful suggestion", message = "<img src=x onerror=alert(1)> Please add more practice.", email = "student@example.test" };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/contact", body)).StatusCode);
        Assert.Empty(mail.Messages);
        await SetCsrf(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/contact", body with { subject = "Subject\r\nBcc: attacker@example.test" })).StatusCode);
        (await client.PostAsJsonAsync("/api/contact", body)).EnsureSuccessStatusCode();
        Assert.Single(mail.Messages);
        Assert.Equal(body.message, mail.Messages[0]);
        using var crossSite = new HttpRequestMessage(HttpMethod.Post, "/api/contact") { Content = JsonContent.Create(body) };
        crossSite.Headers.Add("Sec-Fetch-Site", "cross-site");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(crossSite)).StatusCode);
    }

    [Fact]
    public async Task Contact_reports_transport_failure_and_limits_repeated_requests()
    {
        var mail = new RecordingMail { Fail = true };
        await using var factory = ContactFactory(mail);
        using var client = Client(factory);
        await SetCsrf(client);
        var body = new { subject = "My feedback", message = "Please improve the activity instructions.", email = "student@example.test" };
        var failure = await client.PostAsJsonAsync("/api/contact", body);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode);
        Assert.DoesNotContain("fake SMTP secret", await failure.Content.ReadAsStringAsync());
        await client.PostAsJsonAsync("/api/contact", body);
        await client.PostAsJsonAsync("/api/contact", body);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/contact", body)).StatusCode);
    }

    [Fact]
    public async Task Removed_google_routes_do_not_issue_authentication_tokens()
    {
        await using var factory = ContactFactory(new RecordingMail());
        using var client = Client(factory);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/auth/google")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/auth/google/complete", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/auth/providers")).StatusCode);
    }

    private static HttpClient Client(WebApplicationFactory<Program> factory) => factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    private static async Task SetCsrf(HttpClient client)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
    }
    private static WebApplicationFactory<Program> ContactFactory(RecordingMail mail) => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:ToeflExam"] = "Server=localhost;Database=ContactTestNeverOpened;Integrated Security=True" }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IContactMailSender>(); services.AddSingleton<IContactMailSender>(mail);
            services.Configure<ContactMailOptions>(options => { options.Host = "smtp.example.test"; options.From = "admin@example.test"; options.Username = "admin"; options.Password = "fake"; });
        });
    });
    private sealed class RecordingMail : IContactMailSender
    {
        public bool Fail { get; init; }
        public List<string> Messages { get; } = [];
        public Task SendAsync(string subject, string message, string replyTo, CancellationToken ct)
        { if (Fail) throw new IOException("fake SMTP secret"); Messages.Add(message); return Task.CompletedTask; }
    }
}
