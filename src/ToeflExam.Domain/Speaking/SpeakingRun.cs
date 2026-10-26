using ToeflExam.Domain.Learning;

namespace ToeflExam.Domain.Speaking;

public sealed class SpeakingRun
{
    private readonly List<SpeakingStep> _steps = [];
    /// <summary>Allows Entity Framework to materialize a stored snapshot.</summary>
    private SpeakingRun() { }
    /// <summary>Initializes the speaking run with its required dependencies or validated state.</summary>
    public SpeakingRun(string userId, SpeakingTaskKind kind, Guid pathId, IEnumerable<Guid> questions,
        DateTimeOffset now, Guid? restartedFromId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var ids = questions.ToArray();
        if (!Enum.IsDefined(kind) || pathId == Guid.Empty || ids.Length == 0 ||
            ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
            throw new ArgumentException("A speaking path needs a valid kind and distinct questions.");
        Id = Guid.NewGuid(); UserId = userId; Kind = kind; PathId = pathId;
        StartedAt = now; RestartedFromId = restartedFromId;
        _steps.AddRange(ids.Select((id, i) => new SpeakingStep(Id, i + 1, id)));
    }
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = "";
    public SpeakingTaskKind Kind { get; private set; }
    public Guid PathId { get; private set; }
    public Guid? RestartedFromId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public bool IsCompleted { get; private set; }
    public int Revision { get; private set; }
    public IReadOnlyCollection<SpeakingStep> Steps => _steps.AsReadOnly();
    public int CompletedCount => _steps.Count(s => s.Score.HasValue);
    public decimal CompletionPercent => decimal.Round(100m * CompletedCount / _steps.Count, 2);

    /// <summary>Checks ordered access and returns the requested domain step.</summary>
    public SpeakingStep AccessibleStep(int position)
    {
        var step = _steps.SingleOrDefault(s => s.Position == position)
            ?? throw new PracticeRuleException("This activity is not in this run.");
        if (position > CompletedCount + 1) throw new PracticeRuleException("Complete the next activity first.");
        return step;
    }

    /// <summary>Acquires a bounded evaluation lease for an idempotent submission.</summary>
    public Guid BeginEvaluation(int position, Guid submissionId, DateTimeOffset now)
    {
        if (submissionId == Guid.Empty) throw new ArgumentException("A submission ID is required.");
        var step = AccessibleStep(position);
        if (step.Score.HasValue) throw new PracticeRuleException("This activity is already complete.");
        if (step.LeaseUntil > now) throw new PracticeRuleException("Assessment is in progress. Wait, then retry with the same submission ID.");
        if (_steps.Any(s => s.Position != position && s.SubmissionId == submissionId))
            throw new PracticeRuleException("This submission ID belongs to another activity.");
        var token = Guid.NewGuid();
        step.Begin(submissionId, token, now.AddMinutes(5));
        Revision++;
        return token;
    }

    /// <summary>Records successful completion and updates the relevant progress state.</summary>
    public void Complete(int position, Guid leaseToken, int score, int durationMilliseconds, string model, string rubric, DateTimeOffset now)
    {
        if (score is < 0 or > 5 || durationMilliseconds < 0) throw new ArgumentException("Invalid score or recording duration.");
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(rubric);
        var step = AccessibleStep(position);
        if (step.LeaseToken != leaseToken || step.LeaseUntil <= now || step.Score.HasValue)
            throw new PracticeRuleException("The assessment lease expired or another request took over.");
        step.Finish(score, durationMilliseconds, model, rubric, now);
        Revision++;
        IsCompleted = CompletedCount == _steps.Count;
    }

    /// <summary>Releases a failed evaluation lease so the response can be retried.</summary>
    public void Release(int position, Guid leaseToken)
    {
        var step = AccessibleStep(position);
        if (step.LeaseToken != leaseToken || step.Score.HasValue) return;
        step.Release();
        Revision++;
    }
}

public sealed class SpeakingStep
{
    /// <summary>Allows Entity Framework to materialize a stored snapshot.</summary>
    private SpeakingStep() { }
    /// <summary>Initializes the speaking step with its required dependencies or validated state.</summary>
    internal SpeakingStep(Guid runId, int position, Guid questionId)
        => (RunId, Position, QuestionId) = (runId, position, questionId);
    public Guid RunId { get; private set; }
    public int Position { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid? SubmissionId { get; private set; }
    public Guid? LeaseToken { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public int? Score { get; private set; }
    public int? DurationMilliseconds { get; private set; }
    public string? Model { get; private set; }
    public string? RubricVersion { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    /// <summary>Marks an assessment as processing with its submission ID and lease expiration.</summary>
    internal void Begin(Guid submissionId, Guid token, DateTimeOffset until)
        => (SubmissionId, LeaseToken, LeaseUntil) = (submissionId, token, until);
    /// <summary>Releases a failed evaluation lease so the response can be retried.</summary>
    internal void Release() => (SubmissionId, LeaseToken, LeaseUntil) = (null, null, null);
    /// <summary>Records the assessment score and provenance and completes the step.</summary>
    internal void Finish(int score, int count, string model, string rubric, DateTimeOffset now)
    {
        Score = score; DurationMilliseconds = count; Model = model; RubricVersion = rubric; CompletedAt = now;
        LeaseToken = null; LeaseUntil = null;
    }
}

