using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ToeflExam.Application.Writing;

namespace ToeflExam.Infrastructure.Assessment;

public sealed class GeminiWritingScorer(HttpClient http, IOptions<GeminiOptions> options) : IWritingScorer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        { RespectRequiredConstructorParameters = true, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };
    private static readonly string SystemPrompt = ReadResource("writing-system.txt")
        .Replace("{{RUBRIC_VERSION}}", WritingPracticeService.RubricVersion, StringComparison.Ordinal);
    private static readonly JsonElement Schema = JsonDocument.Parse(ReadResource("writing-feedback.schema.json")).RootElement.Clone();

    /// <summary>Requests rubric-based AI feedback and validates the provider result before returning it.</summary>
    public async Task<ScoringResult> ScoreAsync(WritingQuestion question, string answer, CancellationToken ct)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
            throw new WritingScoringException("AI assessment is not configured. Please contact the administrator.", true);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        using var request = new HttpRequestMessage(HttpMethod.Post,
            "https://generativelanguage.googleapis.com/v1beta/models/" + settings.Model + ":generateContent");
        request.Headers.Add("x-goog-api-key", settings.ApiKey);
        request.Content = JsonContent.Create(new
        {
            systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = JsonSerializer.Serialize(new
            {
                task = WritingModules.Slug(question.Kind), question.Instructions, question.Context,
                question.Requirements, question.Posts, studentAnswer = answer
            }) } } } },
            generationConfig = new
            {
                maxOutputTokens = 8192,
                // TextResponseFormat uses an enum, not an IANA MIME string.
                responseFormat = new { text = new { mimeType = "APPLICATION_JSON", schema = Schema } }
            }
        });
        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new WritingScoringException(response.StatusCode switch
                {
                    HttpStatusCode.BadRequest => "The assessment provider rejected the request. The server configuration needs attention.",
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "The assessment provider could not verify access.",
                    HttpStatusCode.TooManyRequests => "The assessment provider's quota or request limit has been reached. Your response is preserved. Please retry later; the administrator may need to check the provider quota.",
                    HttpStatusCode.NotFound => "The configured assessment model is unavailable.",
                    _ => "AI assessment could not be completed."
                }, response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500);
            // Bound the provider response before parsing; never log request/response bodies.
            await response.Content.LoadIntoBufferAsync(128 * 1024, timeout.Token);
            using var envelope = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            if (!envelope.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() != 1)
                throw InvalidResponse();
            var candidate = candidates[0];
            if (!candidate.TryGetProperty("finishReason", out var finish) || finish.GetString() != "STOP")
                throw InvalidResponse(); // Includes safety blocks and truncated output; never turn these into score zero.
            var parts = candidate.GetProperty("content").GetProperty("parts");
            var json = string.Concat(parts.EnumerateArray()
                .Where(p => (!p.TryGetProperty("thought", out var thought) || !thought.GetBoolean()) && p.TryGetProperty("text", out _))
                .Select(p => p.GetProperty("text").GetString()));
            var feedback = JsonSerializer.Deserialize<WritingFeedback>(json, JsonOptions) ?? throw InvalidResponse();
            Validate(feedback, answer);
            var model = envelope.RootElement.TryGetProperty("modelVersion", out var version) ? version.GetString() : settings.Model;
            if (string.IsNullOrWhiteSpace(model) || model.Length > 120) throw InvalidResponse();
            return new(feedback, model);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new WritingScoringException("Assessment timed out. Keep your response and retry.", true); }
        catch (HttpRequestException) { throw new WritingScoringException("The assessment provider could not be reached. Keep your response and retry.", true); }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        { throw InvalidResponse(); }
    }
    /// <summary>Checks sizes, required values and scoring invariants before accepting external data.</summary>
    private static void Validate(WritingFeedback f, string answer)
    {
        if (f.Score is < 0 or > 5 || !Text(f.Rationale, 2000) || f.Strengths is null ||
            f.Strengths.Length > 5 || f.Strengths.Any(s => !Text(s, 1000)) ||
            f.NextSteps is null || f.NextSteps.Length is < 1 or > 5 || f.NextSteps.Any(s => !Text(s, 1000)) ||
            f.Issues is null || f.Issues.Length > 8 || f.ImprovedResponse is null || f.ImprovedResponse.Length > 12000)
            throw InvalidResponse();
        foreach (var issue in f.Issues)
            if (issue is null || !new[] { "task", "organization", "grammar", "vocabulary", "register", "mechanics" }.Contains(issue.Category) ||
                issue.Evidence is null || issue.Evidence.Length > 1200 || !answer.Contains(issue.Evidence, StringComparison.Ordinal) ||
                !Text(issue.Explanation, 1200) || !Text(issue.Suggestion, 1200))
                throw InvalidResponse();
    }
    /// <summary>Checks that feedback text is nonempty and within its allowed size.</summary>
    private static bool Text(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Length <= max;
    /// <summary>Creates a safe provider-response error without exposing raw AI output.</summary>
    private static WritingScoringException InvalidResponse() => new("The provider did not return a valid assessment. Your progress is unchanged; please retry.");
    /// <summary>Loads the embedded assessment prompt or response schema.</summary>
    private static string ReadResource(string name)
    {
        using var stream = typeof(GeminiWritingScorer).Assembly.GetManifestResourceStream(
            "ToeflExam.Infrastructure.Assessment.Prompts." + name) ?? throw new InvalidOperationException("Missing scoring resource.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
