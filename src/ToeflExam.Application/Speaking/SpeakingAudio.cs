using System.Buffers.Binary;

namespace ToeflExam.Application.Speaking;

// Browser normalizes audio to canonical mono 16 kHz PCM16 WAV. No filenames or MIME claims are trusted.
public sealed class SpeakingAudio
{
    public const int MaxBytes = 44 + 46 * 32000;
    private readonly byte[] _bytes;
    public int DurationMilliseconds { get; }
    public string Base64 => Convert.ToBase64String(_bytes);
    /// <summary>Initializes the speaking audio with its required dependencies or validated state.</summary>
    private SpeakingAudio(byte[] bytes) => (_bytes, DurationMilliseconds) = (bytes, (bytes.Length - 44) / 32);
    /// <summary>Validates bounded mono PCM WAV data and computes its duration before transcription.</summary>
    public static SpeakingAudio Parse(string base64, int maxSeconds)
    {
        if (string.IsNullOrEmpty(base64) || base64.Length > 4 * ((MaxBytes + 2) / 3))
            throw new ArgumentException("The recording is missing or too large.");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(base64); }
        catch (FormatException) { throw new ArgumentException("The recording is not valid Base64."); }
        if (bytes.Length < 44 + 3200 || bytes.Length > MaxBytes || bytes.Length % 2 != 0)
            throw new ArgumentException("The recording must last at least 0.1 seconds and stay within the task limit.");
        var s = bytes.AsSpan();
        if (!s[..4].SequenceEqual("RIFF"u8) || !s[8..12].SequenceEqual("WAVE"u8) || !s[12..16].SequenceEqual("fmt "u8) ||
            BinaryPrimitives.ReadUInt32LittleEndian(s[4..8]) != bytes.Length - 8 ||
            BinaryPrimitives.ReadInt32LittleEndian(s[16..20]) != 16 || BinaryPrimitives.ReadInt16LittleEndian(s[20..22]) != 1 ||
            BinaryPrimitives.ReadInt16LittleEndian(s[22..24]) != 1 || BinaryPrimitives.ReadInt32LittleEndian(s[24..28]) != 16000 ||
            BinaryPrimitives.ReadInt32LittleEndian(s[28..32]) != 32000 || BinaryPrimitives.ReadInt16LittleEndian(s[32..34]) != 2 ||
            BinaryPrimitives.ReadInt16LittleEndian(s[34..36]) != 16 || !s[36..40].SequenceEqual("data"u8) ||
            BinaryPrimitives.ReadInt32LittleEndian(s[40..44]) != bytes.Length - 44)
            throw new ArgumentException("The recording must be mono 16 kHz PCM16 WAV.");
        var result = new SpeakingAudio(bytes);
        if (maxSeconds is < 1 or > 45 || result.DurationMilliseconds > maxSeconds * 1000 + 250)
            throw new ArgumentException("The recording exceeds this task's time limit.");
        return result;
    }
}
