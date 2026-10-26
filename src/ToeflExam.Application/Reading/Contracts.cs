using ToeflExam.Domain.Reading;

namespace ToeflExam.Application.Reading;

public sealed record ReadingOption(string Id, string Text);
public sealed record ReadingItem(int Id, string Prompt, string Prefix, int MissingLetters, ReadingOption[] Options,
    string Answer, string Explanation);
public sealed record ReadingContent(Guid Id, string Module, string Title, string Format, string Text, ReadingItem[] Items);
public sealed record ReadingPath(string Module, int Version, Guid[] SetIds);
public sealed record ReadingItemDto(int Id, string Prompt, string Prefix, int MissingLetters, ReadingOption[] Options);
public sealed record ReadingQuestionDto(Guid Id, string Title, string Format, string Text, ReadingItemDto[] Items);
public sealed record ReadingFeedbackItem(int Id, int Score, string CorrectAnswer, string Explanation);
public sealed record ReadingFeedback(int Score, int MaxScore, ReadingFeedbackItem[] Items);
public sealed record ReadingStepDto(int Position, ReadingQuestionDto Question, ReadingFeedback? Feedback);
public sealed record ReadingProgress(int Position, string Status, int? Score, int? MaxScore);
public sealed record ReadingRunDto(Guid Id, string Module, int Version, bool IsCompleted, int CompletedSteps,
    int TotalSteps, decimal CompletionPercent, int Score, int MaxScore, int? NextPosition, ReadingProgress[] Steps);
public sealed record ReadingSubmission(ReadingRunDto Progress, ReadingFeedback Feedback);
public sealed record ReadingModuleDto(string Module, int TotalSteps, int CompletedSteps, decimal CompletionPercent, Guid? RunId);
public interface IReadingStore
{
    Task<ReadingPath?> PathAsync(string module, CancellationToken ct);
    Task<ReadingContent?> ContentAsync(Guid id, CancellationToken ct);
    Task<ReadingRun?> RunAsync(string user, string module, Guid id, CancellationToken ct);
    Task<ReadingRun[]> HistoryAsync(string user, string module, int skip, int take, CancellationToken ct);
    Task<ReadingRun?> RestartAsync(string user, Guid source, CancellationToken ct);
    void Add(ReadingRun run);
    Task SaveAsync(CancellationToken ct);
}
