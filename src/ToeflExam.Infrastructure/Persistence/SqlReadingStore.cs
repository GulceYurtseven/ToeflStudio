using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Application.Reading;
using ToeflExam.Domain.Content;
using ToeflExam.Domain.Reading;

namespace ToeflExam.Infrastructure.Persistence;

public sealed class SqlReadingStore(ToeflExamDbContext db) : IReadingStore
{
    /// <summary>Loads the current published path for the selected practice module.</summary>
    public async Task<ReadingPath?> PathAsync(string module, CancellationToken ct)
    {
        var version = await db.ReadingSets.Where(x => x.Module == module && x.PublicationStatus == PublicationStatus.Published)
            .MaxAsync(x => (int?)x.Version, ct);
        if (version is null) return null;
        var rows = await db.ReadingSets.AsNoTracking().Where(x => x.Module == module && x.Version == version)
            .OrderBy(x => x.Position).ToArrayAsync(ct);
        if (rows.Any(x => x.PublicationStatus != PublicationStatus.Published) || !rows.Select(x => x.Position).SequenceEqual(Enumerable.Range(1, rows.Length)))
            throw new PracticeConflictException("The Reading learning path is currently unavailable.");
        return new(module, version.Value, rows.Select(x => x.Id).ToArray());
    }
    /// <summary>Loads the published Reading set and reconstructs its authored items.</summary>
    public async Task<ReadingContent?> ContentAsync(Guid id, CancellationToken ct)
    {
        var row = await db.ReadingSets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return row is null ? null : new(row.Id, row.Module, row.Title, row.Format, row.Text, JsonSerializer.Deserialize<ReadingItem[]>(row.ItemsJson)!);
    }
    /// <summary>Finds a run scoped to its student and task module.</summary>
    public Task<ReadingRun?> RunAsync(string user, string module, Guid id, CancellationToken ct)
        => db.ReadingRuns.Include(x => x.Steps).SingleOrDefaultAsync(x => x.UserId == user && x.Module == module && x.Id == id, ct);
    /// <summary>Loads student-owned run summaries with the requested scope.</summary>
    public Task<ReadingRun[]> HistoryAsync(string user, string module, int skip, int take, CancellationToken ct)
        => db.ReadingRuns.Include(x => x.Steps).Where(x => x.UserId == user && x.Module == module)
            .OrderBy(x => x.IsCompleted).ThenByDescending(x => x.StartedAt).ThenByDescending(x => x.Id).Skip(skip).Take(take).ToArrayAsync(ct);
    /// <summary>Resolves or creates a new run linked to a completed source run.</summary>
    public Task<ReadingRun?> RestartAsync(string user, Guid source, CancellationToken ct)
        => db.ReadingRuns.Include(x => x.Steps).SingleOrDefaultAsync(x => x.UserId == user && x.RestartedFromId == source, ct);
    /// <summary>Tracks a new practice run for persistence.</summary>
    public void Add(ReadingRun run) => db.ReadingRuns.Add(run);
    /// <summary>Persists progress and translates concurrent or duplicate writes into recoverable conflicts.</summary>
    public async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new PracticeConflictException("Progress changed. Reload and retry the same submission."); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { throw new PracticeConflictException("This action has already completed. Reload your run."); }
    }
}
