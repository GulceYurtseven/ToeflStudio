using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Infrastructure.Persistence;

namespace ToeflExam.IntegrationTests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TOEFL_TEST_SQL")))
            Skip = "Set TOEFL_TEST_SQL to run against an isolated SQL Server test database.";
    }
}

public sealed class SqlPracticeFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public string ConnectionString { get; private set; } = string.Empty;
    private string _databaseName = string.Empty;
    public ConcurrentQueue<string> Errors { get; } = new();

    public async Task InitializeAsync()
    {
        var server = Environment.GetEnvironmentVariable("TOEFL_TEST_SQL");
        if (string.IsNullOrWhiteSpace(server)) return;
        _databaseName = "ToeflExam_Tests_" + Guid.NewGuid().ToString("N");
        ConnectionString = new SqlConnectionStringBuilder(server) { InitialCatalog = _databaseName }.ConnectionString;
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging => logging.AddProvider(new TestErrorLoggerProvider(Errors)));
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:ToeflExam"] = ConnectionString }));
        });
    }

    private sealed class TestErrorLoggerProvider(ConcurrentQueue<string> errors) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new TestErrorLogger(errors);
        public void Dispose() { }
    }

    private sealed class TestErrorLogger(ConcurrentQueue<string> errors) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        { if (level >= LogLevel.Error && exception is not null) errors.Enqueue(exception.ToString()); }
    }

    public ToeflExamDbContext CreateContext() => new(new DbContextOptionsBuilder<ToeflExamDbContext>().UseSqlServer(ConnectionString).Options);

    // Exercise migration compatibility: existing students keep their original edition,
    // then a restart selects the expanded edition. New-content counts have separate tests.
    public async Task PinLegacyRun(HttpClient client,string section,string module)
    {
        var info=await client.GetFromJsonAsync<JsonElement>("/api/auth/manage/info");
        var email=info.GetProperty("email").GetString();
        await using var db=CreateContext();var user=await db.Users.SingleAsync(x=>x.Email==email);
        if(section=="writing")
        {
            var kind=ToeflExam.Application.Writing.WritingModules.Parse(module);
            var path=await db.WritingPaths.Include(x=>x.Steps).SingleAsync(x=>x.Kind==kind&&x.Version==1);
            db.WritingRuns.Add(new(user.Id,kind,path.Id,path.Steps.OrderBy(x=>x.Position).Select(x=>x.QuestionId),DateTimeOffset.UtcNow));
        }
        else
        {
            var kind=ToeflExam.Application.Speaking.SpeakingModules.Parse(module);
            var path=await db.SpeakingPaths.Include(x=>x.Steps).SingleAsync(x=>x.Kind==kind&&x.Version==1);
            db.SpeakingRuns.Add(new(user.Id,kind,path.Id,path.Steps.OrderBy(x=>x.Position).Select(x=>x.QuestionId),DateTimeOffset.UtcNow));
        }
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        if (string.IsNullOrEmpty(ConnectionString)) return;
        // Only the unique database created by this fixture may be dropped.
        if (!_databaseName.StartsWith("ToeflExam_Tests_", StringComparison.Ordinal)
            || new SqlConnectionStringBuilder(ConnectionString).InitialCatalog != _databaseName)
            throw new InvalidOperationException("Unsafe test cleanup target.");
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}

public sealed class BuildSentenceSqlTests(SqlPracticeFixture fixture) : IClassFixture<SqlPracticeFixture>
{
    private const string Root = "/api/practice/build-sentence";

