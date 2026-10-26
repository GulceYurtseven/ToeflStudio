using ToeflExam.Application.Content.GetBuildSentenceQuestion;
using ToeflExam.Domain.Learning;

namespace ToeflExam.Application.Learning.BuildSentence;

public sealed class BuildSentencePracticeService(IBuildSentencePracticeStore store, TimeProvider clock)
{
    /// <summary>Loads the currently published ordered question path.</summary>
    public async Task<PathDto> GetPathAsync(CancellationToken ct)
    {
        var path = await store.GetCurrentPathAsync(ct) ?? throw new PracticeNotFoundException();
        return new(path.Id, path.Title, path.Version, path.QuestionVersionIds.Length);
    }

    /// <summary>Resumes an existing run or creates an ordered run for the selected content.</summary>
    public async Task<RunDto> StartAsync(string userId, CancellationToken ct)
    {
        var existing = await store.FindLatestRunAsync(userId, ct);
        if (existing is not null) return ToDto(existing);
        return await CreateRunAsync(userId, null, ct);
    }

    /// <summary>Resolves or creates a new run linked to a completed source run.</summary>
    public async Task<RunDto> RestartAsync(string userId, Guid runId, CancellationToken ct)
    {
        var source = await RequireRunAsync(userId, runId, ct);
        if (!source.IsCompleted) throw new PracticeRuleException("Complete the current run before restarting.");
        var alreadyRestarted = await store.FindRestartAsync(userId, source.Id, ct);
        if (alreadyRestarted is not null) return ToDto(alreadyRestarted);
        var latest = await store.FindLatestRunAsync(userId, ct);
        if (latest?.Id != runId) throw new PracticeRuleException("Only the latest completed run can be restarted.");
        return await CreateRunAsync(userId, runId, ct);
    }

    /// <summary>Creates a run pinned to immutable question versions.</summary>
    private async Task<RunDto> CreateRunAsync(string userId, Guid? source, CancellationToken ct)
    {
        var path = await store.GetCurrentPathAsync(ct) ?? throw new PracticeNotFoundException();
        if (!await store.IsPathAvailableAsync(path, ct))
            throw new PracticeConflictException("This learning path is currently unavailable.");
        var run = new BuildSentenceRun(userId, path.Id, path.QuestionVersionIds, clock.GetUtcNow(), source);
        store.AddRun(run);
        await store.SaveAsync(ct);
        return ToDto(run);
    }

    /// <summary>Returns progress for the student-owned run.</summary>
    public async Task<RunDto> GetRunAsync(string userId, Guid runId, CancellationToken ct)
        => ToDto(await RequireRunAsync(userId, runId, ct));

    /// <summary>Returns a bounded page of the student's run history.</summary>
    public async Task<IReadOnlyList<RunDto>> ListRunsAsync(string userId, int skip, int take, CancellationToken ct)
    {
        if (skip < 0 || take is < 1 or > 50) throw new ArgumentException("skip must be nonnegative; take must be between 1 and 50.");
        return (await store.ListRunsAsync(userId, skip, take, ct)).Select(ToDto).ToArray();
    }

    /// <summary>Returns the accessible question and any available feedback for this run position.</summary>
    public async Task<StepDto> GetStepAsync(string userId, Guid runId, int position, CancellationToken ct)
    {
        var run = await RequireRunAsync(userId, runId, ct);
        var step = run.GetAccessibleStep(position);
        var content = await store.GetQuestionAsync(step.QuestionVersionId, ct) ?? throw new PracticeNotFoundException();
        var question = content.Question;
        return new(position, new(question.Id, question.Prompt,
            question.Tiles.Select(tile => new SentenceTileDto(tile.Id, tile.Text)).ToArray(),
            question.ResponseParts.Select(part => new SentencePartDto(part.IsSlot, part.Text)).ToArray(), question.SlotCount),
            step.RawScore.HasValue ? Feedback(content, step.RawScore.Value) : null);
    }

    /// <summary>Checks submission identity and run ownership before recording scores and progress.</summary>
    public async Task<SubmissionDto> SubmitAsync(string userId, Guid runId, int position,
        Guid submissionId, int[] tileIds, CancellationToken ct)
    {
        if (submissionId == Guid.Empty || tileIds is null || tileIds.Length > 50)
            throw new ArgumentException("A submission ID and no more than 50 tiles are required.");
        var run = await RequireRunAsync(userId, runId, ct);
        var step = run.GetAccessibleStep(position);
        var content = await store.GetQuestionAsync(step.QuestionVersionId, ct) ?? throw new PracticeNotFoundException();

        // A retry of an accepted submission returns its original score. Raw responses are not retained.
        if (step.CompletedAt.HasValue)
        {
            if (step.SubmissionId != submissionId) throw new PracticeRuleException("Bu etkinlik zaten teslim edildi.");
            return new(Feedback(content, step.RawScore!.Value), ToDto(run));
        }

        var score = content.Question.Score(tileIds);
        var feedback = Feedback(content, score);
        run.CompleteStep(position, submissionId, score, clock.GetUtcNow());
        await store.SaveAsync(ct);
        return new(feedback, ToDto(run));
    }

    /// <summary>Loads the student-owned run required for a practice operation.</summary>
    private async Task<BuildSentenceRun> RequireRunAsync(string userId, Guid runId, CancellationToken ct)
        => await store.FindRunAsync(userId, runId, ct) ?? throw new PracticeNotFoundException();

    /// <summary>Builds explanatory feedback from the question key and recorded objective scores.</summary>
    private static FeedbackDto Feedback(QuestionContent content, int score)
    {
        var sentences = content.AcceptedOrders.Select(order =>
        {
            var position = 0;
            var tiles = content.Question.Tiles.ToDictionary(tile => tile.Id, tile => tile.Text);
            var fragments = new List<string>();
            foreach (var part in content.Question.ResponseParts)
                fragments.Add(part.IsSlot ? tiles[order[position++]] : part.Text!);
            return string.Join(" ", fragments.Select(fragment => fragment.Trim()))
                .Replace(" .", ".").Replace(" ?", "?").Replace(" ,", ",").Replace(" !", "!");
        }).ToArray();
        return new(score, 1, content.Explanation, content.AcceptedOrders, sentences);
    }

    /// <summary>Projects domain progress into the student-facing response contract.</summary>
    private static RunDto ToDto(BuildSentenceRun run) => new(run.Id, run.PathVersionId, run.StartedAt,
        run.IsCompleted, run.CompletedCount, run.Steps.Count, run.CompletionPercent, run.CorrectCount,
        run.IsCompleted ? null : run.CompletedCount + 1,
        run.Steps.OrderBy(step => step.Position).Select(step => new StepProgressDto(step.Position,
            step.CompletedAt.HasValue ? "completed" : step.Position == run.CompletedCount + 1 ? "available" : "locked",
            step.RawScore)).ToArray());
}
