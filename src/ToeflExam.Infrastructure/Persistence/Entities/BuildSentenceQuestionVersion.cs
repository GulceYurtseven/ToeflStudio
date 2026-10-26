using ToeflExam.Domain.Content;

namespace ToeflExam.Infrastructure.Persistence.Entities;

/// <summary>Persistence representation; never returned directly to a student.</summary>
public sealed class BuildSentenceQuestionVersion
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public int VersionNumber { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public PublicationStatus PublicationStatus { get; set; } = PublicationStatus.Draft;
    public string TilesJson { get; set; } = string.Empty;
    public string ResponsePartsJson { get; set; } = string.Empty;
    public string AcceptedOrdersJson { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}
