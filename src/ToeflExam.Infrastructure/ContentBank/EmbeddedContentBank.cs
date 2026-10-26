using System.Text.Json;
using ToeflExam.Application.ContentBank;

namespace ToeflExam.Infrastructure.ContentBank;

public sealed class EmbeddedContentBank : IContentBank
{
    public static readonly BankEntry[] PracticeEntries = Read("practice-v1.json");
    public static readonly BankEntry[] ExamEntries = Read("exams-v1.json");
    public IReadOnlyList<BankEntry> Practice => PracticeEntries;
    public IReadOnlyList<BankEntry> Exams => ExamEntries;
    /// <summary>Reconstructs validated content from its stored representation.</summary>
    private static BankEntry[] Read(string file)
    {
        using var stream = typeof(EmbeddedContentBank).Assembly.GetManifestResourceStream("ToeflExam.Infrastructure.ContentBank." + file)
            ?? throw new InvalidOperationException("Missing authored content: " + file);
        return JsonSerializer.Deserialize<BankEntry[]>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Invalid authored content.");
    }
}
