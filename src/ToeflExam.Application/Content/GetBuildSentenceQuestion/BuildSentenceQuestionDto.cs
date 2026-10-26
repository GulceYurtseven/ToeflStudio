namespace ToeflExam.Application.Content.GetBuildSentenceQuestion;

// Explicit allow-list: no domain entities, answer keys or scoring definitions.
public sealed record BuildSentenceQuestionDto(
    Guid Id,
    string Prompt,
    IReadOnlyList<SentenceTileDto> Tiles,
    IReadOnlyList<SentencePartDto> ResponseParts,
    int SlotCount);

public sealed record SentenceTileDto(int Id, string Text);

public sealed record SentencePartDto(bool IsSlot, string? Text);
