using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ToeflExam.Application.Speaking;
using ToeflExam.Domain.Speaking;
using ToeflExam.Infrastructure.Assessment;

namespace ToeflExam.IntegrationTests;

public sealed class SpeakingProviderTests
{
    internal static string Wave(int milliseconds = 1000)
    {
        var data = new byte[44 + milliseconds * 32]; var span = data.AsSpan();
        "RIFF"u8.CopyTo(span); BinaryPrimitives.WriteInt32LittleEndian(span[4..], data.Length-8); "WAVEfmt "u8.CopyTo(span[8..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[16..],16); BinaryPrimitives.WriteInt16LittleEndian(span[20..],1);
        BinaryPrimitives.WriteInt16LittleEndian(span[22..],1); BinaryPrimitives.WriteInt32LittleEndian(span[24..],16000);
        BinaryPrimitives.WriteInt32LittleEndian(span[28..],32000); BinaryPrimitives.WriteInt16LittleEndian(span[32..],2);
        BinaryPrimitives.WriteInt16LittleEndian(span[34..],16); "data"u8.CopyTo(span[36..]); BinaryPrimitives.WriteInt32LittleEndian(span[40..],data.Length-44);
        return Convert.ToBase64String(data);
    }
    [Fact]
    public void Audio_validation_checks_real_header_length_format_and_duration()
    {
        Assert.Equal(1000, SpeakingAudio.Parse(Wave(),8).DurationMilliseconds);
        Assert.Throws<ArgumentException>(() => SpeakingAudio.Parse("not audio",8));
        Assert.Throws<ArgumentException>(() => SpeakingAudio.Parse(Wave(9000),8));
        Assert.Throws<ArgumentException>(() => SpeakingAudio.Parse(Wave(50),8));
        var bytes = Convert.FromBase64String(Wave()); bytes[22] = 2;
        Assert.Throws<ArgumentException>(() => SpeakingAudio.Parse(Convert.ToBase64String(bytes),8));
        bytes = Convert.FromBase64String(Wave()); bytes[40] = 1;
        Assert.Throws<ArgumentException>(() => SpeakingAudio.Parse(Convert.ToBase64String(bytes),8));
    }
    [Theory]
    [InlineData("ok")] [InlineData("unassessable")] [InlineData("bad-score")] [InlineData("truncated")] [InlineData("quota")]
    public async Task Scoring_receives_only_transcript_and_rejects_invalid_or_failed_scoring(string condition)
    {
        var handler = new Handler(async request =>
        {
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync()); var root = json.RootElement;
            Assert.Equal("key-for-tests", request.Headers.GetValues("x-goog-api-key").Single());
            Assert.DoesNotContain("key-for-tests", request.RequestUri!.ToString());
            Assert.Contains(SpeakingPracticeService.RubricVersion,root.GetProperty("systemInstruction").ToString());
            Assert.Contains("untrusted",root.GetProperty("systemInstruction").ToString());
            var parts = root.GetProperty("contents")[0].GetProperty("parts");
            Assert.Equal(1,parts.GetArrayLength());
            Assert.DoesNotContain("inlineData", root.ToString());
            Assert.Contains("Welcome", parts[0].GetProperty("text").GetString());
            if(condition == "quota") return new(HttpStatusCode.TooManyRequests) { Content = new StringContent("secret quota payload") };
            var feedback = new { assessable = condition != "unassessable", score = condition == "bad-score" ? 9 : 4,
                rationale = "İçerik korunmuş.", strengths = new[] { "İçerik." }, issues = Array.Empty<object>(), nextSteps = new[] { "Sonraki cümleyi dene." } };
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { candidates = new[] { new {
                finishReason = condition == "truncated" ? "MAX_TOKENS" : "STOP", content = new { parts = new[] { new { text = JsonSerializer.Serialize(feedback) } } }
            } } })) };
        });
        var scorer = new GeminiSpeakingScorer(new HttpClient(handler), Options.Create(new GeminiOptions { ApiKey="key-for-tests" }), new Transcriber());
        var question = new SpeakingQuestion(Guid.NewGuid(),SpeakingTaskKind.ListenRepeat,"Practice","Repeat","Campus","Welcome","1-1-v1.wav",8,1);
        if(condition == "ok") Assert.Equal(4,(await scorer.ScoreAsync(question,SpeakingAudio.Parse(Wave(),8),default)).Feedback.Score);
        else { var error = await Assert.ThrowsAsync<SpeakingScoringException>(()=>scorer.ScoreAsync(question,SpeakingAudio.Parse(Wave(),8),default)); Assert.DoesNotContain("secret",error.Message); }
        Assert.Equal(1,handler.Calls);
    }
    private sealed class Handler(Func<HttpRequestMessage,Task<HttpResponseMessage>> response) : HttpMessageHandler
    { public int Calls; protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) { Calls++; return response(request); } }
    private sealed class Transcriber : ISpeechTranscriber
    { public Task<SpeechTranscript> TranscribeAsync(SpeakingAudio audio, CancellationToken ct) => Task.FromResult(new SpeechTranscript("Welcome", "test-asr")); }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task Transcription_is_separate_verbatim_and_has_no_reference(bool usable)
    {
        var handler = new Handler(async request => {
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("inlineData", body); Assert.DoesNotContain("referencePrompt", body);
            Assert.DoesNotContain("Campus", body);
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { candidates = new[] { new {
                finishReason = "STOP", content = new { parts = new[] { new { text = JsonSerializer.Serialize(new { usable, transcript = "I has a [inaudible]" }) } } }
            } } }) };
        });
        var transcriber = new GeminiSpeechTranscriber(new HttpClient(handler), Options.Create(new GeminiOptions { ApiKey = "test" }));
        if (usable) Assert.Equal("I has a [inaudible]", (await transcriber.TranscribeAsync(SpeakingAudio.Parse(Wave(),8),default)).Text);
        else await Assert.ThrowsAsync<SpeakingScoringException>(() => transcriber.TranscribeAsync(SpeakingAudio.Parse(Wave(),8),default));
    }
}

