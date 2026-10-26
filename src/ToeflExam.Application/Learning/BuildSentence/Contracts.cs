using ToeflExam.Application.Content.GetBuildSentenceQuestion;
using ToeflExam.Domain.Content;
using ToeflExam.Domain.Learning;

namespace ToeflExam.Application.Learning.BuildSentence;

public sealed record PathDefinition(Guid Id, string Title, int Version, Guid[] QuestionVersionIds);
public sealed record PathDto(Guid Id, string Title, int Version, int TotalSteps);
public sealed record StepProgressDto(int Position, string Status, int? RawScore);
public sealed record RunDto(Guid Id, Guid PathVersionId, DateTimeOffset StartedAt, bool IsCompleted,
    int CompletedSteps, int TotalSteps, decimal CompletionPercent, int CorrectAnswers, int? NextPosition,
    IReadOnlyList<StepProgressDto> Steps);
public sealed record QuestionContent(BuildSentenceQuestion Question, int[][] AcceptedOrders, string Explanation);
public sealed record StepDto(int Position, BuildSentenceQuestionDto Question, FeedbackDto? Feedback);
public sealed record FeedbackDto(int RawScore, int MaxRawScore, string Explanation, int[][] AcceptedOrders,
    IReadOnlyList<string> CorrectSentences);
public sealed record SubmissionDto(FeedbackDto Feedback, RunDto Progress);

public interface IBuildSentencePracticeStore
{
    Task<PathDefinition?> GetCurrentPathAsync(CancellationToken ct);
    Task<BuildSentenceRun?> FindRunAsync(string userId, Guid runId, CancellationToken ct);
    Task<BuildSentenceRun?> FindLatestRunAsync(string userId, CancellationToken ct);
    Task<BuildSentenceRun?> FindRestartAsync(string userId, Guid sourceRunId, CancellationToken ct);
    Task<IReadOnlyList<BuildSentenceRun>> ListRunsAsync(string userId, int skip, int take, CancellationToken ct);
    Task<QuestionContent?> GetQuestionAsync(Guid versionId, CancellationToken ct);
    Task<bool> IsPathAvailableAsync(PathDefinition path, CancellationToken ct);
    void AddRun(BuildSentenceRun run);
    Task SaveAsync(CancellationToken ct);
}

public sealed class PracticeNotFoundException() : Exception("Practice run not found.");
public sealed class PracticeConflictException(string message) : Exception(message);
