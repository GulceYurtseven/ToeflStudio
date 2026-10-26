using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Domain.Learning;
using ToeflExam.Domain.Reading;

namespace ToeflExam.Application.Reading;

public sealed class ReadingPracticeService(IReadingStore store, TimeProvider clock)
{
    /// <summary>Combines the available Reading modules with the current student progress.</summary>
    public async Task<ReadingModuleDto[]> OverviewAsync(string user, CancellationToken ct)
    {
        var modules = new List<ReadingModuleDto>();
        foreach (var module in ReadingModules.All)
        {
            var path = await Path(module, ct);
            var latest = (await store.HistoryAsync(user, module, 0, 1, ct)).FirstOrDefault();
            var dto = latest is null ? null : ToDto(latest);
            modules.Add(new(module, dto?.TotalSteps ?? path.SetIds.Length, dto?.CompletedSteps ?? 0, dto?.CompletionPercent ?? 0, dto?.Id));
        }
        return modules.ToArray();
    }
    /// <summary>Loads student-owned run summaries with the requested scope.</summary>
    public async Task<ReadingRunDto[]> HistoryAsync(string user, string module, int skip, int take, CancellationToken ct)
    {
        ReadingModules.Require(module);
        if (skip < 0 || take is < 1 or > 50) throw new ArgumentException("skip must be nonnegative; take must be between 1 and 50.");
        return (await store.HistoryAsync(user, module, skip, take, ct)).Select(ToDto).ToArray();
    }
    /// <summary>Resumes an existing run or creates an ordered run for the selected content.</summary>
    public async Task<ReadingRunDto> StartAsync(string user, string module, CancellationToken ct)
    {
        ReadingModules.Require(module);
        var latest = (await store.HistoryAsync(user, module, 0, 1, ct)).FirstOrDefault();
        return latest is null ? await Create(user, module, null, ct) : ToDto(latest);
    }
    /// <summary>Resolves or creates a new run linked to a completed source run.</summary>
    public async Task<ReadingRunDto> RestartAsync(string user, string module, Guid id, CancellationToken ct)
    {
        var source = await Require(user, module, id, ct);
        if (!source.IsCompleted) throw new PracticeRuleException("Complete the current run before starting a new one.");
        var existing = await store.RestartAsync(user, id, ct);
        if (existing is not null) return ToDto(existing);
        if ((await store.HistoryAsync(user, module, 0, 1, ct)).FirstOrDefault()?.Id != id)
            throw new PracticeRuleException("Only the latest completed run can be restarted.");
        return await Create(user, module, id, ct);
    }
    /// <summary>Creates the requested practice state using the selected content edition.</summary>
    private async Task<ReadingRunDto> Create(string user, string module, Guid? source, CancellationToken ct)
    {
        var path = await Path(module, ct);
        var run = new ReadingRun(user, module, path.Version, path.SetIds, clock.GetUtcNow(), source);
        store.Add(run); await store.SaveAsync(ct); return ToDto(run);
    }
    /// <summary>Loads the student-owned run and returns its current progress.</summary>
    public async Task<ReadingRunDto> ProgressAsync(string user, string module, Guid id, CancellationToken ct)
        => ToDto(await Require(user, module, id, ct));
    /// <summary>Loads an accessible practice step without disclosing future answer keys.</summary>
    public async Task<ReadingStepDto> StepAsync(string user, string module, Guid id, int position, CancellationToken ct)
    {
        var step = (await Require(user, module, id, ct)).Accessible(position);
        var content = await Content(step.SetId, ct);
        return new(position, new(content.Id, content.Title, content.Format, content.Text,
            content.Items.Select(i => new ReadingItemDto(i.Id, i.Prompt, i.Prefix, i.MissingLetters, i.Options)).ToArray()),
            step.SubmissionId.HasValue ? Feedback(content, step.ItemScores) : null);
    }
    /// <summary>Checks submission identity and run ownership before recording scores and progress.</summary>
    public async Task<ReadingSubmission> SubmitAsync(string user, string module, Guid id, int position,
        Guid submissionId, Dictionary<int, string>? answers, CancellationToken ct)
    {
        if (submissionId == Guid.Empty || answers is null || answers.Count > 10 || answers.Values.Any(v => v is null || v.Length > 80))
            throw new ArgumentException("A valid submission ID and no more than 10 short answers are required.");
        var run = await Require(user, module, id, ct);
        var step = run.Accessible(position);
        var content = await Content(step.SetId, ct);
        if (step.SubmissionId.HasValue)
        {
            if (step.SubmissionId != submissionId) throw new PracticeRuleException("Etkinlik zaten teslim edildi.");
            return new(ToDto(run), Feedback(content, step.ItemScores));
        }
        if (answers.Keys.Any(k => !content.Items.Any(i => i.Id == k))) throw new ArgumentException("Unknown question ID.");
        var scores = content.Items.Select(item =>
        {
            var answer = answers.GetValueOrDefault(item.Id, "").Trim();
            if (item.Options.Length > 0 && answer.Length > 0 && !item.Options.Any(o => o.Id == answer))
                throw new ArgumentException("Invalid option.");
            return string.Equals(answer, item.Answer, item.Options.Length == 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ? 1 : 0;
        }).ToArray();
        run.Complete(position, submissionId, scores, clock.GetUtcNow());
        await store.SaveAsync(ct);
        return new(ToDto(run), Feedback(content, scores));
    }
    /// <summary>Loads the ordered published path required for this practice module.</summary>
    private async Task<ReadingPath> Path(string module, CancellationToken ct)
        => await store.PathAsync(ReadingModules.Require(module), ct) ?? throw new PracticeNotFoundException();
    /// <summary>Loads content for the selected module or exam edition.</summary>
    private async Task<ReadingContent> Content(Guid id, CancellationToken ct)
        => await store.ContentAsync(id, ct) ?? throw new PracticeNotFoundException();
    /// <summary>Rejects an unavailable or unauthorized practice resource.</summary>
    private async Task<ReadingRun> Require(string user, string module, Guid id, CancellationToken ct)
        => await store.RunAsync(user, ReadingModules.Require(module), id, ct) ?? throw new PracticeNotFoundException();
    /// <summary>Builds explanatory feedback from the question key and recorded objective scores.</summary>
    private static ReadingFeedback Feedback(ReadingContent content, int[] scores) => new(scores.Sum(), scores.Length,
        content.Items.Select((item, i) => new ReadingFeedbackItem(item.Id, scores[i],
            item.Options.Length == 0 ? item.Prefix + item.Answer : item.Options.Single(o => o.Id == item.Answer).Text, item.Explanation)).ToArray());
    /// <summary>Projects domain progress into the student-facing response contract.</summary>
    private static ReadingRunDto ToDto(ReadingRun run) => new(run.Id, run.Module, run.Version, run.IsCompleted,
        run.CompletedCount, run.Steps.Count, decimal.Round(100m * run.CompletedCount / run.Steps.Count, 2),
        run.Steps.Sum(s => s.ItemScores.Sum()), run.Steps.Sum(s => s.ItemScores.Length),
        run.IsCompleted ? null : run.CompletedCount + 1,
        run.Steps.OrderBy(s => s.Position).Select(s => new ReadingProgress(s.Position,
            s.SubmissionId.HasValue ? "completed" : s.Position == run.CompletedCount + 1 ? "available" : "locked",
            s.SubmissionId.HasValue ? s.ItemScores.Sum() : null, s.SubmissionId.HasValue ? s.ItemScores.Length : null)).ToArray());
}
