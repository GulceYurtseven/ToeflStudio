using ToeflExam.Domain.Learning;

namespace ToeflExam.Domain.Reading;

public static class ReadingModules
{
    public static readonly string[] All = ["complete-words", "daily-life", "academic-passage"];
    /// <summary>Rejects an unavailable or unauthorized practice resource.</summary>
    public static string Require(string value) => All.Contains(value) ? value : throw new ArgumentException("Invalid Reading module.");
}

public sealed class ReadingRun
{
    private readonly List<ReadingStep> _steps = [];
    /// <summary>Allows Entity Framework to materialize a stored snapshot.</summary>
    private ReadingRun() { }
    /// <summary>Initializes the reading run with its required dependencies or validated state.</summary>
    public ReadingRun(string userId, string module, int version, Guid[] sets, DateTimeOffset now, Guid? source = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        Module = ReadingModules.Require(module);
        if (version < 1 || sets.Length == 0 || sets.Any(id => id == Guid.Empty) || sets.Distinct().Count() != sets.Length)
            throw new ArgumentException("Valid ordered content is required.");
        Id = Guid.NewGuid(); UserId = userId; Version = version; StartedAt = now; RestartedFromId = source;
        _steps.AddRange(sets.Select((id, i) => new ReadingStep(Id, i + 1, id)));
    }
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = "";
    public string Module { get; private set; } = "";
    public int Version { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public Guid? RestartedFromId { get; private set; }
    public bool IsCompleted { get; private set; }
    public int Revision { get; private set; }
    public IReadOnlyCollection<ReadingStep> Steps => _steps.AsReadOnly();
    public int CompletedCount => _steps.Count(s => s.SubmissionId.HasValue);
    /// <summary>Rejects locked or out-of-range practice steps.</summary>
    public ReadingStep Accessible(int position)
    {
        var step = _steps.SingleOrDefault(s => s.Position == position) ?? throw new PracticeRuleException("Activity not found.");
        if (position > CompletedCount + 1) throw new PracticeRuleException("Complete the next activity first.");
        return step;
    }
    /// <summary>Records successful completion and updates the relevant progress state.</summary>
    public void Complete(int position, Guid submissionId, int[] scores, DateTimeOffset now)
    {
        if (submissionId == Guid.Empty || scores.Length is < 1 or > 10 || scores.Any(s => s is < 0 or > 1))
            throw new ArgumentException("Invalid submission or score.");
        var step = Accessible(position);
        if (step.SubmissionId == submissionId) return;
        if (step.SubmissionId.HasValue || _steps.Any(s => s.SubmissionId == submissionId))
            throw new PracticeRuleException("This activity or submission ID has already been used.");
        step.Complete(submissionId, scores, now); Revision++; IsCompleted = CompletedCount == _steps.Count;
    }
}

public sealed class ReadingStep
{
    /// <summary>Allows Entity Framework to materialize a stored snapshot.</summary>
    private ReadingStep() { }
    /// <summary>Initializes the reading step with its required dependencies or validated state.</summary>
    internal ReadingStep(Guid run, int position, Guid set) => (RunId, Position, SetId) = (run, position, set);
    public Guid RunId { get; private set; }
    public int Position { get; private set; }
    public Guid SetId { get; private set; }
    public Guid? SubmissionId { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    // Only correctness flags are retained, never the submitted text or selected option.
    public int[] ItemScores { get; private set; } = [];
    /// <summary>Records successful completion and updates the relevant progress state.</summary>
    internal void Complete(Guid id, int[] scores, DateTimeOffset now)
        => (SubmissionId, ItemScores, CompletedAt) = (id, scores.ToArray(), now);
}
