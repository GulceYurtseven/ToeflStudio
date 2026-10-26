using System.Text.RegularExpressions;

namespace ToeflExam.Infrastructure.Assessment;

public sealed class GeminiOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-3.1-flash-lite";
    public int TimeoutSeconds { get; set; } = 120;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && ValidModel && TimeoutSeconds is >= 10 and <= 180;
    public bool ValidModel => Model == "gemini-3.1-flash-lite" ||
        (!string.IsNullOrWhiteSpace(Model) && Regex.IsMatch(Model, @"^gemini-[a-zA-Z0-9.-]*pro[a-zA-Z0-9.-]*$")
        && !Model.Contains("image", StringComparison.OrdinalIgnoreCase) && !Model.Contains("tts", StringComparison.OrdinalIgnoreCase));
}
