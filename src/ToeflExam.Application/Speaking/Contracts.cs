using ToeflExam.Domain.Speaking;

namespace ToeflExam.Application.Speaking;

public sealed record SpeakingQuestion(Guid Id, SpeakingTaskKind Kind, string Title, string Instructions, string Context,
    string Prompt, string AudioFile, int ResponseSeconds, int ScenePosition);
// Intentionally no spoken prompt or file name; authenticated audio endpoint resolves the pinned question.
public sealed record SpeakingQuestionDto(Guid Id, string Title, string Instructions, string Context, int ResponseSeconds, int ScenePosition);
public sealed record SpeakingPath(Guid Id, SpeakingTaskKind Kind, int Version, string Title, Guid[] QuestionIds);
public sealed record SpeakingPathDto(Guid Id, string Module, int Version, string Title, int TotalSteps);
public sealed record SpeakingStepProgress(int Position, string Status, int? Score, int? DurationMilliseconds, Guid? SubmissionId);
public sealed record SpeakingRunDto(Guid Id, string Module, Guid PathId, bool IsCompleted, int CompletedSteps,
    int TotalSteps, decimal CompletionPercent, decimal? AverageRawScore, int? NextPosition, SpeakingStepProgress[] Steps);
public sealed record SpeakingStepDto(int Position, SpeakingQuestionDto Question, SpeakingReport? Report);
public sealed record SpeakingIssue(string Category, string Explanation, string Suggestion);
public sealed record SpeakingFeedback(int Score, string Transcript, string Rationale, string[] Strengths,
    SpeakingIssue[] Issues, string[] NextSteps);
public sealed record SpeakingScoringResult(SpeakingFeedback Feedback, string Model);
public sealed record SpeakingReport(Guid RunId, int Position, Guid SubmissionId, string Module, string Title,
    string Prompt, int DurationMilliseconds, SpeakingFeedback Feedback, string Model, string RubricVersion,
    DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt)
{
    public bool IsOfficialScore => false;
    public int MaxRawScore => 5;
    public string AssessmentScope => "transcript-content-only";
    public string[] UnassessedCriteria => ["pronunciation", "intelligibility", "fluency", "rhythm-and-intonation"];
    public string Limitation => "This is a transcript-based content estimate, not a full TOEFL Speaking rubric score. Transcription can contain errors. Audio delivery criteria are not assessed.";
}
public sealed record SpeakingSubmission(SpeakingRunDto Progress, SpeakingReport? Report, bool ReportUnavailable);

public interface ISpeakingStore
{
    Task<SpeakingPath?> CurrentPathAsync(SpeakingTaskKind kind, CancellationToken ct);
    Task<bool> AvailableAsync(SpeakingPath path, CancellationToken ct);
    Task<SpeakingQuestion?> QuestionAsync(Guid id, CancellationToken ct);
    Task<SpeakingRun?> RunAsync(string userId, SpeakingTaskKind kind, Guid runId, CancellationToken ct);
    Task<SpeakingRun?> LatestAsync(string userId, SpeakingTaskKind kind, CancellationToken ct);
    Task<SpeakingRun?> RestartAsync(string userId, Guid sourceId, CancellationToken ct);
    Task<IReadOnlyList<SpeakingRun>> HistoryAsync(string userId, SpeakingTaskKind kind, int skip, int take, CancellationToken ct);
    void Add(SpeakingRun run);
    Task SaveAsync(CancellationToken ct);
}
public interface ISpeakingScorer
{
    Task<SpeakingScoringResult> ScoreAsync(SpeakingQuestion question, SpeakingAudio audio, CancellationToken ct);
}
// Transcription receives no reference answer: missing words must never be filled from the prompt.
public sealed record SpeechTranscript(string Text, string Model);
public interface ISpeechTranscriber
{
    Task<SpeechTranscript> TranscribeAsync(SpeakingAudio audio, CancellationToken ct);
}
public interface ISpeakingReportCache
{
    void Put(string userId, SpeakingReport report);
    SpeakingReport? Get(string userId, Guid runId, int position);
}
public sealed class SpeakingScoringException(string message, bool unavailable = false) : Exception(message)
{ public bool Unavailable { get; } = unavailable; }
public sealed class SpeakingReportExpiredException() : Exception("Detailed speaking feedback has expired. Your score and progress are saved.");
public static class SpeakingModules
{
    /// <summary>Validates an external representation and converts it to the supported domain value.</summary>
    public static SpeakingTaskKind Parse(string value) => value switch
    {
        "listen-repeat" => SpeakingTaskKind.ListenRepeat,
        "interview" => SpeakingTaskKind.Interview,
        _ => throw new ToeflExam.Application.Learning.BuildSentence.PracticeNotFoundException()
    };
    /// <summary>Converts a supported task kind into its stable API route name.</summary>
    public static string Slug(SpeakingTaskKind kind) => kind switch
    {
        SpeakingTaskKind.ListenRepeat => "listen-repeat", SpeakingTaskKind.Interview => "interview",
        _ => throw new ArgumentException("Invalid speaking kind.")
    };
}
