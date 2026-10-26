namespace ToeflExam.Domain.Learning;

// Scheduling and score metadata live here; latest completed exam detail is stored in a separate report snapshot.
public sealed class BankSession
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Form { get; set; } = "";
    public int Edition { get; set; } = 2;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? Deadline { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? RestartedFromId { get; set; }
    public int Revision { get; set; }
    public List<BankStep> Steps { get; set; } = [];
}
public sealed class BankStep
{
    public int ItemIndex { get; set; } = -1;
    public Guid SessionId { get; set; }
    public int Position { get; set; }
    public Guid ContentId { get; set; }
    public string Section { get; set; } = "";
    public string Stage { get; set; } = "";
    public string Kind { get; set; } = "";
    public bool Excluded { get; set; }
    public Guid? SubmissionId { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public int[] Scores { get; set; } = [];
    public int Maximum { get; set; }
    public bool NeedsEvaluation { get; set; }
    public bool TimedOut { get; set; }
}
