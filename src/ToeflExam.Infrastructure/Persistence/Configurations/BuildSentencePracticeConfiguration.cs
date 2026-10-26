using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToeflExam.Domain.Learning;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Infrastructure.Persistence.Configurations;

public sealed class BuildSentencePracticeConfiguration :
    IEntityTypeConfiguration<BuildSentencePathVersion>, IEntityTypeConfiguration<BuildSentencePathStep>,
    IEntityTypeConfiguration<BuildSentenceRun>, IEntityTypeConfiguration<BuildSentenceStep>
{
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<BuildSentencePathVersion> builder)
    {
        builder.ToTable("BuildSentencePaths", "learning");
        builder.HasKey(path => path.Id);
        builder.Property(path => path.Id).ValueGeneratedNever();
        builder.Property(path => path.Title).HasMaxLength(200).IsRequired();
        builder.HasIndex(path => path.Version).IsUnique();
        builder.HasMany(path => path.Steps).WithOne().HasForeignKey(step => step.PathVersionId).OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<BuildSentencePathStep> builder)
    {
        builder.ToTable("BuildSentencePathSteps", "learning");
        builder.HasKey(step => new { step.PathVersionId, step.Position });
        builder.HasIndex(step => new { step.PathVersionId, step.QuestionVersionId }).IsUnique();
        builder.HasOne<BuildSentenceQuestionVersion>().WithMany().HasForeignKey(step => step.QuestionVersionId).OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<BuildSentenceRun> builder)
    {
        builder.ToTable("BuildSentenceRuns", "learning");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.Id).ValueGeneratedNever();
        builder.Property(run => run.UserId).HasMaxLength(450).IsRequired();
        builder.Property(run => run.Revision).IsConcurrencyToken();
        builder.HasIndex(run => run.UserId).IsUnique().HasFilter("[IsCompleted] = 0");
        builder.HasIndex(run => run.RestartedFromId).IsUnique().HasFilter("[RestartedFromId] IS NOT NULL");
        builder.HasOne<IdentityUser>().WithMany().HasForeignKey(run => run.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BuildSentencePathVersion>().WithMany().HasForeignKey(run => run.PathVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BuildSentenceRun>().WithMany().HasForeignKey(run => run.RestartedFromId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.Steps).WithOne().HasForeignKey(step => step.RunId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(run => run.Steps).HasField("_steps").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(run => run.CompletedCount);
        builder.Ignore(run => run.CorrectCount);
        builder.Ignore(run => run.CompletionPercent);
    }

    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<BuildSentenceStep> builder)
    {
        builder.ToTable("BuildSentenceRunSteps", "learning", table =>
            table.HasCheckConstraint("CK_BuildSentenceRunSteps_Completion",
                "([RawScore] IS NULL AND [SubmissionId] IS NULL AND [CompletedAt] IS NULL) OR " +
                "([RawScore] IN (0, 1) AND [RawScore] IS NOT NULL AND [SubmissionId] IS NOT NULL AND [CompletedAt] IS NOT NULL)"));
        builder.HasKey(step => new { step.RunId, step.Position });
        builder.HasIndex(step => new { step.RunId, step.SubmissionId }).IsUnique().HasFilter("[SubmissionId] IS NOT NULL");
        builder.HasOne<BuildSentenceQuestionVersion>().WithMany().HasForeignKey(step => step.QuestionVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
