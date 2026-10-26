using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Infrastructure.Persistence.Configurations;

public sealed class BuildSentenceQuestionVersionConfiguration : IEntityTypeConfiguration<BuildSentenceQuestionVersion>
{
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public void Configure(EntityTypeBuilder<BuildSentenceQuestionVersion> builder)
    {
        builder.ToTable("BuildSentenceQuestionVersions", "content", table =>
        {
            table.HasCheckConstraint("CK_BuildSentenceQuestionVersions_Version", "[VersionNumber] > 0");
            table.HasCheckConstraint("CK_BuildSentenceQuestionVersions_Status", "[PublicationStatus] IN (0, 1, 2, 3)");
            table.HasCheckConstraint("CK_BuildSentenceQuestionVersions_TilesJson", "ISJSON([TilesJson]) = 1 AND LEFT(LTRIM([TilesJson]), 1) = '['");
            table.HasCheckConstraint("CK_BuildSentenceQuestionVersions_ResponsePartsJson", "ISJSON([ResponsePartsJson]) = 1 AND LEFT(LTRIM([ResponsePartsJson]), 1) = '['");
            table.HasCheckConstraint("CK_BuildSentenceQuestionVersions_AcceptedOrdersJson", "ISJSON([AcceptedOrdersJson]) = 1 AND LEFT(LTRIM([AcceptedOrdersJson]), 1) = '['");
        });

        builder.HasKey(question => question.Id);
        builder.Property(question => question.Id).ValueGeneratedNever();
        builder.HasIndex(question => new { question.QuestionId, question.VersionNumber }).IsUnique();
        builder.HasIndex(question => question.PublicationStatus);
        builder.Property(question => question.Prompt).IsRequired().HasMaxLength(4000);
        builder.Property(question => question.Explanation).IsRequired().HasMaxLength(4000);
        builder.Property(question => question.PublicationStatus).HasConversion<int>();
        builder.Property(question => question.TilesJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(question => question.ResponsePartsJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(question => question.AcceptedOrdersJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(question => question.RowVersion).IsRowVersion();
    }
}
