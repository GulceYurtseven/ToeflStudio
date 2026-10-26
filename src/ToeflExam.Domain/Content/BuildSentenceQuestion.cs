using ToeflExam.Domain.Assessment;

namespace ToeflExam.Domain.Content;

/// <summary>
/// An immutable question snapshot including its publication status.
/// Publication transitions, versioning and persistence are separate concerns.
/// The answer key is held privately; student-facing API contracts must be separate DTOs.
/// </summary>
public sealed class BuildSentenceQuestion
{
    private readonly BuildSentenceScoringRule _scoringRule;

    /// <summary>Initializes the build sentence question with its required dependencies or validated state.</summary>
    public BuildSentenceQuestion(
        Guid id,
        string prompt,
        IEnumerable<SentenceTile> tiles,
        IEnumerable<IEnumerable<int>> acceptedOrders,
        IEnumerable<SentencePart>? responseParts = null,
        PublicationStatus publicationStatus = PublicationStatus.Draft)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A question must have an identifier.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(tiles);
        if (!Enum.IsDefined(publicationStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(publicationStatus));
        }

        var tileSnapshot = tiles.ToArray();
        if (tileSnapshot.Any(tile => tile is null))
        {
            throw new ArgumentException("Tiles must not contain null entries.", nameof(tiles));
        }

        // Reuse answer-key validation rather than maintaining a second set of scoring rules.
        _scoringRule = new BuildSentenceScoringRule(tileSnapshot.Select(tile => tile.Id), acceptedOrders);

        // Omitting the layout preserves questions whose entire answer is built from tiles.
        var partSnapshot = responseParts?.ToArray()
            ?? Enumerable.Range(0, _scoringRule.SlotCount).Select(_ => SentencePart.Slot()).ToArray();

        if (partSnapshot.Any(part => part is null) ||
            partSnapshot.Count(part => part.IsSlot) != _scoringRule.SlotCount)
        {
            throw new ArgumentException(
                "The response layout must contain non-null parts and exactly one slot per answer-key tile.",
                nameof(responseParts));
        }

        Id = id;
        Prompt = prompt;
        Tiles = Array.AsReadOnly(tileSnapshot);
        ResponseParts = Array.AsReadOnly(partSnapshot);
        PublicationStatus = publicationStatus;
    }

    public Guid Id { get; }
    public string Prompt { get; }
    public PublicationStatus PublicationStatus { get; }
    public IReadOnlyList<SentenceTile> Tiles { get; }
    /// <summary>Display order; submitted identifiers fill slots from left to right, skipping fixed text.</summary>
    public IReadOnlyList<SentencePart> ResponseParts { get; }
    public int SlotCount => _scoringRule.SlotCount;
    public int MaxRawScore => _scoringRule.MaxRawScore;

    /// <summary>Computes an objective raw score using the validated answer key.</summary>
    public int Score(IEnumerable<int> submittedTileIds) => _scoringRule.Score(submittedTileIds);
}
