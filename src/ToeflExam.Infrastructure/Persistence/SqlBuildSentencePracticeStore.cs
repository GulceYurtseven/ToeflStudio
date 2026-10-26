using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ToeflExam.Application.Content;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Domain.Content;
using ToeflExam.Domain.Learning;

namespace ToeflExam.Infrastructure.Persistence;

public sealed class SqlBuildSentencePracticeStore(ToeflExamDbContext db) : IBuildSentencePracticeStore, IBuildSentenceQuestionReader
{
    /// <summary>Loads the current published Build a Sentence path and its ordered steps.</summary>
    public async Task<PathDefinition?> GetCurrentPathAsync(CancellationToken ct)
    {
        var row = await db.BuildSentencePaths.AsNoTracking().Include(path => path.Steps)
            .Where(path => path.IsPublished).OrderByDescending(path => path.Version).FirstOrDefaultAsync(ct);
        return row is null ? null : new(row.Id, row.Title, row.Version,
            row.Steps.OrderBy(step => step.Position).Select(step => step.QuestionVersionId).ToArray());
    }

    /// <summary>Finds a run by both its ID and authenticated owner.</summary>
    public Task<BuildSentenceRun?> FindRunAsync(string userId, Guid runId, CancellationToken ct)
        => db.BuildSentenceRuns.Include(run => run.Steps).SingleOrDefaultAsync(run => run.Id == runId && run.UserId == userId, ct);

    /// <summary>Loads the student's most recent run.</summary>
    public Task<BuildSentenceRun?> FindLatestRunAsync(string userId, CancellationToken ct)
        => db.BuildSentenceRuns.Include(run => run.Steps).Where(run => run.UserId == userId)
            .OrderBy(run => run.IsCompleted).ThenByDescending(run => run.StartedAt).ThenByDescending(run => run.Id).FirstOrDefaultAsync(ct);

    /// <summary>Finds an existing restart to make repeat requests idempotent.</summary>
    public Task<BuildSentenceRun?> FindRestartAsync(string userId, Guid sourceRunId, CancellationToken ct)
        => db.BuildSentenceRuns.Include(run => run.Steps).SingleOrDefaultAsync(run => run.UserId == userId && run.RestartedFromId == sourceRunId, ct);

    /// <summary>Returns a bounded page of the student's run history.</summary>
    public async Task<IReadOnlyList<BuildSentenceRun>> ListRunsAsync(string userId, int skip, int take, CancellationToken ct)
        => await db.BuildSentenceRuns.AsNoTracking().Include(run => run.Steps).Where(run => run.UserId == userId)
            .OrderByDescending(run => run.StartedAt).ThenByDescending(run => run.Id).Skip(skip).Take(take).ToArrayAsync(ct);

    /// <summary>Loads and validates a pinned question version.</summary>
    public async Task<QuestionContent?> GetQuestionAsync(Guid versionId, CancellationToken ct)
    {
        var row = await db.BuildSentenceQuestionVersions.AsNoTracking().SingleOrDefaultAsync(row => row.Id == versionId, ct);
        return row is null ? null : BuildSentenceQuestionMapper.Read(row);
    }

    /// <summary>Finds a published question snapshot by its identifier.</summary>
    public async Task<BuildSentenceQuestion?> FindAsync(Guid questionId, CancellationToken cancellationToken)
        => (await GetQuestionAsync(questionId, cancellationToken))?.Question;

    /// <summary>Checks that every question in a published path remains available.</summary>
    public async Task<bool> IsPathAvailableAsync(PathDefinition path, CancellationToken ct)
        => path.QuestionVersionIds.Length > 0 && await db.BuildSentenceQuestionVersions.CountAsync(
            question => path.QuestionVersionIds.Contains(question.Id) && question.PublicationStatus == PublicationStatus.Published, ct) == path.QuestionVersionIds.Length;

    /// <summary>Tracks a new run for the next database save.</summary>
    public void AddRun(BuildSentenceRun run) => db.BuildSentenceRuns.Add(run);

    /// <summary>Persists progress and translates concurrent or duplicate writes into recoverable conflicts.</summary>
    public async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new PracticeConflictException("Progress was changed by another request. Retry with the same submission ID."); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { throw new PracticeConflictException("This action has already completed or conflicted with another request. Reload your run."); }
    }
}
