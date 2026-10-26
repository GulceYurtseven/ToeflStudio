using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ToeflExam.Application.ContentBank;
using ToeflExam.Application.Writing;
using ToeflExam.Infrastructure.Assessment;

namespace ToeflExam.IntegrationTests;

public sealed class ExamCoachTests
{
    private static readonly string[] Skills = ["reading", "listening", "writing", "speaking"];
    private static ExamReport Report => new(Guid.NewGuid(), "mock-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30),
        Skills.Select(s => new SectionResult(s, 0, 1, 0)).ToArray(),
        Skills.Select((s, i) => (object)new { section = s, position = i + 1, score = 0, maximum = 1, answer = "sample", feedback = "incorrect" }).ToArray(), [], false);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Guidance_requires_real_same_section_evidence(bool fabricated)
    {
        var plan = new ExamStudyPlan(Skills.Select((s, i) => new SectionStudyPlan(s, "Review the observed error.",
            [new("Accuracy", "The response did not match the key.", [fabricated ? 99 : i + 1], "Try five related items.", "Explain four correct answers.")])).ToArray());
        var envelope = JsonSerializer.Serialize(new { candidates = new[] { new { finishReason = "STOP", content = new { parts = new[] { new { text = JsonSerializer.Serialize(plan, new JsonSerializerOptions(JsonSerializerDefaults.Web)) } } } } } });
        var scorer = new GeminiExamCoach(new HttpClient(new Stub(HttpStatusCode.OK, envelope)), Options.Create(new GeminiOptions { ApiKey = "test" }));
        if (fabricated) await Assert.ThrowsAsync<WritingScoringException>(() => scorer.CreateAsync(Report, default));
        else Assert.Equal(4, (await scorer.CreateAsync(Report, default)).Sections.Length);
    }

    [Fact]
    public async Task Provider_failure_returns_no_fabricated_plan()
    {
        var scorer = new GeminiExamCoach(new HttpClient(new Stub(HttpStatusCode.TooManyRequests, "{}")), Options.Create(new GeminiOptions { ApiKey = "test" }));
        var error = await Assert.ThrowsAsync<WritingScoringException>(() => scorer.CreateAsync(Report, default));
        Assert.Contains("request limit", error.Message);
    }

    private sealed class Stub(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
