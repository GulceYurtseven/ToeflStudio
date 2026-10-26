using ToeflExam.Domain.Writing;

namespace ToeflExam.Application.Writing;

public sealed record DiscussionPost(string Name, string Text);
public sealed record WritingQuestion(Guid Id, WritingTaskKind Kind, string Title, string Instructions,
    string Context, string[] Requirements, DiscussionPost[] Posts);
public sealed record WritingPath(Guid Id, WritingTaskKind Kind, int Version, string Title, Guid[] QuestionIds);
public sealed record WritingPathDto(Guid Id, string Module, int Version, string Title, int TotalSteps);
public sealed record WritingStepProgress(int Position, string Status, int? Score, int? WordCount, Guid? SubmissionId);
public sealed record WritingRunDto(Guid Id, string Module, Guid PathId, bool IsCompleted, int CompletedSteps,
    int TotalSteps, decimal CompletionPercent, decimal? AverageRawScore, int? NextPosition, WritingStepProgress[] Steps);
public sealed record WritingStepDto(int Position, WritingQuestion Question, WritingReport? Report);
public sealed record WritingIssue(string Category, string Evidence, string Explanation, string Suggestion);
public sealed record WritingFeedback(int Score, string Rationale, string[] Strengths, WritingIssue[] Issues,
    string[] NextSteps, string ImprovedResponse);
public sealed record ScoringResult(WritingFeedback Feedback, string Model);
public sealed record WritingReport(Guid RunId, int Position, Guid SubmissionId, string Module, WritingQuestion Question,
    string Answer, int WordCount, WritingFeedback Feedback, string Model, string RubricVersion,
    DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt)
{
    public bool IsOfficialScore => false;
    public int MaxRawScore => 5;
}
public sealed record WritingSubmission(WritingRunDto Progress, WritingReport? Report, bool ReportUnavailable);

public interface IWritingStore
{
    Task<WritingPath?> CurrentPathAsync(WritingTaskKind kind, CancellationToken ct);
    Task<bool> AvailableAsync(WritingPath path, CancellationToken ct);
    Task<WritingQuestion?> QuestionAsync(Guid id, CancellationToken ct);
    Task<WritingRun?> RunAsync(string userId, WritingTaskKind kind, Guid runId, CancellationToken ct);
    Task<WritingRun?> LatestAsync(string userId, WritingTaskKind kind, CancellationToken ct);
    Task<WritingRun?> RestartAsync(string userId, Guid sourceId, CancellationToken ct);
    Task<IReadOnlyList<WritingRun>> HistoryAsync(string userId, WritingTaskKind kind, int skip, int take, CancellationToken ct);
    void Add(WritingRun run);
    Task SaveAsync(CancellationToken ct);
}
public interface IWritingScorer
{
    Task<ScoringResult> ScoreAsync(WritingQuestion question, string answer, CancellationToken ct);
}
public interface IWritingReportCache
{
    void Put(string userId, WritingReport report);
    WritingReport? Get(string userId, Guid runId, int position);
}
public sealed class WritingScoringException(string message, bool unavailable = false) : Exception(message)
{
    public bool Unavailable { get; } = unavailable;
}
public sealed class WritingReportExpiredException() : Exception("Detailed feedback is no longer in memory. Your score summary is saved.");
public static class WritingModules
{
    /// <summary>Validates an external representation and converts it to the supported domain value.</summary>
    public static WritingTaskKind Parse(string value) => value switch
    {
        "email" => WritingTaskKind.Email,
        "academic-discussion" => WritingTaskKind.AcademicDiscussion,
        _ => throw new ToeflExam.Application.Learning.BuildSentence.PracticeNotFoundException()
    };
    /// <summary>Converts a supported task kind into its stable API route name.</summary>
    public static string Slug(WritingTaskKind kind) => kind == WritingTaskKind.Email ? "email" : "academic-discussion";
}
