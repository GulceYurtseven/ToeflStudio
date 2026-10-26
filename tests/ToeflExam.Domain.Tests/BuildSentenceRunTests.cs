using ToeflExam.Domain.Learning;

namespace ToeflExam.Domain.Tests;

public sealed class BuildSentenceRunTests
{
    private static BuildSentenceRun Run() => new("user", Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()], DateTimeOffset.UtcNow);

    [Fact]
    public void Completion_and_accuracy_are_independent()
    {
        var run = Run();
        run.CompleteStep(1, Guid.NewGuid(), 0, DateTimeOffset.UtcNow);
        Assert.Equal(50m, run.CompletionPercent);
        Assert.Equal(0, run.CorrectCount);
        run.CompleteStep(2, Guid.NewGuid(), 1, DateTimeOffset.UtcNow);
        Assert.Equal(100m, run.CompletionPercent);
        Assert.Equal(1, run.CorrectCount);
        Assert.True(run.IsCompleted);
        Assert.NotNull(run.CompletedAt);
    }

    [Fact]
    public void Cannot_read_or_submit_a_locked_step()
    {
        var run = Run();
        Assert.Throws<PracticeRuleException>(() => run.GetAccessibleStep(2));
        Assert.Throws<PracticeRuleException>(() => run.CompleteStep(2, Guid.NewGuid(), 1, DateTimeOffset.UtcNow));
        Assert.Equal(0, run.CompletedCount);
    }

    [Fact]
    public void Retry_never_changes_score_or_increments_progress_twice()
    {
        var run = Run();
        var submission = Guid.NewGuid();
        Assert.True(run.CompleteStep(1, submission, 0, DateTimeOffset.UtcNow));
        Assert.False(run.CompleteStep(1, submission, 1, DateTimeOffset.UtcNow));
        Assert.Equal(1, run.CompletedCount);
        Assert.Equal(1, run.Revision);
        Assert.Equal(0, run.CorrectCount);
    }

    [Fact]
    public void New_submission_cannot_overwrite_completed_step()
    {
        var run = Run();
        run.CompleteStep(1, Guid.NewGuid(), 0, DateTimeOffset.UtcNow);
        Assert.Throws<PracticeRuleException>(() => run.CompleteStep(1, Guid.NewGuid(), 1, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Submission_identifier_cannot_be_reused_on_another_step()
    {
        var run = Run();
        var id = Guid.NewGuid();
        run.CompleteStep(1, id, 1, DateTimeOffset.UtcNow);
        Assert.Throws<PracticeRuleException>(() => run.CompleteStep(2, id, 1, DateTimeOffset.UtcNow));
    }
}
