using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Application.Speaking;
using ToeflExam.Domain.Content;
using ToeflExam.Domain.Speaking;

namespace ToeflExam.Infrastructure.Persistence;

public sealed class SqlSpeakingStore(ToeflExamDbContext db) : ISpeakingStore
{
    /// <summary>Loads the current published path for the requested task kind.</summary>
    public async Task<SpeakingPath?> CurrentPathAsync(SpeakingTaskKind kind, CancellationToken ct)
    {
        var path = await db.SpeakingPaths.AsNoTracking().Include(p => p.Steps)
            .Where(p => p.Kind == kind && p.IsPublished).OrderByDescending(p => p.Version).FirstOrDefaultAsync(ct);
        return path is null ? null : new(path.Id, kind, path.Version, path.Title,
            path.Steps.OrderBy(s => s.Position).Select(s => s.QuestionId).ToArray());
    }
    /// <summary>Verifies that every pinned question in the path is published.</summary>
    public async Task<bool> AvailableAsync(SpeakingPath path, CancellationToken ct) =>
        path.QuestionIds.Length > 0 && await db.SpeakingQuestions.CountAsync(q =>
            path.QuestionIds.Contains(q.Id) && q.Kind == path.Kind && q.PublicationStatus == PublicationStatus.Published, ct) == path.QuestionIds.Length;
    /// <summary>Loads a pinned question and its authored scoring metadata.</summary>
    public async Task<SpeakingQuestion?> QuestionAsync(Guid id, CancellationToken ct)
    {
        var q = await db.SpeakingQuestions.AsNoTracking().SingleOrDefaultAsync(q => q.Id == id, ct);
        return q is null ? null : new(q.Id, q.Kind, q.Title, q.Instructions, q.Context,
            q.Prompt, q.AudioFile, q.ResponseSeconds, q.ScenePosition);
    }
    /// <summary>Finds a run scoped to its student and task module.</summary>
    public Task<SpeakingRun?> RunAsync(string userId, SpeakingTaskKind kind, Guid runId, CancellationToken ct)
        => db.SpeakingRuns.Include(r => r.Steps).SingleOrDefaultAsync(r => r.Id == runId && r.UserId == userId && r.Kind == kind, ct);
    /// <summary>Finds the student's latest run for a task kind.</summary>
    public Task<SpeakingRun?> LatestAsync(string userId, SpeakingTaskKind kind, CancellationToken ct)
        => db.SpeakingRuns.Include(r => r.Steps).Where(r => r.UserId == userId && r.Kind == kind)
            .OrderBy(r => r.IsCompleted).ThenByDescending(r => r.StartedAt).ThenByDescending(r => r.Id).FirstOrDefaultAsync(ct);
    /// <summary>Resolves or creates a new run linked to a completed source run.</summary>
    public Task<SpeakingRun?> RestartAsync(string userId, Guid sourceId, CancellationToken ct)
        => db.SpeakingRuns.Include(r => r.Steps).SingleOrDefaultAsync(r => r.UserId == userId && r.RestartedFromId == sourceId, ct);
    /// <summary>Loads student-owned run summaries with the requested scope.</summary>
    public async Task<IReadOnlyList<SpeakingRun>> HistoryAsync(string userId, SpeakingTaskKind kind, int skip, int take, CancellationToken ct)
        => await db.SpeakingRuns.AsNoTracking().Include(r => r.Steps).Where(r => r.UserId == userId && r.Kind == kind)
            .OrderByDescending(r => r.StartedAt).ThenByDescending(r => r.Id).Skip(skip).Take(take).ToArrayAsync(ct);
    /// <summary>Tracks a new practice run for persistence.</summary>
    public void Add(SpeakingRun run) => db.SpeakingRuns.Add(run);
    /// <summary>Persists progress and translates concurrent or duplicate writes into recoverable conflicts.</summary>
    public async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new PracticeConflictException("Another request changed this action. Reload and retry with the same submission ID."); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        { throw new PracticeConflictException("This action has already completed. Reload your run and retry."); }
    }
}

