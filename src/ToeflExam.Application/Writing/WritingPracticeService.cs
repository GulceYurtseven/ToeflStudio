using System.Text.RegularExpressions;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Domain.Learning;
using ToeflExam.Domain.Writing;

namespace ToeflExam.Application.Writing;

public sealed class WritingPracticeService(IWritingStore store, IWritingScorer scorer,
    IWritingReportCache reports, TimeProvider clock)
{
    public const string RubricVersion = "ets-rr25-12-writing-en-v3";
    /// <summary>Loads the current published path for the selected practice module.</summary>
    public async Task<WritingPathDto> PathAsync(WritingTaskKind kind, CancellationToken ct)
    {
        var path = await store.CurrentPathAsync(kind, ct) ?? throw new PracticeNotFoundException();
        return new(path.Id, WritingModules.Slug(kind), path.Version, path.Title, path.QuestionIds.Length);
    }
    /// <summary>Resumes an existing run or creates an ordered run for the selected content.</summary>
    public async Task<WritingRunDto> StartAsync(string user, WritingTaskKind kind, CancellationToken ct)
        => Map(await store.LatestAsync(user, kind, ct) ?? await CreateAsync(user, kind, null, ct));
    /// <summary>Builds an ordered run from the published path and persists its metadata.</summary>
    private async Task<WritingRun> CreateAsync(string user, WritingTaskKind kind, Guid? source, CancellationToken ct)
    {
        var path = await store.CurrentPathAsync(kind, ct) ?? throw new PracticeNotFoundException();
        if (!await store.AvailableAsync(path, ct)) throw new PracticeConflictException("This learning path is currently unavailable.");
        var run = new WritingRun(user, kind, path.Id, path.QuestionIds, clock.GetUtcNow(), source);
        store.Add(run); await store.SaveAsync(ct); return run;
    }
    /// <summary>Resolves or creates a new run linked to a completed source run.</summary>
    public async Task<WritingRunDto> RestartAsync(string user, WritingTaskKind kind, Guid id, CancellationToken ct)
    {
        var source = await RequireAsync(user, kind, id, ct);
        if (!source.IsCompleted) throw new PracticeRuleException("Complete the run before restarting.");
        var prior = await store.RestartAsync(user, id, ct);
        if (prior is not null) return Map(prior);
        if ((await store.LatestAsync(user, kind, ct))?.Id != id) throw new PracticeRuleException("Only the latest run can be restarted.");
        return Map(await CreateAsync(user, kind, id, ct));
    }
    /// <summary>Loads the student-owned run and returns its current progress.</summary>
    public async Task<WritingRunDto> ProgressAsync(string user, WritingTaskKind kind, Guid id, CancellationToken ct)
        => Map(await RequireAsync(user, kind, id, ct));
    /// <summary>Loads student-owned run summaries with the requested scope.</summary>
    public async Task<WritingRunDto[]> HistoryAsync(string user, WritingTaskKind kind, int skip, int take, CancellationToken ct)
    {
        if (skip < 0 || take is < 1 or > 50) throw new ArgumentException("skip must be nonnegative; take must be between 1 and 50.");
        return (await store.HistoryAsync(user, kind, skip, take, ct)).Select(Map).ToArray();
    }
    /// <summary>Loads an accessible practice step without disclosing future answer keys.</summary>
    public async Task<WritingStepDto> StepAsync(string user, WritingTaskKind kind, Guid id, int position, CancellationToken ct)
    {
        var step = (await RequireAsync(user, kind, id, ct)).AccessibleStep(position);
        var question = await store.QuestionAsync(step.QuestionId, ct) ?? throw new PracticeNotFoundException();
        return new(position, question, reports.Get(user, id, position));
    }
    /// <summary>Retrieves student-owned feedback while its in-memory retention period remains valid.</summary>
    public async Task<WritingReport> ReportAsync(string user, WritingTaskKind kind, Guid id, int position, CancellationToken ct)
    {
        var step = (await RequireAsync(user, kind, id, ct)).AccessibleStep(position);
        if (!step.Score.HasValue) throw new PracticeRuleException("This activity has not been assessed yet.");
        return reports.Get(user, id, position) ?? throw new WritingReportExpiredException();
    }
    /// <summary>Checks submission identity and run ownership before recording scores and progress.</summary>
    public async Task<WritingSubmission> SubmitAsync(string user, WritingTaskKind kind, Guid id, int position,
        Guid submissionId, string answer, CancellationToken ct)
    {
        if (submissionId == Guid.Empty || answer is null || answer.Length > 12000)
            throw new ArgumentException("A submission ID and a response of no more than 12000 characters are required.");
        var run = await RequireAsync(user, kind, id, ct);
        var step = run.AccessibleStep(position);
        if (step.Score.HasValue)
        {
            if (step.SubmissionId != submissionId) throw new PracticeRuleException("Bu etkinlik zaten teslim edildi.");
            var cached = reports.Get(user, id, position);
            return new(Map(run), cached, cached is null);
        }
        var question = await store.QuestionAsync(step.QuestionId, ct) ?? throw new PracticeNotFoundException();
        var token = run.BeginEvaluation(position, submissionId, clock.GetUtcNow());
        await store.SaveAsync(ct); // Commit claim before paid work; optimistic concurrency protects multiple servers.
        var count = Regex.Matches(answer, @"\S+").Count;
        ScoringResult result;
        try
        {
            result = string.IsNullOrWhiteSpace(answer)
                ? new ScoringResult(new(0, "The response is empty, so there is no text to assess.", [],
                    [], ["Address the task requirements in English."], ""), "local-empty-answer")
                : await scorer.ScoreAsync(question, answer, ct);
        }
        catch
        {
            // A failed provider call does not award zero or advance the learner.
            // If cleanup cannot reach SQL, the five-minute lease allows eventual retry.
            run.Release(position, token);
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try { await store.SaveAsync(cleanup.Token); }
            catch { /* Preserve the original error; lease expiry is the recovery mechanism. */ }
            throw;
        }
        var now = clock.GetUtcNow();
        run.Complete(position, token, result.Feedback.Score, count, result.Model, RubricVersion, now);
        await store.SaveAsync(ct);
        var report = new WritingReport(id, position, submissionId, WritingModules.Slug(kind), question,
            answer, count, result.Feedback, result.Model, RubricVersion, now, now.AddMinutes(30));
        reports.Put(user, report);
        return new(Map(run), report, false);
    }
    /// <summary>Loads a student-owned run or raises a not-found failure.</summary>
    private async Task<WritingRun> RequireAsync(string user, WritingTaskKind kind, Guid id, CancellationToken ct)
        => await store.RunAsync(user, kind, id, ct) ?? throw new PracticeNotFoundException();
    /// <summary>Maps a domain run to its progress and assessment summary.</summary>
    private WritingRunDto Map(WritingRun run) => new(run.Id, WritingModules.Slug(run.Kind), run.PathId,
        run.IsCompleted, run.CompletedCount, run.Steps.Count, run.CompletionPercent,
        run.CompletedCount == 0 ? null : decimal.Round((decimal)run.Steps.Where(s => s.Score.HasValue).Average(s => s.Score!.Value), 2),
        run.IsCompleted ? null : run.CompletedCount + 1,
        run.Steps.OrderBy(s => s.Position).Select(s => new WritingStepProgress(s.Position,
            s.Score.HasValue ? "completed" : s.LeaseUntil > clock.GetUtcNow() ? "evaluating" :
            s.Position == run.CompletedCount + 1 ? "available" : "locked", s.Score, s.WordCount, s.SubmissionId)).ToArray());
}
