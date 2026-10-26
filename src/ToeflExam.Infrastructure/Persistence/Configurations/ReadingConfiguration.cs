using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToeflExam.Domain.Reading;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Infrastructure.Persistence.Configurations;

public sealed class ReadingConfiguration : IEntityTypeConfiguration<ReadingSetVersion>, IEntityTypeConfiguration<ReadingRun>, IEntityTypeConfiguration<ReadingStep>
{
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<ReadingSetVersion> b)
    {
        b.ToTable("ReadingSetVersions", "content"); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Module).HasMaxLength(40); b.Property(x => x.Title).HasMaxLength(200); b.Property(x => x.Format).HasMaxLength(40);
        b.Property(x => x.Text).HasMaxLength(12000); b.Property(x => x.ItemsJson).IsRequired();
        b.HasIndex(x => new { x.Module, x.Version, x.Position }).IsUnique();
    }
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<ReadingRun> b)
    {
        b.ToTable("ReadingRuns", "learning"); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.UserId).HasMaxLength(450); b.Property(x => x.Module).HasMaxLength(40);
        b.Property(x => x.Revision).IsConcurrencyToken(); b.Ignore(x => x.CompletedCount);
        b.HasIndex(x => new { x.UserId, x.Module }).IsUnique().HasFilter("[IsCompleted] = 0");
        b.HasIndex(x => x.RestartedFromId).IsUnique().HasFilter("[RestartedFromId] IS NOT NULL");
        b.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ReadingRun>().WithMany().HasForeignKey(x => x.RestartedFromId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Steps).WithOne().HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Steps).HasField("_steps").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<ReadingStep> b)
    {
        b.ToTable("ReadingRunSteps", "learning", t => t.HasCheckConstraint("CK_ReadingRunSteps_Completion",
            "([SubmissionId] IS NULL AND [CompletedAt] IS NULL) OR ([SubmissionId] IS NOT NULL AND [CompletedAt] IS NOT NULL)"));
        b.HasKey(x => new { x.RunId, x.Position });
        b.HasIndex(x => new { x.RunId, x.SubmissionId }).IsUnique().HasFilter("[SubmissionId] IS NOT NULL");
        b.HasOne<ReadingSetVersion>().WithMany().HasForeignKey(x => x.SetId).OnDelete(DeleteBehavior.Restrict);
        b.PrimitiveCollection(x => x.ItemScores).HasMaxLength(100);
    }
}