    [SqlServerFact]
    public async Task Complete_student_journey_is_ordered_private_restartable_and_durable()
    {
        using var anonymous = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Root + "/path")).StatusCode);
        using var first = await Register();
        using var other = await Register();
        var path = await first.GetFromJsonAsync<PathDto>(Root + "/path");
        Assert.Equal(30, path!.TotalSteps);
        var run = await Read<RunDto>(await first.PostAsync(Root + "/runs", null));
        Assert.Equal(0m, run.CompletionPercent);
        var resumed = await Read<RunDto>(await first.PostAsync(Root + "/runs", null));
        Assert.Equal(run.Id, resumed.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"{Root}/runs/{run.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"{Root}/runs/{run.Id}/steps/1/submissions",
            new { submissionId = Guid.NewGuid(), tileIds = Array.Empty<int>() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"{Root}/runs/{run.Id}/restart", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await first.GetAsync($"{Root}/runs/{run.Id}/steps/2")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await first.PostAsync($"{Root}/runs/{run.Id}/restart", null)).StatusCode);
        var step = await first.GetFromJsonAsync<StepDto>($"{Root}/runs/{run.Id}/steps/1");
        Assert.Null(step!.Feedback);
        var json = JsonSerializer.Serialize(step);
        Assert.DoesNotContain("AcceptedOrders", json);
        Assert.DoesNotContain("Explanation", json);

        var bad = await first.PostAsJsonAsync($"{Root}/runs/{run.Id}/steps/1/submissions",
            new { submissionId = Guid.NewGuid(), tileIds = new[] { 999 } });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(0, (await first.GetFromJsonAsync<RunDto>($"{Root}/runs/{run.Id}"))!.CompletedSteps);
        var nullAnswer = await first.PostAsJsonAsync($"{Root}/runs/{run.Id}/steps/1/submissions", new { submissionId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.BadRequest, nullAnswer.StatusCode);

        for (var position = 1; position <= run.TotalSteps; position++)
        {
            var current = await first.GetFromJsonAsync<StepDto>($"{Root}/runs/{run.Id}/steps/{position}");
            await using var db = fixture.CreateContext();
            var row = await db.BuildSentenceQuestionVersions.SingleAsync(row => row.Id == current!.Question.Id);
            var correct = JsonSerializer.Deserialize<int[][]>(row.AcceptedOrdersJson)![0];
            var submissionId = Guid.NewGuid();
            // First question intentionally blank: completion must not imply accuracy.
            var response = await Read<SubmissionDto>(await first.PostAsJsonAsync($"{Root}/runs/{run.Id}/steps/{position}/submissions",
                new { submissionId, tileIds = position == 1 ? Array.Empty<int>() : correct }));
            Assert.Equal(decimal.Round(position * 100m / run.TotalSteps,2), response.Progress.CompletionPercent);
            Assert.Equal(position == 1 ? 0 : 1, response.Feedback.RawScore);
            Assert.NotEmpty(response.Feedback.Explanation);
            Assert.NotEmpty(response.Feedback.CorrectSentences);
            if (position == 1)
            {
                Assert.Equal("Before class, Maya visits the library.", response.Feedback.CorrectSentences.Single());
                var retry = await Read<SubmissionDto>(await first.PostAsJsonAsync($"{Root}/runs/{run.Id}/steps/1/submissions", new { submissionId, tileIds = correct }));
                Assert.Equal(0, retry.Feedback.RawScore);
                Assert.Equal(1, retry.Progress.CompletedSteps);
            }
        }

        var completed = await first.GetFromJsonAsync<RunDto>($"{Root}/runs/{run.Id}");
        Assert.True(completed!.IsCompleted);
        Assert.Equal(29, completed.CorrectAnswers);
        Assert.Null(completed.NextPosition);
        Assert.NotNull((await first.GetFromJsonAsync<StepDto>($"{Root}/runs/{run.Id}/steps/1"))!.Feedback);
        var next = await Read<RunDto>(await first.PostAsync($"{Root}/runs/{run.Id}/restart", null));
        Assert.NotEqual(run.Id, next.Id);
        Assert.Equal(0m, next.CompletionPercent);
        Assert.Equal(next.Id, (await Read<RunDto>(await first.PostAsync($"{Root}/runs/{run.Id}/restart", null))).Id);
        var history = await first.GetFromJsonAsync<RunDto[]>(Root + "/runs");
        Assert.Equal(2, history!.Length);
        Assert.Contains(history, item => item.Id == run.Id && item.CompletionPercent == 100);
        Assert.Empty((await other.GetFromJsonAsync<RunDto[]>(Root + "/runs"))!);
    }

    [SqlServerFact]
    public async Task Concurrent_submissions_only_complete_the_step_once()
    {
        using var client = await Register();
        var run = await Read<RunDto>(await client.PostAsync(Root + "/runs", null));
        var request = new { submissionId = Guid.NewGuid(), tileIds = Array.Empty<int>() };
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync($"{Root}/runs/{run.Id}/steps/1/submissions", request),
            client.PostAsJsonAsync($"{Root}/runs/{run.Id}/steps/1/submissions", request));
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.All(responses, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        var final = await Read<SubmissionDto>(await client.PostAsJsonAsync($"{Root}/runs/{run.Id}/steps/1/submissions", request));
        Assert.Equal(1, final.Progress.CompletedSteps);
    }

    [SqlServerFact]
    public async Task Migration_and_sql_constraints_are_applied()
    {
        await using var db = fixture.CreateContext();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(30, await db.BuildSentenceQuestionVersions.CountAsync());
        Assert.Equal(2, await db.BuildSentencePaths.CountAsync());
        await using var transaction = await db.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(
            "UPDATE content.BuildSentenceQuestionVersions SET TilesJson = N'not-json' WHERE VersionNumber = 1"));
        await transaction.RollbackAsync();
    }

    [SqlServerFact]
    public async Task Sql_rejects_invalid_versions_states_and_duplicate_versions()
    {
        string[] statements =
        [
            "UPDATE content.BuildSentenceQuestionVersions SET VersionNumber = 0 WHERE VersionNumber = 1",
            "UPDATE content.BuildSentenceQuestionVersions SET PublicationStatus = 99 WHERE VersionNumber = 1",
            "INSERT INTO content.BuildSentenceQuestionVersions (Id, QuestionId, VersionNumber, Prompt, PublicationStatus, TilesJson, ResponsePartsJson, AcceptedOrdersJson, Explanation) SELECT TOP(1) NEWID(), QuestionId, VersionNumber, Prompt, PublicationStatus, TilesJson, ResponsePartsJson, AcceptedOrdersJson, Explanation FROM content.BuildSentenceQuestionVersions",
            "UPDATE learning.BuildSentencePathSteps SET QuestionVersionId = NEWID() WHERE Position = 1"
        ];
        foreach (var statement in statements)
        {
            await using var db = fixture.CreateContext();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(statement));
            Assert.Contains(error.Number, new[] { 547, 2601, 2627 });
            await transaction.RollbackAsync();
        }
    }

    [SqlServerFact]
    public async Task Published_content_cannot_be_overwritten()
    {
        await using var db = fixture.CreateContext();
        var question = await db.BuildSentenceQuestionVersions.FirstAsync();
        question.Prompt = "Overwritten prompt";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task Retirement_blocks_new_runs_but_preserves_assigned_questions()
    {
        using var learner = await Register();
        var run = await Read<RunDto>(await learner.PostAsync(Root + "/runs", null));
        var assigned = await learner.GetFromJsonAsync<StepDto>($"{Root}/runs/{run.Id}/steps/1");
        await using var db = fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Use a single store transaction to test both reads without changing the shared committed seed.
        var row = await db.BuildSentenceQuestionVersions.SingleAsync(row => row.Id == assigned!.Question.Id);
        row.PublicationStatus = ToeflExam.Domain.Content.PublicationStatus.Retired;
        await db.SaveChangesAsync();
        var store = new SqlBuildSentencePracticeStore(db);
        var path = await store.GetCurrentPathAsync(default);
        Assert.False(await store.IsPathAvailableAsync(path!, default));
        var pinned = await store.GetQuestionAsync(row.Id, default);
        Assert.NotNull(pinned);
        Assert.Equal(1, pinned.Question.Score(pinned.AcceptedOrders[0]));
        await transaction.RollbackAsync();
    }

    private HttpClient Client() => fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    private async Task<HttpClient> Register()
    {
        var client = Client();
        var credentials = new { username = "student_" + Guid.NewGuid().ToString("N")[..20], email = $"learner-{Guid.NewGuid():N}@example.test", password = $"Practice!8-{Guid.NewGuid():N}" };
        (await client.PostAsJsonAsync("/api/auth/register", credentials)).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/api/auth/login?useCookies=false", credentials);
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());
        return client;
    }

    private async Task<T> Read<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}\n{string.Join('\n', fixture.Errors)}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
