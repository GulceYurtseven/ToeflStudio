using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Application.Writing;
using ToeflExam.Domain.Content;
using ToeflExam.Domain.Writing;

namespace ToeflExam.Infrastructure.Persistence;

public sealed class SqlWritingStore(ToeflExamDbContext db) : IWritingStore
{
    /// <summary>Loads the current published path for the requested task kind.</summary>
    public async Task<WritingPath?> CurrentPathAsync(WritingTaskKind kind, CancellationToken ct)
    {
        var path = await db.WritingPaths.AsNoTracking().Include(p => p.Steps)
            .Where(p => p.Kind == kind && p.IsPublished).OrderByDescending(p => p.Version).FirstOrDefaultAsync(ct);
        return path is null ? null : new(path.Id, kind, path.Version, path.Title,
            path.Steps.OrderBy(s => s.Position).Select(s => s.QuestionId).ToArray());
    }
    /// <summary>Verifies that every pinned question in the path is published.</summary>
    public async Task<bool> AvailableAsync(WritingPath path, CancellationToken ct) =>
        path.QuestionIds.Length > 0 && await db.WritingQuestions.CountAsync(q =>
            path.QuestionIds.Contains(q.Id) && q.Kind == path.Kind && q.PublicationStatus == PublicationStatus.Published, ct) == path.QuestionIds.Length;
    /// <summary>Loads a pinned question and its authored scoring metadata.</summary>
    public async Task<WritingQuestion?> QuestionAsync(Guid id, CancellationToken ct)
    {
        var q = await db.WritingQuestions.AsNoTracking().SingleOrDefaultAsync(q => q.Id == id, ct);
        return q is null ? null : new(q.Id, q.Kind, q.Title, q.Instructions, q.Context,
            JsonSerializer.Deserialize<string[]>(q.RequirementsJson)!, JsonSerializer.Deserialize<DiscussionPost[]>(q.PostsJson)!);
    }
    /// <summary>Finds a run scoped to its student and task module.</summary>
    public Task<WritingRun?> RunAsync(string userId, WritingTaskKind kind, Guid runId, CancellationToken ct)
        => db.WritingRuns.Include(r => r.Steps).SingleOrDefaultAsync(r => r.Id == runId && r.UserId == userId && r.Kind == kind, ct);
    /// <summary>Finds the student's latest run for a task kind.</summary>
    public Task<WritingRun?> LatestAsync(string userId, WritingTaskKind kind, CancellationToken ct)
        => db.WritingRuns.Include(r => r.Steps).Where(r => r.UserId == userId && r.Kind == kind)
            .OrderBy(r => r.IsCompleted).ThenByDescending(r => r.StartedAt).ThenByDescending(r => r.Id).FirstOrDefaultAsync(ct);
    /// <summary>Resolves or creates a new run linked to a completed source run.</summary>
    public Task<WritingRun?> RestartAsync(string userId, Guid sourceId, CancellationToken ct)
        => db.WritingRuns.Include(r => r.Steps).SingleOrDefaultAsync(r => r.UserId == userId && r.RestartedFromId == sourceId, ct);
    /// <summary>Loads student-owned run summaries with the requested scope.</summary>
    public async Task<IReadOnlyList<WritingRun>> HistoryAsync(string userId, WritingTaskKind kind, int skip, int take, CancellationToken ct)
        => await db.WritingRuns.AsNoTracking().Include(r => r.Steps).Where(r => r.UserId == userId && r.Kind == kind)
            .OrderByDescending(r => r.StartedAt).ThenByDescending(r => r.Id).Skip(skip).Take(take).ToArrayAsync(ct);
    /// <summary>Tracks a new practice run for persistence.</summary>
    public void Add(WritingRun run) => db.WritingRuns.Add(run);
    /// <summary>Persists progress and translates concurrent or duplicate writes into recoverable conflicts.</summary>
    public async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new PracticeConflictException("Another request changed this action. Reload and retry with the same submission ID."); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        { throw new PracticeConflictException("This action has already completed. Reload your run and retry."); }
    }
}
