using ToeflExam.Application.Reading;
using ToeflExam.Application.Writing;

namespace ToeflExam.Application.ContentBank;

public sealed record BankQuestionDto(Guid Id,string Section,string Kind,string Title,string Text,string Context,string Instructions,
    string Prompt,string Prefix,string Suffix,BankTile[] Tiles,ReadingItemDto[] Questions,string[] Requirements,DiscussionPost[] Posts,int ResponseSeconds,int ScenePosition);
public sealed record BankProgress(Guid Id,string Mode,string Form,bool Completed,int CompletedActivities,int TotalActivities,
    decimal CompletionPercent,int? NextPosition,DateTimeOffset? Deadline,int PendingEvaluations,int[] AccessiblePositions, bool CanReview = false, string? Phase = null, bool HasSavedReport = false);
public sealed record BankStepDto(int Position,BankQuestionDto Question,object? Feedback,bool PlayAudio=true, BankAnswer? Draft = null);
public sealed record BankAnswer(Guid SubmissionId,Dictionary<int,string>? Answers,string? Text,int[]? Order,string? AudioBase64,bool Skip=false);
public sealed record BankPhaseSubmission(int AnchorPosition, Dictionary<int, BankAnswer> Responses);
public sealed record BankResult(BankProgress Progress,object? Feedback);
public sealed record SectionResult(string Section,int Score,int Maximum,int Pending,bool IsOfficialScore=false);
public sealed record ExamReport(Guid Id,string Form,DateTimeOffset CompletedAt,DateTimeOffset? ExpiresAt,
    SectionResult[] Sections,object[] Details,string[] Limitations,bool DetailsUnavailable,bool IsOfficialScore=false);
public interface IBankSessionService
{
    Task<BankProgress> StartAsync(string user,string mode,string form,CancellationToken ct);
    Task<BankProgress[]> HistoryAsync(string user,string mode,string form,CancellationToken ct);
    Task<BankProgress> RestartAsync(string user,Guid id,CancellationToken ct);
    Task<BankProgress> ProgressAsync(string user,Guid id,CancellationToken ct);
    Task<BankStepDto> StepAsync(string user,Guid id,int position,CancellationToken ct);
    Task<string> AudioAsync(string user,Guid id,int position,CancellationToken ct);
    Task<BankResult> SubmitAsync(string user,Guid id,int position,BankAnswer answer,CancellationToken ct);
    Task<BankProgress> SaveDraftAsync(string user, Guid id, int position, BankAnswer answer, CancellationToken ct);
    Task<BankResult> FinishPhaseAsync(string user, Guid id, BankPhaseSubmission submission, CancellationToken ct);
    Task<ExamReport> ReportAsync(string user,Guid id,CancellationToken ct);
    Task<ExamStudyPlan> StudyPlanAsync(string user,Guid id,CancellationToken ct);
    Task<ExamReport> EvaluateNextAsync(string user,Guid id,CancellationToken ct);
}
