using System.Text.Json;
using ToeflExam.Application.Content;
using ToeflExam.Application.Content.GetBuildSentenceQuestion;
using ToeflExam.Domain.Content;

namespace ToeflExam.Application.Tests;

public sealed class GetBuildSentenceQuestionTests
{
    [Fact]
    public async Task Student_payload_preserves_layout_and_contains_only_allowed_fields()
    {
        var question = CreateQuestion();
        var handler = new GetBuildSentenceQuestionHandler(new StubReader(question));

        var result = await handler.HandleAsync(question.Id);

        Assert.NotNull(result);
        Assert.Equal(question.Id, result.Id);
        Assert.Equal("The student ", result.ResponseParts[0].Text);
        Assert.True(result.ResponseParts[1].IsSlot);
        Assert.Null(result.ResponseParts[1].Text);
        Assert.Equal(" every day.", result.ResponseParts[2].Text);
        Assert.Equal(1, result.SlotCount);
        Assert.Equal(new[] { 20, 10 }, result.Tiles.Select(tile => tile.Id));

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        AssertFields(json.RootElement, "id", "prompt", "tiles", "responseParts", "slotCount");
        foreach (var tile in json.RootElement.GetProperty("tiles").EnumerateArray())
            AssertFields(tile, "id", "text");
        foreach (var part in json.RootElement.GetProperty("responseParts").EnumerateArray())
            AssertFields(part, "isSlot", "text");
    }

    [Fact]
    public async Task Changing_the_answer_key_does_not_change_the_student_payload()
    {
        var first = CreateQuestion();
        var second = new BuildSentenceQuestion(first.Id, first.Prompt, first.Tiles, [new[] { 20 }], first.ResponseParts,
            PublicationStatus.Published);
        var a = await new GetBuildSentenceQuestionHandler(new StubReader(first)).HandleAsync(first.Id);
        var b = await new GetBuildSentenceQuestionHandler(new StubReader(second)).HandleAsync(second.Id);

        Assert.Equal(JsonSerializer.Serialize(a), JsonSerializer.Serialize(b));
    }

    [Fact]
    public async Task Missing_question_returns_null()
    {
        var handler = new GetBuildSentenceQuestionHandler(new StubReader(null));
        Assert.Null(await handler.HandleAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Empty_identifier_is_rejected_before_reading()
    {
        var reader = new StubReader(null);
        await Assert.ThrowsAsync<ArgumentException>(() => new GetBuildSentenceQuestionHandler(reader).HandleAsync(Guid.Empty));
        Assert.False(reader.WasCalled);
    }

    [Fact]
    public async Task Cancelled_request_does_not_read_content()
    {
        var reader = new StubReader(null);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new GetBuildSentenceQuestionHandler(reader).HandleAsync(Guid.NewGuid(), cancellation.Token));
        Assert.False(reader.WasCalled);
    }

    [Fact]
    public async Task Reader_receives_the_requested_identifier_and_cancellation_token()
    {
        var reader = new StubReader(null);
        var id = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        await new GetBuildSentenceQuestionHandler(reader).HandleAsync(id, cancellation.Token);
        Assert.Equal(id, reader.RequestedId);
        Assert.Equal(cancellation.Token, reader.ReceivedToken);
    }

    [Theory]
    [InlineData(PublicationStatus.Draft)]
    [InlineData(PublicationStatus.InReview)]
    [InlineData(PublicationStatus.Retired)]
    public async Task Non_published_content_is_not_returned_to_students(PublicationStatus status)
    {
        var question = CreateQuestion(status);
        var handler = new GetBuildSentenceQuestionHandler(new StubReader(question));

        Assert.Null(await handler.HandleAsync(question.Id));
    }

    [Fact]
    public async Task New_question_is_not_visible_without_explicit_publication()
    {
        var question = new BuildSentenceQuestion(Guid.NewGuid(), "Arrange the sentence.",
            [new(1, "Students"), new(2, "read")], [new[] { 1, 2 }]);

        Assert.Null(await new GetBuildSentenceQuestionHandler(new StubReader(question)).HandleAsync(question.Id));
    }

    private static BuildSentenceQuestion CreateQuestion(PublicationStatus status = PublicationStatus.Published) => new(
        Guid.NewGuid(), "Complete the sentence.", [new(20, "study"), new(10, "studies")],
        [new[] { 10 }], [SentencePart.Fixed("The student "), SentencePart.Slot(), SentencePart.Fixed(" every day.")], status);

    private static void AssertFields(JsonElement element, params string[] expected) =>
        Assert.Equal(expected.Order(), element.EnumerateObject().Select(property => property.Name).Order());

    private sealed class StubReader(BuildSentenceQuestion? question) : IBuildSentenceQuestionReader
    {
        public bool WasCalled { get; private set; }
        public Guid RequestedId { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public Task<BuildSentenceQuestion?> FindAsync(Guid questionId, CancellationToken cancellationToken)
        {
            WasCalled = true;
            RequestedId = questionId;
            ReceivedToken = cancellationToken;
            return Task.FromResult(question?.Id == questionId ? question : null);
        }
    }
}
