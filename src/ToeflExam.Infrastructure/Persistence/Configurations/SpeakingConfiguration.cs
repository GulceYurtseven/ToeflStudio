using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToeflExam.Domain.Speaking;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Infrastructure.Persistence.Configurations;

public sealed class SpeakingConfiguration : IEntityTypeConfiguration<SpeakingQuestionVersion>,
    IEntityTypeConfiguration<SpeakingPathVersion>, IEntityTypeConfiguration<SpeakingPathStep>,
    IEntityTypeConfiguration<SpeakingRun>, IEntityTypeConfiguration<SpeakingStep>
{
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<SpeakingQuestionVersion> b)
    {
        b.ToTable("SpeakingQuestionVersions", "content", t =>
        {
            t.HasCheckConstraint("CK_SpeakingQuestion_Version", "[Version] > 0");
            t.HasCheckConstraint("CK_SpeakingQuestion_Kind", "[Kind] IN (1,2)");
            t.HasCheckConstraint("CK_SpeakingQuestion_Status", "[PublicationStatus] BETWEEN 0 AND 3");
            t.HasCheckConstraint("CK_SpeakingQuestion_Timing", "[ResponseSeconds] BETWEEN 1 AND 45 AND [ScenePosition] BETWEEN 1 AND 7");
        });
        b.HasKey(q => q.Id); b.Property(q => q.Id).ValueGeneratedNever();
        b.HasIndex(q => new { q.QuestionId, q.Version }).IsUnique();
        b.Property(q => q.Title).HasMaxLength(200);
        b.Property(q => q.Instructions).HasMaxLength(2000);
        b.Property(q => q.Context).HasMaxLength(6000);
        b.Property(q => q.Prompt).HasMaxLength(2000);
        b.Property(q => q.AudioFile).HasMaxLength(100);
    }
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<SpeakingPathVersion> b)
    {
        b.ToTable("SpeakingPaths", "learning", t => t.HasCheckConstraint("CK_SpeakingPath_KindVersion", "[Kind] IN (1,2) AND [Version] > 0"));
        b.HasKey(p => p.Id); b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.Title).HasMaxLength(200);
        b.HasIndex(p => new { p.Kind, p.Version }).IsUnique();
        b.HasMany(p => p.Steps).WithOne().HasForeignKey(s => s.PathId).OnDelete(DeleteBehavior.Restrict);
    }
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<SpeakingPathStep> b)
    {
        b.ToTable("SpeakingPathSteps", "learning");
        b.HasKey(s => new { s.PathId, s.Position });
        b.HasIndex(s => new { s.PathId, s.QuestionId }).IsUnique();
        b.HasOne<SpeakingQuestionVersion>().WithMany().HasForeignKey(s => s.QuestionId).OnDelete(DeleteBehavior.Restrict);
    }
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<SpeakingRun> b)
    {
        b.ToTable("SpeakingRuns", "learning");
        b.HasKey(r => r.Id); b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.UserId).HasMaxLength(450);
        b.Property(r => r.Revision).IsConcurrencyToken();
        b.HasIndex(r => new { r.UserId, r.Kind }).IsUnique().HasFilter("[IsCompleted] = 0");
        b.HasIndex(r => r.RestartedFromId).IsUnique().HasFilter("[RestartedFromId] IS NOT NULL");
        b.HasOne<IdentityUser>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SpeakingPathVersion>().WithMany().HasForeignKey(r => r.PathId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SpeakingRun>().WithMany().HasForeignKey(r => r.RestartedFromId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(r => r.Steps).WithOne().HasForeignKey(s => s.RunId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(r => r.Steps).HasField("_steps").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Ignore(r => r.CompletedCount); b.Ignore(r => r.CompletionPercent);
    }
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<SpeakingStep> b)
    {
        b.ToTable("SpeakingRunSteps", "learning", t =>
        {
            t.HasCheckConstraint("CK_SpeakingStep_Score", "[Score] IS NULL OR ([Score] BETWEEN 0 AND 5 AND [DurationMilliseconds] >= 0)");
            t.HasCheckConstraint("CK_SpeakingStep_State",
                "([Score] IS NULL AND [CompletedAt] IS NULL AND [DurationMilliseconds] IS NULL AND [Model] IS NULL AND [RubricVersion] IS NULL " +
                "AND (([SubmissionId] IS NULL AND [LeaseToken] IS NULL AND [LeaseUntil] IS NULL) OR " +
                "([SubmissionId] IS NOT NULL AND [LeaseToken] IS NOT NULL AND [LeaseUntil] IS NOT NULL))) OR " +
                "([Score] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [DurationMilliseconds] IS NOT NULL AND [Model] IS NOT NULL " +
                "AND [RubricVersion] IS NOT NULL AND [SubmissionId] IS NOT NULL AND [LeaseToken] IS NULL AND [LeaseUntil] IS NULL)");
        });
        b.HasKey(s => new { s.RunId, s.Position });
        b.HasIndex(s => new { s.RunId, s.SubmissionId }).IsUnique().HasFilter("[SubmissionId] IS NOT NULL");
        b.Property(s => s.Model).HasMaxLength(120); b.Property(s => s.RubricVersion).HasMaxLength(60);
        b.HasOne<SpeakingQuestionVersion>().WithMany().HasForeignKey(s => s.QuestionId).OnDelete(DeleteBehavior.Restrict);
    }
}

