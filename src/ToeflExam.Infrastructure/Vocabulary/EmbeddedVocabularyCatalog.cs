using System.Text.Json;
using ToeflExam.Application.Vocabulary;

namespace ToeflExam.Infrastructure.Vocabulary;

/// <summary>Loads reviewed vocabulary once and rejects missing content or broken provenance links.</summary>
public sealed class EmbeddedVocabularyCatalog : IVocabularyCatalog
{
    public VocabularyCatalog Catalog { get; }

    /// <summary>Deserializes the embedded catalog and exposes read-only collections to all requests.</summary>
    public EmbeddedVocabularyCatalog()
    {
        using var stream = typeof(EmbeddedVocabularyCatalog).Assembly.GetManifestResourceStream(
            "ToeflExam.Infrastructure.Vocabulary.catalog.json")
            ?? throw new InvalidOperationException("The vocabulary catalog is missing.");
        var catalog = JsonSerializer.Deserialize<VocabularyCatalog>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("The vocabulary catalog is invalid.");
        Validate(catalog);
        Catalog = catalog with
        {
            Words = Array.AsReadOnly(catalog.Words.OrderBy(x => x.Word, StringComparer.OrdinalIgnoreCase)
                .Select(x => x with { SourceIds = Array.AsReadOnly(x.SourceIds.ToArray()), Aliases = Array.AsReadOnly(x.Aliases.ToArray()) }).ToArray()),
            Sources = Array.AsReadOnly(catalog.Sources.Select(x => x with
                { WordIds = Array.AsReadOnly(x.WordIds.ToArray()) }).ToArray())
        };
    }

    /// <summary>Checks content integrity and two-way source coverage before serving any learning material.</summary>
    private static void Validate(VocabularyCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(catalog.Version) || catalog.Words.Count == 0 || catalog.Sources.Count == 0)
            throw new InvalidOperationException("The vocabulary catalog must contain words and sources.");
        var words = catalog.Words.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var sources = catalog.Sources.ToDictionary(x => x.Id, StringComparer.Ordinal);
        if (catalog.Words.Select(x => x.Word).Distinct(StringComparer.OrdinalIgnoreCase).Count() != words.Count)
            throw new InvalidOperationException("Vocabulary headwords must be unique.");
        foreach (var word in catalog.Words)
        {
            if (new[] { word.Id, word.Word, word.PartOfSpeech, word.Meaning, word.ExampleSentence }
                .Any(string.IsNullOrWhiteSpace) || word.SourceIds.Count == 0 || word.SourceIds.Distinct().Count() != word.SourceIds.Count
                || word.SourceIds.Any(id => !sources.TryGetValue(id, out var source) || !source.WordIds.Contains(word.Id)))
                throw new InvalidOperationException($"Vocabulary entry '{word.Id}' has incomplete content or provenance.");
        }
        foreach (var source in catalog.Sources)
        {
            if (string.IsNullOrWhiteSpace(source.Title) || source.Coverage is not ("complete" or "partial")
                || source.WordIds.Distinct().Count() != source.WordIds.Count
                || source.WordIds.Any(id => !words.TryGetValue(id, out var word) || !word.SourceIds.Contains(source.Id))
                || (source.Url is not null && (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) || uri.Scheme != "https")))
                throw new InvalidOperationException($"Vocabulary source '{source.Id}' has invalid coverage or links.");
        }
    }
}
