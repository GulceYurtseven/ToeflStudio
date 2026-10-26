using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToeflExam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VersionedExamSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankSessions_UserId_Mode_Form",
                schema: "learning",
                table: "BankSessions");

            migrationBuilder.CreateIndex(
                name: "IX_BankSessions_UserId_Mode_Form_Edition",
                schema: "learning",
                table: "BankSessions",
                columns: new[] { "UserId", "Mode", "Form", "Edition" },
                unique: true,
                filter: "[CompletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankSessions_UserId_Mode_Form_Edition",
                schema: "learning",
                table: "BankSessions");

            migrationBuilder.CreateIndex(
                name: "IX_BankSessions_UserId_Mode_Form",
                schema: "learning",
                table: "BankSessions",
                columns: new[] { "UserId", "Mode", "Form" },
                unique: true,
                filter: "[CompletedAt] IS NULL");
        }
    }
}
