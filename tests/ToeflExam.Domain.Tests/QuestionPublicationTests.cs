using ToeflExam.Domain.Content;

namespace ToeflExam.Domain.Tests;

public sealed class QuestionPublicationTests
{
    [Fact]
    public void Unknown_publication_state_cannot_enter_the_domain()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BuildSentenceQuestion(
            Guid.NewGuid(), "Arrange the sentence.", [new(1, "Students"), new(2, "read")],
            [new[] { 1, 2 }], publicationStatus: (PublicationStatus)99));
    }

    [Fact]
    public void Retired_content_can_still_score_a_previously_assigned_response()
    {
        var question = new BuildSentenceQuestion(
            Guid.NewGuid(), "Arrange the sentence.", [new(1, "Students"), new(2, "read")],
            [new[] { 1, 2 }], publicationStatus: PublicationStatus.Retired);

        Assert.Equal(1, question.Score([1, 2]));
    }
}
