using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToeflExam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LatestCompletedExamReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LatestExamReports",
                schema: "learning",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Form = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Payload = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LatestExamReports", x => new { x.UserId, x.Form })
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_LatestExamReports_BankSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "learning",
                        principalTable: "BankSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LatestExamReports_SessionId",
                schema: "learning",
                table: "LatestExamReports",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LatestExamReports",
                schema: "learning");
        }
    }
}
