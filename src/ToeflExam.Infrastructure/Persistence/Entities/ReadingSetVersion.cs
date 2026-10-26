using ToeflExam.Domain.Content;

namespace ToeflExam.Infrastructure.Persistence.Entities;

public sealed class ReadingSetVersion
{
    public Guid Id { get; set; }
    public string Module { get; set; } = "";
    public int Version { get; set; }
    public int Position { get; set; }
    public PublicationStatus PublicationStatus { get; set; }
    public string Title { get; set; } = "";
    public string Format { get; set; } = "";
    public string Text { get; set; } = "";
    public string ItemsJson { get; set; } = "[]";
}
