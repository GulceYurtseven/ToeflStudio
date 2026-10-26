namespace ToeflExam.Domain.Assessment;

/// <summary>
/// Scores ordered word/phrase identifiers against an editor-approved answer key.
/// Identifiers belong to individual tiles, so identical words can be separate tiles.
/// </summary>
public sealed class BuildSentenceScoringRule
{
    private readonly HashSet<int> _availableTileIds;
    private readonly int[][] _acceptedOrders;

    /// <summary>Initializes the build sentence scoring rule with its required dependencies or validated state.</summary>
    public BuildSentenceScoringRule(
        IEnumerable<int> availableTileIds,
        IEnumerable<IEnumerable<int>> acceptedOrders)
    {
        ArgumentNullException.ThrowIfNull(availableTileIds);
        ArgumentNullException.ThrowIfNull(acceptedOrders);

        var tiles = availableTileIds.ToArray();
        if (tiles.Length == 0 || tiles.Any(id => id <= 0) || tiles.Distinct().Count() != tiles.Length)
        {
            throw new ArgumentException("Tile identifiers must be positive, unique and non-empty.", nameof(availableTileIds));
        }

        _availableTileIds = tiles.ToHashSet();
        _acceptedOrders = acceptedOrders.Select(order =>
        {
            ArgumentNullException.ThrowIfNull(order);
            return order.ToArray();
        }).ToArray();

        if (_acceptedOrders.Length == 0)
        {
            throw new ArgumentException("At least one accepted order is required.", nameof(acceptedOrders));
        }

        var slotCount = _acceptedOrders[0].Length;
        if (slotCount == 0 || _acceptedOrders.Any(order =>
            order.Length != slotCount ||
            order.Distinct().Count() != order.Length ||
            order.Any(id => !_availableTileIds.Contains(id))))
        {
            throw new ArgumentException(
                "Accepted orders must fill the same non-zero number of slots using distinct available tiles.",
                nameof(acceptedOrders));
        }
    }

    public int MaxRawScore => 1;

    public int SlotCount => _acceptedOrders[0].Length;

    /// <summary>
    /// A valid complete match earns one point; blank, incomplete or incorrect orders earn zero.
    /// Unknown or repeated tile identifiers are invalid input, not a language proficiency score.
    /// </summary>
    public int Score(IEnumerable<int> submittedTileIds)
    {
        ArgumentNullException.ThrowIfNull(submittedTileIds);
        var submitted = submittedTileIds.ToArray();

        if (submitted.Any(id => !_availableTileIds.Contains(id)) ||
            submitted.Distinct().Count() != submitted.Length)
        {
            throw new ArgumentException("A response must use distinct available tiles.", nameof(submittedTileIds));
        }

        return _acceptedOrders.Any(order => order.SequenceEqual(submitted)) ? MaxRawScore : 0;
    }
}
