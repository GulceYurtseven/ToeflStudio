using ToeflExam.Application.Assessment;

namespace ToeflExam.Application.Tests;

public sealed class TestFormatTests
{
    [Theory]
    [InlineData("reading", 15)]
    [InlineData("listening", 12)]
    public void Both_adaptive_paths_account_for_35_scored_items_without_invented_clocks(string section, int unscored)
    {
        var format = TestFormat2026.Section(section)!;
        Assert.Equal(20, format.AdaptiveTasks.Sum(t => t.Router));
        Assert.Equal(15, format.AdaptiveTasks.Sum(t => t.Lower));
        Assert.Equal(15, format.AdaptiveTasks.Sum(t => t.Upper));
        Assert.Equal(35, format.ScoredItemCount); Assert.Equal(unscored, format.PossibleUnscoredItems);
        Assert.Null(format.RouterTimeSeconds); Assert.Null(format.RoutingCutoff);
        Assert.False(format.HasOfficialScoreConversion); Assert.False(format.IsExamSimulationAvailable);
    }

    [Fact]
    public void Writing_keeps_raw_totals_separate_from_scaled_scores_and_unverified_timing()
    {
        var format = TestFormat2026.Section("writing")!;
        Assert.Equal(12, format.LinearTasks.Sum(t => t.ItemCount));
        Assert.Equal(20, format.LinearTasks.Sum(t => t.ItemCount * t.MaxRawScorePerItem));
        Assert.Null(format.LinearTasks[0].ConfirmedTimeSeconds);
        Assert.Equal(420, format.LinearTasks[1].ConfirmedTimeSeconds);
        Assert.Equal(600, format.LinearTasks[2].ConfirmedTimeSeconds);
        Assert.Null(TestFormat2026.Section("unknown"));
    }
}
