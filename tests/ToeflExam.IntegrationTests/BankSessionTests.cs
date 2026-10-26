using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ToeflExam.Application.ContentBank;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Application.Speaking;
using ToeflExam.Application.Writing;
using ToeflExam.Domain.Learning;
using ToeflExam.Infrastructure.ContentBank;
using ToeflExam.Infrastructure.Assessment;
using ToeflExam.Infrastructure.Persistence;

namespace ToeflExam.IntegrationTests;

public sealed class BankSessionTests(SqlPracticeFixture fixture) : IClassFixture<SqlPracticeFixture>
{
    private sealed class Clock : TimeProvider
    { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Scorer : IWritingScorer, ISpeakingScorer
    {
        public int Calls; public bool Fail; public bool MalformedOnce;
        public Task<ScoringResult> ScoreAsync(WritingQuestion q, string text, CancellationToken ct)
        { Calls++; if (MalformedOnce) { MalformedOnce = false; throw new WritingScoringException("The provider did not return a valid assessment."); } if (Fail) throw new WritingScoringException("temporary", true); return Task.FromResult(new ScoringResult(new(4, "Useful development", [], [], ["Be specific"], text), "test")); }
        public Task<SpeakingScoringResult> ScoreAsync(SpeakingQuestion q, SpeakingAudio audio, CancellationToken ct)
        { Calls++; return Task.FromResult(new SpeakingScoringResult(new(4, "A private transcript", "Content estimate", [], [], ["Check the transcript"]), "test")); }
    }
    private sealed class Coach : IExamCoach
    {
        public int Calls;
        public Task<ExamStudyPlan> CreateAsync(ExamReport report, CancellationToken ct)
        { Calls++; return Task.FromResult(new ExamStudyPlan([])); }
    }
    private async Task<string> User(ToeflExamDbContext db)
    { var user = new IdentityUser("bank-" + Guid.NewGuid()) { Id = Guid.NewGuid().ToString() }; db.Users.Add(user); await db.SaveChangesAsync(); return user.Id; }
    [SqlServerFact]
    public async Task Listening_is_sequential_private_idempotent_and_restartable_without_answers_in_sql()
    {
        using var scope = fixture.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ToeflExamDbContext>(); var service = scope.ServiceProvider.GetRequiredService<IBankSessionService>();
        var user = await User(db); var run = await service.StartAsync(user, "listening", "conversation", default);
        Assert.Equal(5, run.TotalActivities);
        await Assert.ThrowsAsync<PracticeNotFoundException>(() => service.StepAsync("other", run.Id, 1, default));
        await Assert.ThrowsAsync<PracticeConflictException>(() => service.StepAsync(user, run.Id, 2, default));
        await Assert.ThrowsAsync<PracticeConflictException>(() => service.RestartAsync(user, run.Id, default));
        for (int p = 1; p <= 5; p++)
        {
            var dto = await service.StepAsync(user, run.Id, p, default); Assert.Empty(dto.Question.Text); Assert.Null(dto.Feedback);
            var e = new EmbeddedContentBank().Practice.Single(x => x.Id == dto.Question.Id);
            var answer = new BankAnswer(Guid.NewGuid(), e.Questions.ToDictionary(x => x.Id, x => x.Answer), null, null, null);
            var result = await service.SubmitAsync(user, run.Id, p, answer, default); Assert.Equal(p, result.Progress.CompletedActivities); Assert.NotNull(result.Feedback);
            var repeat = await service.SubmitAsync(user, run.Id, p, answer with { Answers = [] }, default); Assert.Equal(p, repeat.Progress.CompletedActivities);
        }
        var next = await service.RestartAsync(user, run.Id, default); Assert.Equal(0, next.CompletionPercent); Assert.NotEqual(run.Id, next.Id);
        Assert.Equal(next.Id, (await service.RestartAsync(user, run.Id, default)).Id);
        var fields = db.Model.FindEntityType(typeof(BankStep))!.GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("Answers", fields); Assert.DoesNotContain("AudioBase64", fields); Assert.DoesNotContain("Text", fields);
    }
    [SqlServerFact]
    public async Task All_six_exam_paths_finish_with_exact_blueprints_and_deferred_private_reports()
    {
        var scorer = new Scorer(); var coach = new Coach();
        await using var app = fixture.Factory.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IWritingScorer>(); s.RemoveAll<ISpeakingScorer>(); s.AddSingleton<IWritingScorer>(scorer); s.AddSingleton<ISpeakingScorer>(scorer); s.RemoveAll<IExamCoach>(); s.AddSingleton<IExamCoach>(coach); }));
        foreach (var form in new[] { "mock-1", "mock-2", "mock-3" }) foreach (var upper in new[] { false, true })
        {
            using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ToeflExamDbContext>(); var service = scope.ServiceProvider.GetRequiredService<IBankSessionService>(); var user = await User(db);
            var run = await service.StartAsync(user, "exam", form, default); var before = scorer.Calls; int count = 0;
            await Assert.ThrowsAsync<PracticeConflictException>(() => service.ReportAsync(user, run.Id, default));
            while (!run.Completed)
            {
                var dto = await service.StepAsync(user, run.Id, run.NextPosition!.Value, default); var e = new EmbeddedContentBank().Exams.Single(x => x.Id == dto.Question.Id);
                if (run.CanReview)
                {
                    var responses = new Dictionary<int, BankAnswer>();
                    foreach (var position in run.AccessiblePositions)
                    {
                        var item = await service.StepAsync(user, run.Id, position, default);
                        var material = new EmbeddedContentBank().Exams.Single(x => x.Id == item.Question.Id);
                        count += Math.Max(1, item.Question.Questions.Length);
                        responses[position] = new(Guid.NewGuid(), upper ? item.Question.Questions.ToDictionary(x => x.Id, x => material.Questions.Single(q => q.Id == x.Id).Answer) : [], null, material.Order, null);
                    }
                    var submission = new BankPhaseSubmission(run.NextPosition!.Value, responses);
                    run = (await service.FinishPhaseAsync(user, run.Id, submission, default)).Progress;
                    Assert.Equal(run.NextPosition, (await service.FinishPhaseAsync(user, run.Id, submission, default)).Progress.NextPosition);
                    await Assert.ThrowsAsync<PracticeConflictException>(() => service.StepAsync(user, run.Id, dto.Position, default));
                    continue;
                }
                Assert.Single(run.AccessiblePositions);
                if (e.Questions.Length > 0) Assert.Single(dto.Question.Questions);
                count += Math.Max(1, dto.Question.Questions.Length);
                var answer = new BankAnswer(Guid.NewGuid(), upper ? dto.Question.Questions.ToDictionary(x => x.Id, x => e.Questions.Single(q => q.Id == x.Id).Answer) : [], e.Section == "writing" ? "My private answer with a relevant example." : null, e.Order, e.Section == "speaking" ? SpeakingProviderTests.Wave() : null);
                run = (await service.SubmitAsync(user, run.Id, dto.Position, answer, default)).Progress;
                Assert.Equal(run.NextPosition, (await service.SubmitAsync(user, run.Id, dto.Position, answer, default)).Progress.NextPosition);
                await Assert.ThrowsAsync<PracticeConflictException>(() => service.StepAsync(user, run.Id, dto.Position, default));
            }
            Assert.Equal(93, count); Assert.Equal(before, scorer.Calls); Assert.Equal(13, run.PendingEvaluations);
            // A completed report must be durable before the learner first opens it, including pending AI inputs.
            var archive = await db.LatestExamReports.SingleAsync(x => x.UserId == user && x.Form == form);
            Assert.Contains(StoredExamReport.Decode(archive.Payload).Answers.Values, x => x.AudioBase64 is not null);
            app.Services.GetRequiredService<VolatileExamStore>().Remove(user, run.Id);
            var saved = await db.BankSessions.Include(x => x.Steps).SingleAsync(x => x.Id == run.Id);
            foreach (var section in new[] { "reading", "listening" }) Assert.Equal(35, saved.Steps.Where(x => x.Section == section && !x.Excluded).Sum(x => x.Maximum));
            await Assert.ThrowsAsync<PracticeNotFoundException>(() => service.ReportAsync("other", run.Id, default));
            scorer.Fail = true; await Assert.ThrowsAsync<WritingScoringException>(() => service.EvaluateNextAsync(user, run.Id, default)); scorer.Fail = false;
            await Assert.ThrowsAsync<PracticeConflictException>(() => service.StudyPlanAsync(user, run.Id, default));
            scorer.MalformedOnce = true;
            var priorCalls = scorer.Calls;
            ExamReport report = await service.ReportAsync(user, run.Id, default);
            for (int i = 0; i < 13; i++) report = await service.EvaluateNextAsync(user, run.Id, default);
            Assert.Equal(priorCalls + 14, scorer.Calls);
            var coachCalls = coach.Calls;
            await Assert.ThrowsAsync<PracticeNotFoundException>(() => service.StudyPlanAsync("other", run.Id, default));
            var plan = await service.StudyPlanAsync(user, run.Id, default);
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(plan), System.Text.Json.JsonSerializer.Serialize(await service.StudyPlanAsync(user, run.Id, default)));
            Assert.Equal(coachCalls + 1, coach.Calls);
            Assert.All(report.Sections, s => Assert.Equal(0, s.Pending)); Assert.Equal(44, report.Sections.Single(x => x.Section == "speaking").Score);
            Assert.Equal(18, report.Sections.Single(x => x.Section == "writing").Score); Assert.False(report.IsOfficialScore); Assert.False(report.DetailsUnavailable);
            var calls = scorer.Calls; await service.EvaluateNextAsync(user, run.Id, default); Assert.Equal(calls, scorer.Calls);
            app.Services.GetRequiredService<VolatileExamStore>().Remove(user, run.Id);
            using var reopened = app.Services.CreateScope();
            var restoredService = reopened.ServiceProvider.GetRequiredService<IBankSessionService>();
            var restored = await restoredService.ReportAsync(user, run.Id, default);
            Assert.False(restored.DetailsUnavailable); Assert.Null(restored.ExpiresAt);
            var json = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(report.Details, json), System.Text.Json.JsonSerializer.Serialize(restored.Details, json));
            await restoredService.StudyPlanAsync(user, run.Id, default);
            Assert.Equal(coachCalls + 1, coach.Calls);
            Assert.All(StoredExamReport.Decode(archive.Payload).Answers.Values, a => Assert.Null(a.AudioBase64));
        }
    }
    [SqlServerFact]
    public async Task Concurrent_or_failed_submissions_cannot_replace_retained_answers()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ToeflExamDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IBankSessionService>();
        var store = scope.ServiceProvider.GetRequiredService<VolatileExamStore>();
        var user = await User(db); var run = await service.StartAsync(user, "exam", "mock-1", default);
        var state = store.Get(user, run.Id)!;
        await state.Gate.WaitAsync();
        try { await Assert.ThrowsAsync<PracticeConflictException>(() => service.SubmitAsync(user, run.Id, 1, new(Guid.NewGuid(), [], null, null, null), default)); }
        finally { state.Gate.Release(); }
        Assert.Empty(state.Answers);

        var dto = await service.StepAsync(user, run.Id, 1, default);
        var entry = new EmbeddedContentBank().Exams.Single(x => x.Id == dto.Question.Id);
        var answers = dto.Question.Questions.ToDictionary(q => q.Id, q => entry.Questions.Single(x => x.Id == q.Id).Answer);
        var answer = new BankAnswer(Guid.NewGuid(), answers, null, null, null);
        await Assert.ThrowsAsync<ArgumentException>(() => service.FinishPhaseAsync(user, run.Id, new(1, new() { [1] = answer with { Answers = new() { [999] = "invalid" } } }), default));
        Assert.Empty(state.Answers);
        db.ChangeTracker.Clear();
        EventHandler<SavingChangesEventArgs> failSave = (_, _) => throw new InvalidOperationException("Injected SQL save failure");
        db.SavingChanges += failSave;
        try { await Assert.ThrowsAsync<InvalidOperationException>(() => service.FinishPhaseAsync(user, run.Id, new(1, new() { [1] = answer }), default)); }
        finally { db.SavingChanges -= failSave; }
        Assert.Empty(state.Answers);
        db.ChangeTracker.Clear();
        Assert.Equal(0, (await service.ProgressAsync(user, run.Id, default)).CompletedActivities);
        await service.FinishPhaseAsync(user, run.Id, new(1, new() { [1] = answer }), default);
        Assert.Equal(run.AccessiblePositions.Length, state.Answers.Count);
        Assert.Equal(answers, state.Answers[1].Answers);
    }
    [SqlServerFact]
    public async Task Old_exam_is_preserved_and_start_opens_a_separate_forward_only_edition()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ToeflExamDbContext>(); var service = scope.ServiceProvider.GetRequiredService<IBankSessionService>();
        var user = await User(db); var old = await service.StartAsync(user, "exam", "mock-1", default);
        var stored = await db.BankSessions.SingleAsync(x => x.Id == old.Id); stored.Edition = 1; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<PracticeConflictException>(() => service.ProgressAsync(user, old.Id, default));
        var updated = await service.StartAsync(user, "exam", "mock-1", default);
        Assert.NotEqual(old.Id, updated.Id); Assert.Equal(93, updated.TotalActivities);
        Assert.True(updated.CanReview);
        Assert.Equal(11, updated.AccessiblePositions.Length);
        Assert.Equal(updated.Id, (await service.StartAsync(user, "exam", "mock-1", default)).Id);
        Assert.True(await db.BankSessions.AnyAsync(x => x.Id == old.Id && x.Edition == 1));
    }
    [SqlServerFact]
    public async Task Reading_drafts_are_revisable_only_in_the_current_module_and_do_not_reset_its_timer()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ToeflExamDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IBankSessionService>();
        var user = await User(db); var run = await service.StartAsync(user, "exam", "mock-1", default);
        Assert.True(run.CanReview);
        var first = await service.StepAsync(user, run.Id, 1, default);
        Assert.Equal(10, first.Question.Questions.Length);
        Assert.Contains("{{10}}", first.Question.Text);
        var responses = new Dictionary<int, BankAnswer>();
        foreach (var position in run.AccessiblePositions)
        {
            var step = await service.StepAsync(user, run.Id, position, default);
            var source = new EmbeddedContentBank().Exams.Single(e => e.Id == step.Question.Id);
            responses[position] = new(Guid.NewGuid(), step.Question.Questions.ToDictionary(q => q.Id, q => source.Questions.Single(x => x.Id == q.Id).Answer), null, null, null);
        }
        await service.SaveDraftAsync(user, run.Id, 1, responses[1] with { Answers = new() { [1] = "wrong" } }, default);
        await service.SaveDraftAsync(user, run.Id, 1, responses[1], default);
        Assert.Equal(responses[1].Answers, (await service.StepAsync(user, run.Id, 1, default)).Draft!.Answers);
        Assert.Null((await service.StepAsync(user, run.Id, 1, default)).Feedback);
        Assert.Equal(run.Deadline, (await service.ProgressAsync(user, run.Id, default)).Deadline);
        Assert.Equal(0, (await service.ProgressAsync(user, run.Id, default)).CompletedActivities);
        await Assert.ThrowsAsync<PracticeConflictException>(() => service.StepAsync(user, run.Id, 12, default));
        await Assert.ThrowsAsync<PracticeNotFoundException>(() => service.SaveDraftAsync("other", run.Id, 1, responses[1], default));
        var result = await service.FinishPhaseAsync(user, run.Id, new(1, responses), default);
        Assert.Equal(20, result.Progress.CompletedActivities);
        Assert.Equal("reading-upper", result.Progress.Phase);
        await Assert.ThrowsAsync<PracticeConflictException>(() => service.SaveDraftAsync(user, run.Id, 1, responses[1], default));
        Assert.Equal(result.Progress.NextPosition, (await service.FinishPhaseAsync(user, run.Id, new(1, responses), default)).Progress.NextPosition);
    }

    [SqlServerFact]
    public async Task Server_deadlines_survive_refresh_and_advance_all_expired_phases_without_extra_time()
    {
        var clock = new Clock();
        await using var app = fixture.Factory.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); }));
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ToeflExamDbContext>(); var user = await User(db); var service = scope.ServiceProvider.GetRequiredService<IBankSessionService>();
        var run = await service.StartAsync(user, "exam", "mock-1", default); var deadline = run.Deadline;
        Assert.Equal(deadline, (await service.ProgressAsync(user, run.Id, default)).Deadline);
        clock.Now = deadline!.Value.AddSeconds(3);
        var advanced = await service.ProgressAsync(user, run.Id, default);
        Assert.Equal(12, advanced.NextPosition);
        Assert.Equal(20, advanced.CompletedActivities);
        Assert.Equal(deadline.Value.AddMinutes(12), advanced.Deadline);
        await Assert.ThrowsAsync<PracticeConflictException>(() => service.StepAsync(user, run.Id, 1, default));
        await Assert.ThrowsAsync<PracticeConflictException>(() => service.StepAsync(user, run.Id, 3, default));
        clock.Now = clock.Now.AddHours(2); run = await service.ProgressAsync(user, run.Id, default); Assert.True(run.Completed); Assert.Equal(0, run.PendingEvaluations);
        var report = await service.ReportAsync(user, run.Id, default); Assert.All(report.Sections, s => Assert.Equal(0, s.Score));
        clock.Now = clock.Now.AddMinutes(31); Assert.False((await service.ReportAsync(user, run.Id, default)).DetailsUnavailable);
    }

    [SqlServerFact]
    public async Task New_completed_run_replaces_only_the_same_owners_same_exam_report()
    {
        var clock = new Clock();
        await using var app = fixture.Factory.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); }));
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ToeflExamDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IBankSessionService>();
        var user = await User(db); var other = await User(db);
        var first = await service.StartAsync(user, "exam", "mock-1", default);
        var otherForm = await service.StartAsync(user, "exam", "mock-2", default);
        var otherUser = await service.StartAsync(other, "exam", "mock-1", default);
        clock.Now = clock.Now.AddHours(2);
        foreach (var pair in new[] { (user, first.Id), (user, otherForm.Id), (other, otherUser.Id) })
            Assert.True((await service.ProgressAsync(pair.Item1, pair.Id, default)).Completed);
        var replacement = await service.RestartAsync(user, first.Id, default);
        Assert.False((await service.ReportAsync(user, first.Id, default)).DetailsUnavailable);
        Assert.True((await service.HistoryAsync(user, "exam", "mock-1", default)).Single(x => x.Id == first.Id).HasSavedReport);
        clock.Now = clock.Now.AddHours(2);
        Assert.True((await service.ProgressAsync(user, replacement.Id, default)).Completed);
        app.Services.GetRequiredService<VolatileExamStore>().Remove(user, replacement.Id);
        Assert.Equal(replacement.Id, (await service.ReportAsync(user, replacement.Id, default)).Id);
        await Assert.ThrowsAsync<PracticeConflictException>(() => service.ReportAsync(user, first.Id, default));
        await Assert.ThrowsAsync<PracticeConflictException>(() => service.EvaluateNextAsync(user, first.Id, default));
        await Assert.ThrowsAsync<PracticeNotFoundException>(() => service.ReportAsync(other, replacement.Id, default));
        Assert.Equal(1, await db.LatestExamReports.CountAsync(x => x.UserId == user && x.Form == "mock-1"));
        Assert.Equal(otherForm.Id, (await service.ReportAsync(user, otherForm.Id, default)).Id);
        Assert.Equal(otherUser.Id, (await service.ReportAsync(other, otherUser.Id, default)).Id);
        Assert.Equal(replacement.Id, (await service.HistoryAsync(user, "exam", "mock-1", default)).Single(x => x.HasSavedReport).Id);
    }
}
