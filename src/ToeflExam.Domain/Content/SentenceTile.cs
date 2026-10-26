namespace ToeflExam.Domain.Content;

/// <summary>A selectable word or phrase. Identity is local to its question.</summary>
public sealed record SentenceTile
{
    /// <summary>Initializes the sentence tile with its required dependencies or validated state.</summary>
    public SentenceTile(int id, string text)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Id = id;
        Text = text;
    }

    public int Id { get; }
    public string Text { get; }
}
