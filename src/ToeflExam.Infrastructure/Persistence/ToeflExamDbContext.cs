using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ToeflExam.Domain.Learning;
using ToeflExam.Domain.Content;
using ToeflExam.Domain.Writing;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Infrastructure.Persistence;

public sealed class ToeflExamDbContext(DbContextOptions<ToeflExamDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<BankSession> BankSessions => Set<BankSession>();
    public DbSet<LatestExamReport> LatestExamReports => Set<LatestExamReport>();
    public DbSet<SpeakingQuestionVersion> SpeakingQuestions => Set<SpeakingQuestionVersion>();
    public DbSet<SpeakingPathVersion> SpeakingPaths => Set<SpeakingPathVersion>();
    public DbSet<ToeflExam.Domain.Speaking.SpeakingRun> SpeakingRuns => Set<ToeflExam.Domain.Speaking.SpeakingRun>();
    public DbSet<ReadingSetVersion> ReadingSets => Set<ReadingSetVersion>();
    public DbSet<ToeflExam.Domain.Reading.ReadingRun> ReadingRuns => Set<ToeflExam.Domain.Reading.ReadingRun>();
    public DbSet<BuildSentenceQuestionVersion> BuildSentenceQuestionVersions => Set<BuildSentenceQuestionVersion>();
    public DbSet<BuildSentencePathVersion> BuildSentencePaths => Set<BuildSentencePathVersion>();
    public DbSet<BuildSentenceRun> BuildSentenceRuns => Set<BuildSentenceRun>();
    public DbSet<WritingQuestionVersion> WritingQuestions => Set<WritingQuestionVersion>();
    public DbSet<WritingPathVersion> WritingPaths => Set<WritingPathVersion>();
    public DbSet<WritingRun> WritingRuns => Set<WritingRun>();

    /// <summary>Checks published-content immutability before committing tracked changes.</summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardPublishedContent();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>Checks published-content immutability before asynchronously committing tracked changes.</summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        GuardPublishedContent();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Prevents edits to published snapshots already referenced by student runs.</summary>
    private void GuardPublishedContent()
    {
        foreach (var entry in ChangeTracker.Entries<SpeakingQuestionVersion>())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted)) continue;
            var original = entry.OriginalValues.GetValue<PublicationStatus>(nameof(SpeakingQuestionVersion.PublicationStatus));
            if (original is not (PublicationStatus.Published or PublicationStatus.Retired)) continue;
            var retiring = entry.State == EntityState.Modified && original == PublicationStatus.Published && entry.Entity.PublicationStatus == PublicationStatus.Retired
                && entry.Properties.Where(p => p.IsModified).All(p => p.Metadata.Name == nameof(SpeakingQuestionVersion.PublicationStatus));
            if (!retiring) throw new InvalidOperationException("Published Speaking content is immutable; create a new version.");
        }
        foreach (var entry in ChangeTracker.Entries<SpeakingPathVersion>())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted)) continue;
            var retiring = entry.State == EntityState.Modified && entry.OriginalValues.GetValue<bool>(nameof(SpeakingPathVersion.IsPublished))
                && !entry.Entity.IsPublished && entry.Properties.Where(p => p.IsModified).All(p => p.Metadata.Name == nameof(SpeakingPathVersion.IsPublished));
            if (!retiring) throw new InvalidOperationException("Speaking paths are immutable; create a new version.");
        }
        var newSpeakingPaths = ChangeTracker.Entries<SpeakingPathVersion>().Where(e => e.State == EntityState.Added).Select(e => e.Entity.Id).ToHashSet();
        if (ChangeTracker.Entries<SpeakingPathStep>().Any(e => e.State is EntityState.Modified or EntityState.Deleted
            || e.State == EntityState.Added && !newSpeakingPaths.Contains(e.Entity.PathId)))
            throw new InvalidOperationException("Insert speaking steps together with a new path.");
        foreach (var entry in ChangeTracker.Entries<ReadingSetVersion>())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted)) continue;
            var original = entry.OriginalValues.GetValue<PublicationStatus>(nameof(ReadingSetVersion.PublicationStatus));
            if (original is not (PublicationStatus.Published or PublicationStatus.Retired)) continue;
            var retiring = entry.State == EntityState.Modified && original == PublicationStatus.Published && entry.Entity.PublicationStatus == PublicationStatus.Retired
                && entry.Properties.Where(p => p.IsModified).All(p => p.Metadata.Name == nameof(ReadingSetVersion.PublicationStatus));
            if (!retiring) throw new InvalidOperationException("Published Reading content is immutable. Create a new version.");
        }
        foreach (var entry in ChangeTracker.Entries<WritingQuestionVersion>())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted)) continue;
            var original = entry.OriginalValues.GetValue<PublicationStatus>(nameof(WritingQuestionVersion.PublicationStatus));
            if (original is not (PublicationStatus.Published or PublicationStatus.Retired)) continue;
            var retiring = entry.State == EntityState.Modified && original == PublicationStatus.Published
                && entry.Entity.PublicationStatus == PublicationStatus.Retired
                && entry.Properties.Where(p => p.IsModified).All(p => p.Metadata.Name == nameof(WritingQuestionVersion.PublicationStatus));
            if (!retiring) throw new InvalidOperationException("Published writing content is immutable. Create a new version.");
        }
        foreach (var entry in ChangeTracker.Entries<WritingPathVersion>())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted)) continue;
            var retiring = entry.State == EntityState.Modified && entry.OriginalValues.GetValue<bool>(nameof(WritingPathVersion.IsPublished))
                && !entry.Entity.IsPublished && entry.Properties.Where(p => p.IsModified).All(p => p.Metadata.Name == nameof(WritingPathVersion.IsPublished));
            if (!retiring) throw new InvalidOperationException("Writing paths are immutable. Create a new version.");
        }
        var newWritingPaths = ChangeTracker.Entries<WritingPathVersion>().Where(e => e.State == EntityState.Added).Select(e => e.Entity.Id).ToHashSet();
        if (ChangeTracker.Entries<WritingPathStep>().Any(e => e.State is EntityState.Modified or EntityState.Deleted
            || e.State == EntityState.Added && !newWritingPaths.Contains(e.Entity.PathId)))
            throw new InvalidOperationException("Writing path steps must be inserted together with a new path.");
        foreach (var entry in ChangeTracker.Entries<BuildSentenceQuestionVersion>())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted)) continue;
            var original = entry.OriginalValues.GetValue<PublicationStatus>(nameof(BuildSentenceQuestionVersion.PublicationStatus));
            if (original is not (PublicationStatus.Published or PublicationStatus.Retired)) continue;
            var isRetirementOnly = entry.State == EntityState.Modified && original == PublicationStatus.Published
                && entry.Entity.PublicationStatus == PublicationStatus.Retired
                && entry.Properties.Where(property => property.IsModified).All(property => property.Metadata.Name == nameof(BuildSentenceQuestionVersion.PublicationStatus));
            if (!isRetirementOnly) throw new InvalidOperationException("Published content is immutable. Create a new version instead.");
        }

        foreach (var entry in ChangeTracker.Entries<BuildSentencePathVersion>())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted)) continue;
            var isRetirementOnly = entry.State == EntityState.Modified
                && entry.OriginalValues.GetValue<bool>(nameof(BuildSentencePathVersion.IsPublished)) && !entry.Entity.IsPublished
                && entry.Properties.Where(property => property.IsModified).All(property => property.Metadata.Name == nameof(BuildSentencePathVersion.IsPublished));
            if (!isRetirementOnly) throw new InvalidOperationException("Published paths are immutable. Create a new version instead.");
        }

        // A path is inserted as a complete version. Reordering requires a new version.
        if (ChangeTracker.Entries<BuildSentencePathStep>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Path steps are immutable. Create a new path version instead.");
        var newPathIds = ChangeTracker.Entries<BuildSentencePathVersion>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity.Id).ToHashSet();
        if (ChangeTracker.Entries<BuildSentencePathStep>().Any(entry => entry.State == EntityState.Added
            && !newPathIds.Contains(entry.Entity.PathVersionId)))
            throw new InvalidOperationException("Add all steps when creating a new path version.");
    }

    /// <summary>Applies Identity mappings, practice constraints and versioned seed data.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ToeflExamDbContext).Assembly);
        BuildSentenceSeed.Configure(modelBuilder);
        WritingSeed.Configure(modelBuilder);
        ReadingSeed.Configure(modelBuilder);
        SpeakingSeed.Configure(modelBuilder);
        PracticeExpansionSeed.Configure(modelBuilder);
    }
}