public sealed class SpeakingSqlTests(SqlPracticeFixture fixture) : IClassFixture<SqlPracticeFixture>
{
    [SqlServerFact] public Task Repeat_journey() => Journey("listen-repeat",7);
    [SqlServerFact] public Task Interview_journey() => Journey("interview",4);
    private async Task Journey(string module,int total)
    {
        var scorer = new Scorer(); await using var app = App(scorer); using var client = await Register(app); using var other = await Register(app);
        await fixture.PinLegacyRun(client,"speaking",module);
        var root = "/api/practice/speaking/"+module+"/runs";
        using var anonymous = app.CreateClient(new(){BaseAddress=new Uri("https://localhost")});
        Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.PostAsync(root,null)).StatusCode);
        var run = await Read<SpeakingRunDto>(await client.PostAsync(root,null)); var url=root+"/"+run.Id;
        Assert.Equal(total,run.TotalSteps);
        Assert.Equal(run.Id,(await Read<SpeakingRunDto>(await client.PostAsync(root,null))).Id);
        Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync(url+"/steps/1/audio")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.GetAsync(url+"/steps/2/audio")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsync(url+"/restart",null)).StatusCode);
        var initial=await client.GetStringAsync(url+"/steps/1"); Assert.DoesNotContain("\"prompt\"",initial); Assert.DoesNotContain("audioFile",initial);
        var directions = JsonDocument.Parse(initial).RootElement.GetProperty("question").GetProperty("instructions").GetString()!;
        Assert.StartsWith("You are", directions);
        Assert.DoesNotMatch("[çğıöşüÇĞİÖŞÜ]", directions);
        var audio=await client.GetAsync(url+"/steps/1/audio"); Assert.Equal(HttpStatusCode.OK,audio.StatusCode);
        Assert.Equal("audio/wav",audio.Content.Headers.ContentType!.MediaType); Assert.True((await audio.Content.ReadAsByteArrayAsync()).Length>1000);
        Assert.True(audio.Headers.CacheControl!.NoStore);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(url+"/steps/1/submissions",new{submissionId=Guid.NewGuid(),audioBase64="bad",skip=false})).StatusCode);
        for(var p=1;p<=total;p++)
        {
            var id=Guid.NewGuid(); var request=new{submissionId=id,audioBase64=p==1?SpeakingProviderTests.Wave():"",skip=p!=1};
            var result=await Read<SpeakingSubmission>(await client.PostAsJsonAsync(url+$"/steps/{p}/submissions",request));
            Assert.Equal(p,result.Progress.CompletedSteps); Assert.Equal(p==1?4:0,result.Report!.Feedback.Score);
            if (p == 1)
            {
                var retry=await Read<SpeakingSubmission>(await client.PostAsJsonAsync(url+$"/steps/{p}/submissions",request));
                Assert.Equal(result.Report.Feedback.Score,retry.Report!.Feedback.Score);
            }
        }
        Assert.Equal(1,scorer.Calls);
        Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync(url+"/steps/1/report")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(url+"/steps/1/report")).StatusCode);
        var next=await Read<SpeakingRunDto>(await client.PostAsync(url+"/restart",null)); Assert.Equal(0,next.CompletedSteps);
        Assert.Equal(module=="listen-repeat"?21:40,next.TotalSteps);
        Assert.Equal(2,(await client.GetFromJsonAsync<SpeakingRunDto[]>(root))!.Length);
        await using var db=fixture.CreateContext(); var fields=db.Model.FindEntityType(typeof(SpeakingStep))!.GetProperties().Select(p=>p.Name).ToArray();
        Assert.DoesNotContain("Audio",fields); Assert.DoesNotContain("Transcript",fields); Assert.DoesNotContain("Feedback",fields);
        var question=await db.SpeakingQuestions.FirstAsync(); question.Prompt="changed";
        await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());
    }
    [SqlServerFact]
    public async Task Failure_releases_claim_and_concurrent_retry_never_calls_provider_twice()
    {
        var scorer=new Scorer{Fail=true}; await using var app=App(scorer); using var client=await Register(app);
        var root="/api/practice/speaking/listen-repeat/runs";
        var run=await Read<SpeakingRunDto>(await client.PostAsync(root,null)); var url=root+$"/{run.Id}/steps/1/submissions";
        var body=new{submissionId=Guid.NewGuid(),audioBase64=SpeakingProviderTests.Wave(),skip=false};
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(url,body)).StatusCode);
        Assert.Equal(0,(await client.GetFromJsonAsync<SpeakingRunDto>(root+"/"+run.Id))!.CompletedSteps);
        scorer.Fail=false; scorer.Gate=new(TaskCreationOptions.RunContinuationsAsynchronously); scorer.Entered=new(TaskCreationOptions.RunContinuationsAsynchronously);
        var first=client.PostAsJsonAsync(url,body); await scorer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(url,body)).StatusCode);
        scorer.Gate.SetResult(); Assert.Equal(HttpStatusCode.OK,(await first).StatusCode); Assert.Equal(2,scorer.Calls);
    }
    private WebApplicationFactory<Program> App(Scorer scorer) => fixture.Factory.WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.RemoveAll<ISpeakingScorer>();s.AddSingleton<ISpeakingScorer>(scorer);}));
    private static async Task<HttpClient> Register(WebApplicationFactory<Program> app)
    {
        var client=app.CreateClient(new(){BaseAddress=new Uri("https://localhost")}); var credentials=new{username="student_"+Guid.NewGuid().ToString("N")[..20],email=$"s-{Guid.NewGuid():N}@example.test",password="Speaking!9842"};
        (await client.PostAsJsonAsync("/api/auth/register",credentials)).EnsureSuccessStatusCode();
        var tokens=await Read<JsonElement>(await client.PostAsJsonAsync("/api/auth/login?useCookies=false",credentials));
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",tokens.GetProperty("accessToken").GetString()); return client;
    }
    private static async Task<T> Read<T>(HttpResponseMessage response) { Assert.True(response.IsSuccessStatusCode,await response.Content.ReadAsStringAsync());return (await response.Content.ReadFromJsonAsync<T>())!; }
    private sealed class Scorer : ISpeakingScorer
    {
        public int Calls; public bool Fail; public TaskCompletionSource? Gate; public TaskCompletionSource Entered=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<SpeakingScoringResult> ScoreAsync(SpeakingQuestion q,SpeakingAudio audio,CancellationToken ct)
        { Interlocked.Increment(ref Calls); Entered.TrySetResult(); if(Gate is not null)await Gate.Task.WaitAsync(ct); if(Fail)throw new SpeakingScoringException("Temporary failure",true);
            return new(new(4,"Original spoken response","Test feedback",["Clear."],[],["Practice." ]),"test-double"); }
    }
}
