using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Domain.Learning;
using ToeflExam.Domain.Speaking;

namespace ToeflExam.Application.Speaking;

public sealed class SpeakingPracticeService(ISpeakingStore store, ISpeakingScorer scorer,
    ISpeakingReportCache reports, TimeProvider clock)
{
    public const string RubricVersion = "ets-rr25-12-speaking-text-en-v3";
    /// <summary>Loads the current published path for the selected practice module.</summary>
    public async Task<SpeakingPathDto> PathAsync(SpeakingTaskKind kind, CancellationToken ct)
    {
        var path = await store.CurrentPathAsync(kind, ct) ?? throw new PracticeNotFoundException();
        return new(path.Id, SpeakingModules.Slug(kind), path.Version, path.Title, path.QuestionIds.Length);
    }
    /// <summary>Resumes an existing run or creates an ordered run for the selected content.</summary>
    public async Task<SpeakingRunDto> StartAsync(string user, SpeakingTaskKind kind, CancellationToken ct)
        => Map(await store.LatestAsync(user, kind, ct) ?? await CreateAsync(user, kind, null, ct));
    /// <summary>Builds an ordered run from the published path and persists its metadata.</summary>
    private async Task<SpeakingRun> CreateAsync(string user, SpeakingTaskKind kind, Guid? source, CancellationToken ct)
    {
        var path = await store.CurrentPathAsync(kind, ct) ?? throw new PracticeNotFoundException();
        if (!await store.AvailableAsync(path, ct)) throw new PracticeConflictException("This learning path is currently unavailable.");
        var run = new SpeakingRun(user, kind, path.Id, path.QuestionIds, clock.GetUtcNow(), source);
        store.Add(run); await store.SaveAsync(ct); return run;
    }
    /// <summary>Resolves or creates a new run linked to a completed source run.</summary>
    public async Task<SpeakingRunDto> RestartAsync(string user, SpeakingTaskKind kind, Guid id, CancellationToken ct)
    {
        var source = await RequireAsync(user, kind, id, ct);
        if (!source.IsCompleted) throw new PracticeRuleException("Complete the run before restarting.");
        var prior = await store.RestartAsync(user, id, ct);
        if (prior is not null) return Map(prior);
        if ((await store.LatestAsync(user, kind, ct))?.Id != id) throw new PracticeRuleException("Only the latest run can be restarted.");
        return Map(await CreateAsync(user, kind, id, ct));
    }
    /// <summary>Loads the student-owned run and returns its current progress.</summary>
    public async Task<SpeakingRunDto> ProgressAsync(string user, SpeakingTaskKind kind, Guid id, CancellationToken ct)
        => Map(await RequireAsync(user, kind, id, ct));
    /// <summary>Loads student-owned run summaries with the requested scope.</summary>
    public async Task<SpeakingRunDto[]> HistoryAsync(string user, SpeakingTaskKind kind, int skip, int take, CancellationToken ct)
    {
        if (skip < 0 || take is < 1 or > 50) throw new ArgumentException("skip must be nonnegative; take must be between 1 and 50.");
        return (await store.HistoryAsync(user, kind, skip, take, ct)).Select(Map).ToArray();
    }
    /// <summary>Loads an accessible practice step without disclosing future answer keys.</summary>
    public async Task<SpeakingStepDto> StepAsync(string user, SpeakingTaskKind kind, Guid id, int position, CancellationToken ct)
    {
        var step = (await RequireAsync(user, kind, id, ct)).AccessibleStep(position);
        var question = await store.QuestionAsync(step.QuestionId, ct) ?? throw new PracticeNotFoundException();
        return new(position, new(question.Id, question.Title, question.Instructions, question.Context, question.ResponseSeconds, question.ScenePosition), reports.Get(user, id, position));
    }
    /// <summary>Retrieves student-owned feedback while its in-memory retention period remains valid.</summary>
    public async Task<SpeakingReport> ReportAsync(string user, SpeakingTaskKind kind, Guid id, int position, CancellationToken ct)
    {
        var step = (await RequireAsync(user, kind, id, ct)).AccessibleStep(position);
        if (!step.Score.HasValue) throw new PracticeRuleException("This activity has not been assessed yet.");
        return reports.Get(user, id, position) ?? throw new SpeakingReportExpiredException();
    }
    /// <summary>Resolves the audio asset only after checking the requested run and step.</summary>
    public async Task<string> AudioFileAsync(string user, SpeakingTaskKind kind, Guid id, int position, CancellationToken ct)
    {
        var step = (await RequireAsync(user, kind, id, ct)).AccessibleStep(position);
        return (await store.QuestionAsync(step.QuestionId, ct) ?? throw new PracticeNotFoundException()).AudioFile;
    }
    /// <summary>Checks submission identity and run ownership before recording scores and progress.</summary>
    public async Task<SpeakingSubmission> SubmitAsync(string user, SpeakingTaskKind kind, Guid id, int position,
        Guid submissionId, string audioBase64, bool skip, CancellationToken ct)
    {
        if (submissionId == Guid.Empty || audioBase64 is null || (skip && audioBase64.Length != 0))
            throw new ArgumentException("A submission ID is required; skipped questions must not include audio.");
        var run = await RequireAsync(user, kind, id, ct);
        var step = run.AccessibleStep(position);
        if (step.Score.HasValue)
        {
            if (step.SubmissionId != submissionId) throw new PracticeRuleException("Bu etkinlik zaten teslim edildi.");
            var cached = reports.Get(user, id, position);
            return new(Map(run), cached, cached is null);
        }
        var question = await store.QuestionAsync(step.QuestionId, ct) ?? throw new PracticeNotFoundException();
        var audio = skip ? null : SpeakingAudio.Parse(audioBase64, question.ResponseSeconds);
        var token = run.BeginEvaluation(position, submissionId, clock.GetUtcNow());
        await store.SaveAsync(ct); // Commit claim before paid work; optimistic concurrency protects multiple servers.
        var duration = audio?.DurationMilliseconds ?? 0;
        SpeakingScoringResult result;
        try
        {
            result = skip
                ? new SpeakingScoringResult(new(0, "", "No response was provided for this task.", [], [], ["Listen to the recording and try responding in English."]), "local-skipped")
                : await scorer.ScoreAsync(question, audio!, ct);
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
        run.Complete(position, token, result.Feedback.Score, duration, result.Model, RubricVersion, now);
        await store.SaveAsync(ct);
        var report = new SpeakingReport(id, position, submissionId, SpeakingModules.Slug(kind), question.Title,
            question.Prompt, duration, result.Feedback, result.Model, RubricVersion, now, now.AddMinutes(30));
        reports.Put(user, report);
        return new(Map(run), report, false);
    }
    /// <summary>Loads a student-owned run or raises a not-found failure.</summary>
    private async Task<SpeakingRun> RequireAsync(string user, SpeakingTaskKind kind, Guid id, CancellationToken ct)
        => await store.RunAsync(user, kind, id, ct) ?? throw new PracticeNotFoundException();
    /// <summary>Maps a domain run to its progress and assessment summary.</summary>
    private SpeakingRunDto Map(SpeakingRun run) => new(run.Id, SpeakingModules.Slug(run.Kind), run.PathId,
        run.IsCompleted, run.CompletedCount, run.Steps.Count, run.CompletionPercent,
        run.CompletedCount == 0 ? null : decimal.Round((decimal)run.Steps.Where(s => s.Score.HasValue).Average(s => s.Score!.Value), 2),
        run.IsCompleted ? null : run.CompletedCount + 1,
        run.Steps.OrderBy(s => s.Position).Select(s => new SpeakingStepProgress(s.Position,
            s.Score.HasValue ? "completed" : s.LeaseUntil > clock.GetUtcNow() ? "evaluating" :
            s.Position == run.CompletedCount + 1 ? "available" : "locked", s.Score, s.DurationMilliseconds, s.SubmissionId)).ToArray());
}

