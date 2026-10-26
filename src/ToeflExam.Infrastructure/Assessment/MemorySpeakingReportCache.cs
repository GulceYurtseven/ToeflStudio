using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using ToeflExam.Application.Speaking;

namespace ToeflExam.Infrastructure.Assessment;

// Bounded ephemeral storage, deliberately not persisted in SQL. Use sticky routing for a multi-instance deployment.
public sealed class MemorySpeakingReportCache(TimeProvider clock) : ISpeakingReportCache, IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions
        { SizeLimit = 16 * 1024 * 1024, ExpirationScanFrequency = TimeSpan.FromMinutes(1) });
    /// <summary>Stores temporary assessment detail without writing student answers to SQL.</summary>
    public void Put(string userId, SpeakingReport report)
    {
        var ttl = report.ExpiresAt - clock.GetUtcNow();
        if (ttl <= TimeSpan.Zero) return;
        _cache.Set((userId, report.RunId, report.Position), report, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Size = JsonSerializer.SerializeToUtf8Bytes(report).LongLength * 2 + 1024
        });
    }
    /// <summary>Retrieves owner-scoped assessment detail if its retention period has not expired.</summary>
    public SpeakingReport? Get(string userId, Guid runId, int position)
    {
        if (!_cache.TryGetValue<SpeakingReport>((userId, runId, position), out var report)) return null;
        if (report!.ExpiresAt > clock.GetUtcNow()) return report;
        _cache.Remove((userId, runId, position)); return null;
    }
    /// <summary>Releases owned timers or cached resources.</summary>
    public void Dispose() => _cache.Dispose();
}

