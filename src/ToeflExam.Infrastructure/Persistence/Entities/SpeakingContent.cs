using ToeflExam.Domain.Content;
using ToeflExam.Domain.Speaking;

namespace ToeflExam.Infrastructure.Persistence.Entities;

public sealed class SpeakingQuestionVersion
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public int Version { get; set; }
    public SpeakingTaskKind Kind { get; set; }
    public PublicationStatus PublicationStatus { get; set; }
    public string Title { get; set; } = "";
    public string Instructions { get; set; } = "";
    public string Context { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string AudioFile { get; set; } = "";
    public int ResponseSeconds { get; set; }
    public int ScenePosition { get; set; }
}
public sealed class SpeakingPathVersion
{
    public Guid Id { get; set; }
    public SpeakingTaskKind Kind { get; set; }
    public int Version { get; set; }
    public string Title { get; set; } = "";
    public bool IsPublished { get; set; }
    public List<SpeakingPathStep> Steps { get; set; } = [];
}
public sealed class SpeakingPathStep
{
    public Guid PathId { get; set; }
    public int Position { get; set; }
    public Guid QuestionId { get; set; }
}

