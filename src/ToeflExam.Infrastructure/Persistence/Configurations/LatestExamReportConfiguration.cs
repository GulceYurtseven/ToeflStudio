using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToeflExam.Domain.Learning;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Infrastructure.Persistence.Configurations;

/// <summary>Enforces one report per owner/form and protects replacement against concurrent AI updates.</summary>
public sealed class LatestExamReportConfiguration : IEntityTypeConfiguration<LatestExamReport>
{
    /// <summary>Maps the owner/form key, session relationship and optimistic concurrency token.</summary>
    public void Configure(EntityTypeBuilder<LatestExamReport> builder)
    {
        builder.ToTable("LatestExamReports", "learning");
        builder.HasKey(x => new { x.UserId, x.Form }).IsClustered(false);
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.Form).HasMaxLength(40);
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne<BankSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
    }
}
