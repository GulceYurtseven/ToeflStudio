namespace ToeflExam.Application.Vocabulary;

/// <summary>A reviewed word or expression with original English learning material and source references.</summary>
public sealed record VocabularyEntry(string Id, string Word, string PartOfSpeech, string Meaning,
    string ExampleSentence, IReadOnlyList<string> SourceIds, IReadOnlyList<string> Aliases);

/// <summary>Identifies an input source and its verified teaching targets without publishing source files.</summary>
public sealed record VocabularySource(string Id, string Title, string? Url, string Coverage,
    string Note, IReadOnlyList<string> WordIds);

/// <summary>A versioned, complete catalog shared by browsing and the in-page flashcard.</summary>
public sealed record VocabularyCatalog(string Version, IReadOnlyList<VocabularyEntry> Words,
    IReadOnlyList<VocabularySource> Sources);

/// <summary>Reads the curated vocabulary bank independently of student answers or SQL storage.</summary>
public interface IVocabularyCatalog
{
    VocabularyCatalog Catalog { get; }
}
