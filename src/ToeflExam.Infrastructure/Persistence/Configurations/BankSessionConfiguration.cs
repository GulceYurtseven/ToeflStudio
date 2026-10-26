using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToeflExam.Domain.Learning;

namespace ToeflExam.Infrastructure.Persistence.Configurations;

public sealed class BankSessionConfiguration : IEntityTypeConfiguration<BankSession>, IEntityTypeConfiguration<BankStep>
{
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<BankSession> b)
    {
        b.ToTable("BankSessions","learning"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedNever();
        b.Property(x=>x.UserId).HasMaxLength(450); b.Property(x=>x.Mode).HasMaxLength(16); b.Property(x=>x.Form).HasMaxLength(40);
        b.Property(x=>x.Revision).IsConcurrencyToken();
        b.HasIndex(x=>new{x.UserId,x.Mode,x.Form,x.Edition}).IsUnique().HasFilter("[CompletedAt] IS NULL");
        b.HasIndex(x=>x.RestartedFromId).IsUnique().HasFilter("[RestartedFromId] IS NOT NULL");
        b.HasOne<IdentityUser>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x=>x.Steps).WithOne().HasForeignKey(x=>x.SessionId).OnDelete(DeleteBehavior.Restrict);
    }
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<BankStep> b)
    {
        b.ToTable("BankSessionSteps","learning"); b.HasKey(x=>new{x.SessionId,x.Position});
        b.Property(x=>x.Section).HasMaxLength(16); b.Property(x=>x.Stage).HasMaxLength(16); b.Property(x=>x.Kind).HasMaxLength(40);
        b.PrimitiveCollection(x=>x.Scores).HasMaxLength(100);
        b.HasIndex(x=>new{x.SessionId,x.SubmissionId}).IsUnique().HasFilter("[SubmissionId] IS NOT NULL");
    }
}
