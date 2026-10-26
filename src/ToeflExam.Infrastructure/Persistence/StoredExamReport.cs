using System.IO.Compression;
using System.Text.Json;
using ToeflExam.Application.ContentBank;

namespace ToeflExam.Infrastructure.Persistence;

/// <summary>Stores the visible report, resumable AI inputs and study advice without exposing raw audio in API responses.</summary>
public sealed record StoredExamReport(ExamReport Report, Dictionary<int, BankAnswer> Answers,
    Dictionary<int, JsonElement> Feedback, ExamStudyPlan? StudyPlan)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Compresses the single report snapshot to limit SQL storage costs.</summary>
    public byte[] Encode()
    {
        using var output = new MemoryStream();
        using (var compressed = new BrotliStream(output, CompressionLevel.Fastest, leaveOpen: true))
            JsonSerializer.Serialize(compressed, this, Json);
        return output.ToArray();
    }

    /// <summary>Rehydrates a server-written snapshot, bounding decompression memory.</summary>
    public static StoredExamReport Decode(byte[] payload)
    {
        using var input = new MemoryStream(payload, writable: false);
        using var compressed = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = compressed.Read(buffer)) > 0)
        {
            if (output.Length + read > 64L * 1024 * 1024) throw new InvalidDataException("Invalid report size.");
            output.Write(buffer, 0, read);
        }
        output.Position = 0;
        return JsonSerializer.Deserialize<StoredExamReport>(output, Json) ?? throw new InvalidDataException("Invalid report snapshot.");
    }
}
