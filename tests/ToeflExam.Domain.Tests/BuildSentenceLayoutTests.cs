using ToeflExam.Domain.Content;

namespace ToeflExam.Domain.Tests;

public sealed class BuildSentenceLayoutTests
{
    [Fact]
    public void Fixed_text_does_not_consume_answer_tiles_or_change_scoring()
    {
        var question = new BuildSentenceQuestion(
            Guid.NewGuid(), "Complete the sentence.",
            [new(1, "studies"), new(2, "at the library"), new(3, "study")],
            [new[] { 1, 2 }],
            [SentencePart.Fixed("The student "), SentencePart.Slot(),
             SentencePart.Fixed(" every day "), SentencePart.Slot(), SentencePart.Fixed(".")]);

        Assert.Equal(5, question.ResponseParts.Count);
        Assert.Equal(2, question.SlotCount);
        Assert.Equal(1, question.Score([1, 2]));
        Assert.Equal(0, question.Score([2, 1]));
        Assert.Equal(0, question.Score([3, 2]));
    }

    [Fact]
    public void Omitted_layout_creates_only_slots_for_existing_questions()
    {
        var question = new BuildSentenceQuestion(
            Guid.NewGuid(), "Arrange the sentence.",
            [new(1, "Students"), new(2, "read")], [new[] { 1, 2 }]);

        Assert.Equal(2, question.ResponseParts.Count);
        Assert.All(question.ResponseParts, part => Assert.True(part.IsSlot));
        Assert.Equal(1, question.Score([1, 2]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Layout_with_wrong_number_of_slots_is_rejected(int slotCount)
    {
        var parts = Enumerable.Range(0, slotCount).Select(_ => SentencePart.Slot());
        Assert.Throws<ArgumentException>(() => new BuildSentenceQuestion(
            Guid.NewGuid(), "Arrange the sentence.",
            [new(1, "Students"), new(2, "read")], [new[] { 1, 2 }], parts));
    }

    [Fact]
    public void Null_layout_part_is_rejected_even_when_slot_count_matches()
    {
        Assert.Throws<ArgumentException>(() => new BuildSentenceQuestion(
            Guid.NewGuid(), "Complete the sentence.", [new(1, "read")],
            [new[] { 1 }], [SentencePart.Slot(), null!]));
    }

    [Fact]
    public void Supplied_layout_cannot_be_changed_after_question_creation()
    {
        SentencePart[] parts = [SentencePart.Fixed("Students "), SentencePart.Slot(), SentencePart.Fixed(".")];
        var question = new BuildSentenceQuestion(
            Guid.NewGuid(), "Complete the sentence.", [new(1, "read")], [new[] { 1 }], parts);

        parts[1] = SentencePart.Fixed("write");

        Assert.True(question.ResponseParts[1].IsSlot);
        Assert.Equal("Students ", question.ResponseParts[0].Text);
        Assert.Throws<NotSupportedException>(() => ((IList<SentencePart>)question.ResponseParts)[1] = parts[1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_fixed_text_is_rejected(string? text)
    {
        Assert.ThrowsAny<ArgumentException>(() => SentencePart.Fixed(text!));
    }
}
