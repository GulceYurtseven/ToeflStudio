namespace ToeflExam.Domain.Learning;

public sealed class BuildSentenceRun
{
    private readonly List<BuildSentenceStep> _steps = [];
    /// <summary>Allows Entity Framework to materialize a stored snapshot.</summary>
    private BuildSentenceRun() { }

    /// <summary>Initializes the build sentence run with its required dependencies or validated state.</summary>
    public BuildSentenceRun(string userId, Guid pathVersionId, IEnumerable<Guid> questionVersionIds,
        DateTimeOffset startedAt, Guid? restartedFromId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var ids = questionVersionIds.ToArray();
        if (pathVersionId == Guid.Empty || ids.Length == 0 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
            throw new ArgumentException("A path must have a version and distinct ordered questions.");
        Id = Guid.NewGuid();
        UserId = userId;
        PathVersionId = pathVersionId;
        StartedAt = startedAt;
        RestartedFromId = restartedFromId;
        _steps.AddRange(ids.Select((id, index) => new BuildSentenceStep(Id, index + 1, id)));
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public Guid PathVersionId { get; private set; }
    public Guid? RestartedFromId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool IsCompleted { get; private set; }
    public int Revision { get; private set; }
    public IReadOnlyCollection<BuildSentenceStep> Steps => _steps.AsReadOnly();
    public int CompletedCount => _steps.Count(step => step.CompletedAt.HasValue);
    public int CorrectCount => _steps.Count(step => step.RawScore == 1);
    public decimal CompletionPercent => decimal.Round(100m * CompletedCount / _steps.Count, 2);

    /// <summary>Enforces ordered access before returning a practice step.</summary>
    public BuildSentenceStep GetAccessibleStep(int position)
    {
        var step = _steps.SingleOrDefault(step => step.Position == position)
            ?? throw new PracticeRuleException("This activity is not in this run.");
        if (position > CompletedCount + 1)
            throw new PracticeRuleException("Complete the next activity first.");
        return step;
    }

    /// <summary>Records an idempotent step result and updates run completion.</summary>
    public bool CompleteStep(int position, Guid submissionId, int score, DateTimeOffset completedAt)
    {
        if (submissionId == Guid.Empty || score is < 0 or > 1)
            throw new ArgumentException("A submission requires an identifier and a score of zero or one.");
        var step = GetAccessibleStep(position);
        if (step.CompletedAt.HasValue)
        {
            if (step.SubmissionId == submissionId) return false;
            throw new PracticeRuleException("This activity has already been submitted. Finish the run and restart to try again.");
        }
        if (_steps.Any(item => item.SubmissionId == submissionId))
            throw new PracticeRuleException("This submission ID belongs to another activity.");
        step.Complete(submissionId, score, completedAt);
        Revision++;
        IsCompleted = CompletedCount == _steps.Count;
        if (IsCompleted) CompletedAt = completedAt;
        return true;
    }
}

public sealed class BuildSentenceStep
{
    /// <summary>Allows Entity Framework to materialize a stored snapshot.</summary>
    private BuildSentenceStep() { }
    /// <summary>Initializes the build sentence step with its required dependencies or validated state.</summary>
    internal BuildSentenceStep(Guid runId, int position, Guid questionVersionId)
        => (RunId, Position, QuestionVersionId) = (runId, position, questionVersionId);

    public Guid RunId { get; private set; }
    public int Position { get; private set; }
    public Guid QuestionVersionId { get; private set; }
    public Guid? SubmissionId { get; private set; }
    public int? RawScore { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Records successful completion and updates the relevant progress state.</summary>
    internal void Complete(Guid submissionId, int score, DateTimeOffset at)
        => (SubmissionId, RawScore, CompletedAt) = (submissionId, score, at);
}
