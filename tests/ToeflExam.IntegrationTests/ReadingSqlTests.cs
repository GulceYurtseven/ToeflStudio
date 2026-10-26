using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ToeflExam.Application.Reading;
using ToeflExam.Domain.Content;
using ToeflExam.Domain.Reading;
using ToeflExam.Infrastructure.Persistence;

namespace ToeflExam.IntegrationTests;

public sealed class ReadingSqlTests(SqlPracticeFixture fixture) : IClassFixture<SqlPracticeFixture>
{
    [SqlServerFact] public Task Complete_words_journey() => Journey("complete-words");
    [SqlServerFact] public Task Daily_life_journey() => Journey("daily-life");
    [SqlServerFact] public Task Academic_journey() => Journey("academic-passage");
    private async Task Journey(string module)
    {
        using var client = await Register(); using var other = await Register();
        using var anon = fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/reading")).StatusCode);
        var root = "/api/reading/" + module + "/runs";
        var run = await Read<ReadingRunDto>(await client.PostAsync(root, null));
        Assert.Equal(module=="complete-words"?7:module=="daily-life"?10:9, run.TotalSteps);
        Assert.Equal(run.Id, (await Read<ReadingRunDto>(await client.PostAsync(root, null))).Id);
        var url = root + "/" + run.Id;
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(url + "/steps/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(url + "/steps/2")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(url + "/restart", null)).StatusCode);
        var pre = await client.GetStringAsync(url + "/steps/1");
        Assert.DoesNotContain("\"answer\":", pre, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("explanation", pre, StringComparison.OrdinalIgnoreCase);
        var bad = await client.PostAsJsonAsync(url + "/steps/1/submissions", new { submissionId = Guid.NewGuid(), answers = new Dictionary<int, string> { [999] = "x" } });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var stepDto = (await client.GetFromJsonAsync<ReadingStepDto>(url + "/steps/1"))!;
        var invalidBody = new { submissionId = Guid.NewGuid(), answers = new Dictionary<int, string> { [1] = new string('a', 81) } };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url + "/steps/1/submissions", invalidBody)).StatusCode);
        if (module != "complete-words") Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url + "/steps/1/submissions", new { submissionId = Guid.NewGuid(), answers = new Dictionary<int, string> { [1] = "unknown" } })).StatusCode);
        var total = 0;
        for (int n = 1; n <= run.TotalSteps; n++)
        {
            var step = (await client.GetFromJsonAsync<ReadingStepDto>(url + "/steps/" + n))!;
            await using var db = fixture.CreateContext();
            var content = (await new SqlReadingStore(db).ContentAsync(step.Question.Id, default))!;
            var answers = content.Items.ToDictionary(i => i.Id, i => module == "complete-words" ? " " + i.Answer.ToUpperInvariant() + " " : i.Answer);
            var id = Guid.NewGuid();
            if (n == 1) Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync(url + "/steps/1/submissions", new { submissionId = id, answers })).StatusCode);
            var result = await Read<ReadingSubmission>(await client.PostAsJsonAsync(url + $"/steps/{n}/submissions", new { submissionId = id, answers }));
            total += content.Items.Length;
            Assert.Equal(content.Items.Length, result.Feedback.Score); Assert.Equal(decimal.Round(n * 100m / run.TotalSteps,2), result.Progress.CompletionPercent);
            var retry = await Read<ReadingSubmission>(await client.PostAsJsonAsync(url + $"/steps/{n}/submissions", new { submissionId = id, answers = new Dictionary<int, string>() }));
            Assert.Equal(result.Feedback.Score, retry.Feedback.Score);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(url + $"/steps/{n}/submissions", new { submissionId = Guid.NewGuid(), answers })).StatusCode);
        }
        var completed = (await client.GetFromJsonAsync<ReadingRunDto>(url))!;
        Assert.True(completed.IsCompleted); Assert.Equal(total, completed.Score); Assert.Equal(total, completed.MaxScore);
        Assert.NotNull((await client.GetFromJsonAsync<ReadingStepDto>(url + "/steps/1"))!.Feedback);
        var next = await Read<ReadingRunDto>(await client.PostAsync(url + "/restart", null));
        Assert.NotEqual(run.Id, next.Id); Assert.Equal(0, next.CompletionPercent);
        Assert.Equal(next.Id, (await Read<ReadingRunDto>(await client.PostAsync(url + "/restart", null))).Id);
        Assert.Equal(2, (await client.GetFromJsonAsync<ReadingRunDto[]>(root))!.Length);
        await using var savedDb = fixture.CreateContext();
        var props = savedDb.Model.FindEntityType(typeof(ReadingStep))!.GetProperties().Select(p => p.Name);
        Assert.DoesNotContain("Answers", props); Assert.DoesNotContain("Answer", props);
    }
    [SqlServerFact]
    public async Task Seed_content_has_consistent_shapes_and_published_versions_are_immutable()
    {
        await using var db = fixture.CreateContext();
        var rows = await db.ReadingSets.ToArrayAsync(); Assert.Equal(46, rows.Length);
        foreach (var row in rows)
        {
            var items = JsonSerializer.Deserialize<ReadingItem[]>(row.ItemsJson)!;
            Assert.Equal(Enumerable.Range(1, items.Length), items.Select(i => i.Id));
            Assert.All(items, i => { Assert.NotEmpty(i.Answer); Assert.NotEmpty(i.Explanation); });
            if (row.Module == "complete-words")
            {
                Assert.Equal(10, items.Length); Assert.Equal(10, Regex.Matches(row.Text, @"\{\{\d+\}\}").Count);
                Assert.All(items, i => { Assert.Empty(i.Options); Assert.Equal(i.Answer.Length, i.MissingLetters); Assert.NotEmpty(i.Prefix); });
                Assert.InRange(row.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length, 70, 100);
            }
            else
            {
                Assert.InRange(items.Length, row.Module == "daily-life" ? 2 : 5, row.Module == "daily-life" ? 3 : 5);
                Assert.All(items, i => { Assert.Equal(4, i.Options.Length); Assert.Contains(i.Options, o => o.Id == i.Answer); });
                if (row.Module == "daily-life") Assert.InRange(Regex.Matches(row.Text, @"\S+").Count, 15, 150);
            }
        }
        rows[0].Text = "tampered";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
    [SqlServerFact]
    public async Task Academic_edition_preserves_old_questions_and_scores_sentence_insertion()
    {
        await using var db = fixture.CreateContext();
        var store = new SqlReadingStore(db);
        var path = (await store.PathAsync("academic-passage", default))!;
        Assert.Equal(3, path.Version); Assert.Equal(9, path.SetIds.Length);
        var original = (await store.ContentAsync(Guid.Parse("d3100000-0000-4000-8000-000000000003"), default))!;
        Assert.DoesNotContain("[A]", original.Text);
        var updated = (await store.ContentAsync(path.SetIds[2], default))!;
        Assert.All(new[] { "[A]", "[B]", "[C]", "[D]" }, marker => Assert.Contains(marker, updated.Text));
        Assert.Equal("B", updated.Items[4].Answer);
        Assert.DoesNotContain("Neither kind of evidence", updated.Text);

        using var client = await Register();
        var run = await Read<ReadingRunDto>(await client.PostAsync("/api/reading/academic-passage/runs", null));
        var root = $"/api/reading/academic-passage/runs/{run.Id}/steps/";
        for (var position = 1; position <= 2; position++)
            (await client.PostAsJsonAsync(root + position + "/submissions", new { submissionId = Guid.NewGuid(), answers = new Dictionary<int, string>() })).EnsureSuccessStatusCode();
        var question = await client.GetStringAsync(root + "3");
        Assert.Contains("Neither kind of evidence", question);
        Assert.DoesNotContain("correctAnswer", question);
        var result = await Read<ReadingSubmission>(await client.PostAsJsonAsync(root + "3/submissions",
            new { submissionId = Guid.NewGuid(), answers = new Dictionary<int, string> { [5] = "B" } }));
        Assert.Equal(1, result.Feedback.Score);
        Assert.Equal("Location [B]", result.Feedback.Items[4].CorrectAnswer);
    }

    [SqlServerFact]
    public async Task Concurrent_updates_conflict_and_retired_content_stays_pinned()
    {
        using var client = await Register();
        var run = await Read<ReadingRunDto>(await client.PostAsync("/api/reading/daily-life/runs", null));
        await using var first = fixture.CreateContext(); await using var second = fixture.CreateContext();
        var a = await first.ReadingRuns.Include(r => r.Steps).SingleAsync(r => r.Id == run.Id);
        var b = await second.ReadingRuns.Include(r => r.Steps).SingleAsync(r => r.Id == run.Id);
        a.Complete(1, Guid.NewGuid(), [1, 0], DateTimeOffset.UtcNow); b.Complete(1, Guid.NewGuid(), [0, 0], DateTimeOffset.UtcNow);
        await first.SaveChangesAsync(); await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        await using var transaction = await first.Database.BeginTransactionAsync();
        var row = await first.ReadingSets.SingleAsync(x => x.Id == a.Accessible(1).SetId);
        row.PublicationStatus = PublicationStatus.Retired; await first.SaveChangesAsync();
        Assert.NotNull(await new SqlReadingStore(first).ContentAsync(row.Id, default));
        await Assert.ThrowsAsync<ToeflExam.Application.Learning.BuildSentence.PracticeConflictException>(() => new SqlReadingStore(first).PathAsync("daily-life", default));
        await transaction.RollbackAsync();
    }
    private async Task<HttpClient> Register()
    {
        var client = fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var credentials = new { username = "student_" + Guid.NewGuid().ToString("N")[..20], email = $"reader-{Guid.NewGuid():N}@example.test", password = "Reading!8-" + Guid.NewGuid().ToString("N") };
        (await client.PostAsJsonAsync("/api/auth/register", credentials)).EnsureSuccessStatusCode();
        var token = await Read<JsonElement>(await client.PostAsJsonAsync("/api/auth/login?useCookies=false", credentials));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString()); return client;
    }
    [SqlServerFact]
    public async Task Listening_catalog_and_format_require_login_and_offer_published_practice()
    {
        using var anon = fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/listening")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/test-format/listening")).StatusCode);
        using var client = await Register();
        var catalog = await client.GetFromJsonAsync<JsonElement>("/api/listening");
        Assert.Equal(4, catalog.GetProperty("modules").GetArrayLength());
        foreach (var module in catalog.GetProperty("modules").EnumerateArray())
        {
            Assert.True(module.GetProperty("canStart").GetBoolean());
            Assert.True(module.GetProperty("publishedActivities").GetInt32()>0);
        }
        var format = await client.GetFromJsonAsync<JsonElement>("/api/test-format/listening");
        Assert.Equal(29, format.GetProperty("approximateBaseMinutes").GetInt32());
        Assert.Equal(JsonValueKind.Null, format.GetProperty("routingCutoff").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/test-format/unknown")).StatusCode);
    }
    private static async Task<T> Read<T>(HttpResponseMessage response)
    { Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}"); return (await response.Content.ReadFromJsonAsync<T>())!; }
}
