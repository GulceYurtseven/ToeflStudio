using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ToeflExam.IntegrationTests;

public sealed class SettingsTests(SqlPracticeFixture fixture) : IClassFixture<SqlPracticeFixture>
{
    private const string Password = "Settings!Test9834";
    private async Task<(HttpClient Client, string Email)> Student()
    {
        var email = $"settings-{Guid.NewGuid():N}@example.test";
        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.True((await users.CreateAsync(new IdentityUser(email) { Email = email }, Password)).Succeeded);
        var client = fixture.Factory.CreateClient();
        var result = await client.PostAsJsonAsync("/api/auth/login?useCookies=false", new { email, password = Password });
        result.EnsureSuccessStatusCode();
        var token = (await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, email);
    }

    [SqlServerFact]
    public async Task Profiles_are_private_validated_and_persist_without_client_selected_owner_or_plan()
    {
        using var anonymous = fixture.Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/settings")).StatusCode);
        var first = await Student(); var second = await Student();
        using var a = first.Client; using var b = second.Client;
        var response = await a.PostAsJsonAsync("/api/settings/profile", new { displayName = "  Learner  ", dailyPracticeMinutes = 35, targetTestDate = "2027-01-20", userId = second.Email, membership = "Premium" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var profile = await a.GetFromJsonAsync<JsonElement>("/api/settings");
        Assert.Equal("Learner", profile.GetProperty("displayName").GetString());
        Assert.Equal(35, profile.GetProperty("dailyPracticeMinutes").GetInt32());
        Assert.Equal("2027-01-20", profile.GetProperty("targetTestDate").GetString());
        Assert.Equal("Free", profile.GetProperty("membership").GetString());
        Assert.Equal("", (await b.GetFromJsonAsync<JsonElement>("/api/settings")).GetProperty("displayName").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsJsonAsync("/api/settings/profile", new { displayName = "Invalid", dailyPracticeMinutes = 181 })).StatusCode);
        Assert.Equal("Learner", (await a.GetFromJsonAsync<JsonElement>("/api/settings")).GetProperty("displayName").GetString());
    }

    [SqlServerFact]
    public async Task Credential_changes_require_password_and_rollback_duplicate_email()
    {
        var first = await Student(); var second = await Student(); using var a = first.Client; using var b = second.Client;
        var nextEmail = $"changed-{Guid.NewGuid():N}@example.test"; const string nextPassword = "Updated!Test9834";
        Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsJsonAsync("/api/settings/credentials", new { currentPassword = "incorrect", newEmail = nextEmail })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsJsonAsync("/api/settings/credentials", new { currentPassword = Password, newEmail = second.Email, newPassword = nextPassword })).StatusCode);
        Assert.Equal(first.Email, (await a.GetFromJsonAsync<JsonElement>("/api/settings")).GetProperty("email").GetString());
        var result = await a.PostAsJsonAsync("/api/settings/credentials", new { currentPassword = Password, newEmail = nextEmail, newPassword = nextPassword });
        result.EnsureSuccessStatusCode(); Assert.True((await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requiresSignIn").GetBoolean());
        using var login = fixture.Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await login.PostAsJsonAsync("/api/auth/login?useCookies=false", new { email = first.Email, password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await login.PostAsJsonAsync("/api/auth/login?useCookies=false", new { email = nextEmail, password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await login.PostAsJsonAsync("/api/auth/login?useCookies=false", new { email = nextEmail, password = nextPassword })).StatusCode);
    }
}
