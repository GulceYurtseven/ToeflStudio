using System.Text.Json;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Domain.Content;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Infrastructure.Persistence;

internal static class BuildSentenceQuestionMapper
{
    internal sealed record Tile(int Id, string Text);
    internal sealed record Part(bool IsSlot, string? Text);

    /// <summary>Reconstructs validated content from its stored representation.</summary>
    public static QuestionContent Read(BuildSentenceQuestionVersion row)
    {
        var tiles = JsonSerializer.Deserialize<Tile[]>(row.TilesJson) ?? throw new InvalidOperationException("Invalid tiles.");
        var parts = JsonSerializer.Deserialize<Part[]>(row.ResponsePartsJson) ?? throw new InvalidOperationException("Invalid layout.");
        var orders = JsonSerializer.Deserialize<int[][]>(row.AcceptedOrdersJson) ?? throw new InvalidOperationException("Invalid key.");
        var question = new BuildSentenceQuestion(row.Id, row.Prompt, tiles.Select(tile => new SentenceTile(tile.Id, tile.Text)),
            orders, parts.Select(part => part.IsSlot ? SentencePart.Slot() : SentencePart.Fixed(part.Text!)), row.PublicationStatus);
        return new(question, orders, row.Explanation);
    }
}
