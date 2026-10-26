using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToeflExam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialQuestionContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content");

            migrationBuilder.CreateTable(
                name: "BuildSentenceQuestionVersions",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PublicationStatus = table.Column<int>(type: "int", nullable: false),
                    TilesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsePartsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcceptedOrdersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildSentenceQuestionVersions", x => x.Id);
                    table.CheckConstraint("CK_BuildSentenceQuestionVersions_AcceptedOrdersJson", "ISJSON([AcceptedOrdersJson]) = 1 AND LEFT(LTRIM([AcceptedOrdersJson]), 1) = '['");
                    table.CheckConstraint("CK_BuildSentenceQuestionVersions_ResponsePartsJson", "ISJSON([ResponsePartsJson]) = 1 AND LEFT(LTRIM([ResponsePartsJson]), 1) = '['");
                    table.CheckConstraint("CK_BuildSentenceQuestionVersions_Status", "[PublicationStatus] IN (0, 1, 2, 3)");
                    table.CheckConstraint("CK_BuildSentenceQuestionVersions_TilesJson", "ISJSON([TilesJson]) = 1 AND LEFT(LTRIM([TilesJson]), 1) = '['");
                    table.CheckConstraint("CK_BuildSentenceQuestionVersions_Version", "[VersionNumber] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuildSentenceQuestionVersions_PublicationStatus",
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                column: "PublicationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_BuildSentenceQuestionVersions_QuestionId_VersionNumber",
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                columns: new[] { "QuestionId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildSentenceQuestionVersions",
                schema: "content");
        }
    }
}
