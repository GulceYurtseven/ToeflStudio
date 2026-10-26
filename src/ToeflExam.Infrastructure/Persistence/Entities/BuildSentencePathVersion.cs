namespace ToeflExam.Infrastructure.Persistence.Entities;

public sealed class BuildSentencePathVersion
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public List<BuildSentencePathStep> Steps { get; set; } = [];
}

public sealed class BuildSentencePathStep
{
    public Guid PathVersionId { get; set; }
    public int Position { get; set; }
    public Guid QuestionVersionId { get; set; }
}
