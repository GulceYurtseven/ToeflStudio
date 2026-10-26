using ToeflExam.Domain.Learning;
using ToeflExam.Domain.Writing;

namespace ToeflExam.Domain.Tests;

public sealed class WritingRunTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-06T10:00:00Z");
    private static WritingRun Run() => new("learner", WritingTaskKind.Email, Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()], Now);
    [Fact]
    public void Failed_evaluation_can_retry_without_advancing()
    {
        var run = Run();
        var token = run.BeginEvaluation(1, Guid.NewGuid(), Now);
        Assert.Throws<PracticeRuleException>(() => run.BeginEvaluation(1, Guid.NewGuid(), Now));
        run.Release(1, token);
        Assert.Equal(0m, run.CompletionPercent);
        Assert.Null(run.Steps.First().SubmissionId);
        run.BeginEvaluation(1, Guid.NewGuid(), Now);
    }
    [Fact]
    public void Expired_lease_can_be_replaced_but_stale_worker_cannot_finish_or_release_it()
    {
        var run = Run();
        var old = run.BeginEvaluation(1, Guid.NewGuid(), Now);
        var next = run.BeginEvaluation(1, Guid.NewGuid(), Now.AddMinutes(6));
        Assert.Throws<PracticeRuleException>(() => run.Complete(1, old, 5, 10, "model", "v1", Now.AddMinutes(6)));
        run.Release(1, old);
        Assert.Equal(next, run.Steps.First().LeaseToken);
        run.Complete(1, next, 3, 10, "model", "v1", Now.AddMinutes(6));
        Assert.Equal(50m, run.CompletionPercent);
    }
    [Fact]
    public void Cannot_skip_or_reuse_submission_or_overwrite_score()
    {
        var run = Run();
        var id = Guid.NewGuid();
        Assert.Throws<PracticeRuleException>(() => run.AccessibleStep(2));
        var token = run.BeginEvaluation(1, id, Now);
        run.Complete(1, token, 0, 0, "local", "v1", Now);
        Assert.Throws<PracticeRuleException>(() => run.BeginEvaluation(1, id, Now));
        Assert.Throws<PracticeRuleException>(() => run.BeginEvaluation(2, id, Now));
        var last = run.BeginEvaluation(2, Guid.NewGuid(), Now);
        run.Complete(2, last, 1, 20, "model", "v1", Now);
        Assert.True(run.IsCompleted);
        Assert.Equal(100m, run.CompletionPercent);
    }
    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void Invalid_model_score_cannot_complete_step(int score)
    {
        var run = Run();
        var token = run.BeginEvaluation(1, Guid.NewGuid(), Now);
        Assert.Throws<ArgumentException>(() => run.Complete(1, token, score, 10, "model", "v1", Now));
        Assert.Equal(0, run.CompletedCount);
    }
}
