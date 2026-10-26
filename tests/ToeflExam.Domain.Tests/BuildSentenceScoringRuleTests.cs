using ToeflExam.Domain.Assessment;

namespace ToeflExam.Domain.Tests;

public sealed class BuildSentenceScoringRuleTests
{
    // Tiles: 1=I, 2=study, 3=every day, 4=studies (unused distractor).
    private static BuildSentenceScoringRule CreateRule() =>
        new([1, 2, 3, 4], [new[] { 1, 2, 3 }]);

    [Fact]
    public void Correct_order_earns_one_without_using_the_distractor()
    {
        Assert.Equal(1, CreateRule().Score([1, 2, 3]));
    }

    [Theory]
    [InlineData(new int[] { })]
    [InlineData(new[] { 1, 2 })]
    [InlineData(new[] { 2, 1, 3 })]
    [InlineData(new[] { 1, 4, 3 })]
    [InlineData(new[] { 1, 2, 3, 4 })]
    public void Blank_incomplete_or_incorrect_response_earns_zero(int[] response)
    {
        Assert.Equal(0, CreateRule().Score(response));
    }

    [Fact]
    public void Alternative_order_must_be_explicitly_accepted_by_the_editor()
    {
        var rule = new BuildSentenceScoringRule([1, 2, 3, 4], [new[] { 1, 2, 3 }, new[] { 3, 1, 2 }]);
        Assert.Equal(1, rule.Score([3, 1, 2]));
        Assert.Equal(0, CreateRule().Score([3, 1, 2]));
    }

    [Theory]
    [InlineData(new[] { 1, 99, 3 })]
    [InlineData(new[] { 1, 1, 3 })]
    public void Malformed_response_is_rejected_instead_of_scored(int[] response)
    {
        Assert.Throws<ArgumentException>(() => CreateRule().Score(response));
    }

    [Fact]
    public void Editing_source_arrays_does_not_change_an_existing_rule()
    {
        int[] tiles = [1, 2, 3];
        int[] acceptedOrder = [1, 2, 3];
        var rule = new BuildSentenceScoringRule(tiles, [acceptedOrder]);

        tiles[0] = 99;
        acceptedOrder[0] = 99;

        Assert.Equal(1, rule.Score([1, 2, 3]));
        Assert.Throws<ArgumentException>(() => rule.Score([99, 2, 3]));
    }

    [Fact]
    public void Invalid_answer_key_cannot_be_used()
    {
        Assert.Throws<ArgumentException>(() => new BuildSentenceScoringRule([1, 2, 3], []));
        Assert.Throws<ArgumentException>(() => new BuildSentenceScoringRule([1, 2, 3], [new[] { 1, 99 }]));
        Assert.Throws<ArgumentException>(() => new BuildSentenceScoringRule([1, 2, 3], [new[] { 1, 1 }]));
        Assert.Throws<ArgumentException>(() => new BuildSentenceScoringRule([1, 2, 3], [new[] { 1, 2 }, new[] { 1, 2, 3 }]));
    }
}
