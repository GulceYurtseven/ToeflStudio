using ToeflExam.Application.Reading;
using ToeflExam.Application.Writing;

namespace ToeflExam.Application.ContentBank;

// Server-side authored content. Never return this object directly from an API (it includes keys).
public sealed record BankTile(int Id, string Text);
public sealed record BankEntry
{
    public Guid Id { get; init; }
    public string Pool { get; init; } = "";
    public string ExamId { get; init; } = "";
    public string Stage { get; init; } = "linear";
    public string Section { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Title { get; init; } = "";
    public string Text { get; init; } = "";
    public string Context { get; init; } = "";
    public string Instructions { get; init; } = "";
    public string Prompt { get; init; } = "";
    public string AudioFile { get; init; } = "";
    public string[] Requirements { get; init; } = [];
    public DiscussionPost[] Posts { get; init; } = [];
    public ReadingItem[] Questions { get; init; } = [];
    public string Prefix { get; init; } = "";
    public string Suffix { get; init; } = "";
    public BankTile[] Tiles { get; init; } = [];
    public int[] Order { get; init; } = [];
    public string Explanation { get; init; } = "";
    public int ResponseSeconds { get; init; }
    public int ScenePosition { get; init; }
    public int QuestionCount => Questions.Length > 0 ? Questions.Length : 1;
}
public interface IContentBank
{
    IReadOnlyList<BankEntry> Practice { get; }
    IReadOnlyList<BankEntry> Exams { get; }
}
