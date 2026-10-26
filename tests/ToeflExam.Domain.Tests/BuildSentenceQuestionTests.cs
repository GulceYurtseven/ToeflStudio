using ToeflExam.Domain.Content;

namespace ToeflExam.Domain.Tests;

public sealed class BuildSentenceQuestionTests
{
    [Fact]
    public void Question_scores_phrases_and_does_not_require_distractors()
    {
        var question = new BuildSentenceQuestion(
            Guid.NewGuid(), "What did the student say?",
            [new(1, "I"), new(2, "study"), new(3, "every day"), new(4, "studies")],
            [new[] { 1, 2, 3 }]);

        Assert.Equal(3, question.SlotCount);
        Assert.Equal(1, question.Score([1, 2, 3]));
        Assert.Equal(0, question.Score([1, 4, 3]));
    }

    [Fact]
    public void Identical_words_can_be_distinct_tiles()
    {
        var question = new BuildSentenceQuestion(
            Guid.NewGuid(), "Arrange the sentence.",
            [new(1, "The"), new(2, "student"), new(3, "opened"), new(4, "the"), new(5, "book")],
            [new[] { 1, 2, 3, 4, 5 }]);

        Assert.Equal(1, question.Score([1, 2, 3, 4, 5]));
        Assert.Throws<ArgumentException>(() => question.Score([1, 2, 3, 1, 5]));
    }

    [Fact]
    public void Changing_the_supplied_tiles_cannot_corrupt_a_question()
    {
        SentenceTile[] tiles = [new(1, "Students"), new(2, "read")];
        var question = new BuildSentenceQuestion(Guid.NewGuid(), "Arrange the sentence.", tiles, [new[] { 1, 2 }]);

        tiles[0] = new SentenceTile(99, "Changed");

        Assert.Equal("Students", question.Tiles[0].Text);
        Assert.Equal(1, question.Score([1, 2]));
        Assert.Throws<NotSupportedException>(() => ((IList<SentenceTile>)question.Tiles)[0] = tiles[0]);
    }

    [Fact]
    public void Answer_key_cannot_reference_a_tile_from_another_question()
    {
        Assert.Throws<ArgumentException>(() => new BuildSentenceQuestion(
            Guid.NewGuid(), "Arrange the sentence.", [new(1, "Students"), new(2, "read")],
            [new[] { 1, 99 }]));
    }

    [Fact]
    public void Duplicate_tile_identifiers_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new BuildSentenceQuestion(
            Guid.NewGuid(), "Arrange the sentence.", [new(1, "Students"), new(1, "read")],
            [new[] { 1 }]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_prompt_or_tile_text_is_rejected(string text)
    {
        Assert.Throws<ArgumentException>(() => new SentenceTile(1, text));
        Assert.Throws<ArgumentException>(() => new BuildSentenceQuestion(
            Guid.NewGuid(), text, [new(1, "Read")], [new[] { 1 }]));
    }
}
