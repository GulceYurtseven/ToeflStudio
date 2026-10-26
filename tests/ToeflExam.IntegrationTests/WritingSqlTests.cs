using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using ToeflExam.Application.Writing;
using ToeflExam.Infrastructure.Assessment;

namespace ToeflExam.IntegrationTests;

public sealed class WritingSqlTests(SqlPracticeFixture fixture) : IClassFixture<SqlPracticeFixture>
{
    [SqlServerFact]
    public Task Email_journey_is_private_ordered_and_restartable() => Journey("email");
    [SqlServerFact]
    public Task Discussion_journey_is_private_ordered_and_restartable() => Journey("academic-discussion");

    private async Task Journey(string module)
    {
        var scorer = new FakeScorer();
        await using var app = App(scorer);
        using var client = await Register(app);
        using var other = await Register(app);
        await fixture.PinLegacyRun(client,"writing",module);
        var root = "/api/practice/writing/" + module;
        using var anonymous = Client(app);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(root + "/path")).StatusCode);
        var run = await Read<WritingRunDto>(await client.PostAsync(root + "/runs", null));
        Assert.Equal(5, run.TotalSteps);
        Assert.Equal(run.Id, (await Read<WritingRunDto>(await client.PostAsync(root + "/runs", null))).Id);
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(root + $"/runs/{run.Id}/steps/2")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(root + $"/runs/{run.Id}/restart", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(root + $"/runs/{run.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync(root + $"/runs/{run.Id}/steps/1/submissions",
            new { submissionId = Guid.NewGuid(), answer = "Private answer" })).StatusCode);
        var wrongModule = module == "email" ? "academic-discussion" : "email";
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/practice/writing/{wrongModule}/runs/{run.Id}")).StatusCode);
        var first = await client.GetFromJsonAsync<WritingStepDto>(root + $"/runs/{run.Id}/steps/1");
        Assert.Null(first!.Report);
        Assert.Equal(module == "email" ? 0 : 2, first.Question.Posts.Length);
        for (var position = 1; position <= 5; position++)
        {
            var submissionId = Guid.NewGuid();
            var result = await Read<WritingSubmission>(await client.PostAsJsonAsync(root + $"/runs/{run.Id}/steps/{position}/submissions",
                new { submissionId, answer = position == 1 ? "" : "My original private response to this activity." }));
            Assert.Equal(position * 20m, result.Progress.CompletionPercent);
            Assert.Equal(position == 1 ? 0 : 4, result.Report!.Feedback.Score);
            Assert.False(result.Report.IsOfficialScore);
            if (position == 1)
            {
                var retry = await Read<WritingSubmission>(await client.PostAsJsonAsync(root + $"/runs/{run.Id}/steps/1/submissions",
                    new { submissionId, answer = "Changed payload must not change accepted score." }));
                Assert.Equal(0, retry.Report!.Feedback.Score);
                Assert.Equal(1, retry.Progress.CompletedSteps);
            }
        }
        Assert.Equal(4, scorer.Calls);
        var report = await client.GetAsync(root + $"/runs/{run.Id}/steps/5/report");
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Null(report.Content.Headers.ContentDisposition);
        Assert.Contains("My original private response", await report.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(root + $"/runs/{run.Id}/steps/5/report")).StatusCode);
        var progress = await client.GetFromJsonAsync<WritingRunDto>(root + $"/runs/{run.Id}");
        Assert.True(progress!.IsCompleted);
        Assert.Equal(3.2m, progress.AverageRawScore);
        var restarted = await Read<WritingRunDto>(await client.PostAsync(root + $"/runs/{run.Id}/restart", null));
        Assert.NotEqual(run.Id, restarted.Id);
        Assert.Equal(20,restarted.TotalSteps);
        Assert.Equal(0m, restarted.CompletionPercent);
        Assert.Equal(restarted.Id, (await Read<WritingRunDto>(await client.PostAsync(root + $"/runs/{run.Id}/restart", null))).Id);
        Assert.Equal(2, (await client.GetFromJsonAsync<WritingRunDto[]>(root + "/runs"))!.Length);
        var overview = await client.GetFromJsonAsync<JsonElement>("/api/writing");
        Assert.Equal(3, overview.GetProperty("modules").GetArrayLength());
        await using var db = fixture.CreateContext();
        var saved = await db.WritingRuns.Include(r => r.Steps).SingleAsync(r => r.Id == run.Id);
        Assert.Equal(5, saved.CompletedCount);
        Assert.All(saved.Steps, step => Assert.Null(step.LeaseToken));
        // No answer, feedback, quotation or report payload columns can reach persistence.
        var propertyNames = db.Model.FindEntityType(typeof(ToeflExam.Domain.Writing.WritingStep))!.GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain("Answer", propertyNames);
        Assert.DoesNotContain("Feedback", propertyNames);
    }

    [SqlServerFact]
    public async Task Missing_api_key_is_visible_without_secrets_and_does_not_advance_progress()
    {
        await using var app = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Gemini:ApiKey"] = "", ["Gemini:Model"] = "gemini-3.1-pro-preview" })));
        using var client = await Register(app);
        var settings = await client.GetFromJsonAsync<JsonElement>("/api/writing/configuration");
        Assert.False(settings.GetProperty("configured").GetBoolean());
        Assert.False(settings.TryGetProperty("apiKey", out _));
        Assert.Contains("Gemini:ApiKey", settings.GetProperty("missingSettings").ToString());
        const string root = "/api/practice/writing/email/runs";
        var run = await Read<WritingRunDto>(await client.PostAsync(root, null));
        var result = await client.PostAsJsonAsync($"{root}/{run.Id}/steps/1/submissions",
            new { submissionId = Guid.NewGuid(), answer = "An actual response needs a configured model." });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        var progress = await client.GetFromJsonAsync<WritingRunDto>($"{root}/{run.Id}");
        Assert.Equal(0, progress!.CompletedSteps);
        Assert.Equal("available", progress.Steps[0].Status);
    }

    [SqlServerFact]
    public async Task Writing_versions_are_immutable_and_retired_questions_remain_readable_for_assigned_runs()
    {
        await using (var db = fixture.CreateContext())
        {
            var question = await db.WritingQuestions.FirstAsync();
            question.Context = "Unauthorized overwrite";
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
        await using (var db = fixture.CreateContext())
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var question = await db.WritingQuestions.FirstAsync();
            question.PublicationStatus = ToeflExam.Domain.Content.PublicationStatus.Retired;
            await db.SaveChangesAsync();
            var store = new ToeflExam.Infrastructure.Persistence.SqlWritingStore(db);
            var path = await store.CurrentPathAsync(question.Kind, default);
            Assert.False(await store.AvailableAsync(path!, default));
            Assert.NotNull(await store.QuestionAsync(question.Id, default));
            await transaction.RollbackAsync();
        }
    }

    [SqlServerFact]
    public async Task Scoring_failure_releases_claim_and_same_submission_can_retry()
    {
        var scorer = new FakeScorer { Fail = true };
        await using var app = App(scorer);
        using var client = await Register(app);
        const string root = "/api/practice/writing/email/runs";
        var run = await Read<WritingRunDto>(await client.PostAsync(root, null));
        var body = new { submissionId = Guid.NewGuid(), answer = "A private email response." };
        var failed = await client.PostAsJsonAsync($"{root}/{run.Id}/steps/1/submissions", body);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        var current = await client.GetFromJsonAsync<WritingRunDto>($"{root}/{run.Id}");
        Assert.Equal(0, current!.CompletedSteps);
        Assert.Equal("available", current.Steps[0].Status);
        scorer.Fail = false;
        var result = await Read<WritingSubmission>(await client.PostAsJsonAsync($"{root}/{run.Id}/steps/1/submissions", body));
        Assert.Equal(1, result.Progress.CompletedSteps);
        Assert.Equal(2, scorer.Calls);
    }

    [SqlServerFact]
    public async Task Concurrent_requests_do_not_start_a_second_paid_evaluation()
    {
        var scorer = new FakeScorer { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        await using var app = App(scorer);
        using var client = await Register(app);
        const string root = "/api/practice/writing/email/runs";
        var run = await Read<WritingRunDto>(await client.PostAsync(root, null));
        var body = new { submissionId = Guid.NewGuid(), answer = "Original email." };
        var pending = client.PostAsJsonAsync($"{root}/{run.Id}/steps/1/submissions", body);
        try
        {
            await scorer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var current = await client.GetFromJsonAsync<WritingRunDto>($"{root}/{run.Id}");
            Assert.Equal("evaluating", current!.Steps[0].Status);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{root}/{run.Id}/steps/1/submissions", body)).StatusCode);
            Assert.Equal(1, scorer.Calls);
        }
        finally { scorer.Gate.SetResult(); }
        Assert.Equal(HttpStatusCode.OK, (await pending).StatusCode);
    }

    [SqlServerFact]
    public async Task Lost_ephemeral_report_preserves_score_and_does_not_trigger_rescoring()
    {
        var scorer = new FakeScorer();
        await using var app = App(scorer, new EmptyCache());
        using var client = await Register(app);
        const string root = "/api/practice/writing/email/runs";
        var run = await Read<WritingRunDto>(await client.PostAsync(root, null));
        var body = new { submissionId = Guid.NewGuid(), answer = "Original email." };
        Assert.NotNull((await Read<WritingSubmission>(await client.PostAsJsonAsync($"{root}/{run.Id}/steps/1/submissions", body))).Report);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"{root}/{run.Id}/steps/1/report")).StatusCode);
        var retry = await Read<WritingSubmission>(await client.PostAsJsonAsync($"{root}/{run.Id}/steps/1/submissions", body));
        Assert.True(retry.ReportUnavailable);
        Assert.Null(retry.Report);
        Assert.Equal(1, retry.Progress.CompletedSteps);
        Assert.Equal(1, scorer.Calls);
    }

    private WebApplicationFactory<Program> App(FakeScorer scorer, IWritingReportCache? cache = null)
        => fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IWritingScorer>();
            services.AddSingleton<IWritingScorer>(scorer);
            if (cache is not null) { services.RemoveAll<IWritingReportCache>(); services.AddSingleton(cache); }
        }));
    private static HttpClient Client(WebApplicationFactory<Program> app) => app.CreateClient(new()
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    private static async Task<HttpClient> Register(WebApplicationFactory<Program> app)
    {
        var client = Client(app);
        var credentials = new { username = "student_" + Guid.NewGuid().ToString("N")[..20], email = $"writer-{Guid.NewGuid():N}@example.test", password = $"Practice!8-{Guid.NewGuid():N}" };
        (await client.PostAsJsonAsync("/api/auth/register", credentials)).EnsureSuccessStatusCode();
        var tokens = await Read<JsonElement>(await client.PostAsJsonAsync("/api/auth/login?useCookies=false", credentials));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());
        return client;
    }
    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
    private sealed class FakeScorer : IWritingScorer
    {
        public int Calls;
        public bool Fail;
        public TaskCompletionSource? Gate;
        public TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<ScoringResult> ScoreAsync(WritingQuestion question, string answer, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls); Entered.TrySetResult();
            if (Gate is not null) await Gate.Task.WaitAsync(ct);
            if (Fail) throw new WritingScoringException("Temporary provider failure", true);
            return new(new(4, "Test rationale", ["Clear purpose"], [], ["Add a specific example."], answer), "test-double");
        }
    }
    private sealed class EmptyCache : IWritingReportCache
    {
        public void Put(string userId, WritingReport report) { }
        public WritingReport? Get(string userId, Guid runId, int position) => null;
    }
}
