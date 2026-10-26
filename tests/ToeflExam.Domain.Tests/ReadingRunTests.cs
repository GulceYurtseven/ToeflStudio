using ToeflExam.Domain.Learning;
using ToeflExam.Domain.Reading;

namespace ToeflExam.Domain.Tests;

public sealed class ReadingRunTests
{
    [Fact]
    public void Progress_is_ordered_and_duplicate_submission_cannot_change_scores()
    {
        var run = new ReadingRun("alice", "daily-life", 1, [Guid.NewGuid(), Guid.NewGuid()], DateTimeOffset.UtcNow);
        Assert.Throws<PracticeRuleException>(() => run.Accessible(2));
        var id = Guid.NewGuid(); run.Complete(1, id, [1, 0], DateTimeOffset.UtcNow);
        run.Complete(1, id, [0, 0], DateTimeOffset.UtcNow);
        Assert.Equal(new[] { 1, 0 }, run.Accessible(1).ItemScores);
        Assert.Throws<PracticeRuleException>(() => run.Complete(2, id, [1], DateTimeOffset.UtcNow));
        run.Complete(2, Guid.NewGuid(), [1, 1, 0], DateTimeOffset.UtcNow);
        Assert.True(run.IsCompleted); Assert.Equal(2, run.CompletedCount); Assert.Equal(2, run.Revision);
    }
}
