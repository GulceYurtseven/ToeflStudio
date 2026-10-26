using ToeflExam.Domain.Content;
using ToeflExam.Domain.Writing;

namespace ToeflExam.Infrastructure.Persistence.Entities;

public sealed class WritingQuestionVersion
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public int Version { get; set; }
    public WritingTaskKind Kind { get; set; }
    public PublicationStatus PublicationStatus { get; set; }
    public string Title { get; set; } = "";
    public string Instructions { get; set; } = "";
    public string Context { get; set; } = "";
    public string RequirementsJson { get; set; } = "[]";
    public string PostsJson { get; set; } = "[]";
}
public sealed class WritingPathVersion
{
    public Guid Id { get; set; }
    public WritingTaskKind Kind { get; set; }
    public int Version { get; set; }
    public string Title { get; set; } = "";
    public bool IsPublished { get; set; }
    public List<WritingPathStep> Steps { get; set; } = [];
}
public sealed class WritingPathStep
{
    public Guid PathId { get; set; }
    public int Position { get; set; }
    public Guid QuestionId { get; set; }
}
