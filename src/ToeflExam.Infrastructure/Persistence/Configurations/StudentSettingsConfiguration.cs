using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToeflExam.Infrastructure.Persistence.Entities;
namespace ToeflExam.Infrastructure.Persistence.Configurations;
public sealed class StudentSettingsConfiguration : IEntityTypeConfiguration<StudentSettings>
{
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<StudentSettings> b)
    {
        b.ToTable("StudentSettings", "learning", t => t.HasCheckConstraint("CK_StudentSettings_Goal", "[DailyPracticeMinutes] BETWEEN 5 AND 180"));
        b.HasKey(x => x.UserId); b.Property(x => x.UserId).HasMaxLength(450);
        b.Property(x => x.DisplayName).HasMaxLength(100);
        b.HasOne<IdentityUser>().WithOne().HasForeignKey<StudentSettings>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
