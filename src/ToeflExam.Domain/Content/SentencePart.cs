namespace ToeflExam.Domain.Content;

/// <summary>A fixed text fragment or one slot accepting one word/phrase tile.</summary>
public sealed record SentencePart
{
    /// <summary>Initializes the sentence part with its required dependencies or validated state.</summary>
    private SentencePart(string? text) => Text = text;

    public string? Text { get; }
    public bool IsSlot => Text is null;

    /// <summary>Creates an empty response slot to be filled with a sentence tile.</summary>
    public static SentencePart Slot() => new(text: null);

    /// <summary>Creates fixed sentence text that is not part of the submitted tile order.</summary>
    public static SentencePart Fixed(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return new SentencePart(text);
    }
}
