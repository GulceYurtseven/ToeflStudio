using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ToeflExam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SpeakingStudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpeakingPaths",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakingPaths", x => x.Id);
                    table.CheckConstraint("CK_SpeakingPath_KindVersion", "[Kind] IN (1,2) AND [Version] > 0");
                });

            migrationBuilder.CreateTable(
                name: "SpeakingQuestionVersions",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    PublicationStatus = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Context = table.Column<string>(type: "nvarchar(max)", maxLength: 6000, nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AudioFile = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResponseSeconds = table.Column<int>(type: "int", nullable: false),
                    ScenePosition = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakingQuestionVersions", x => x.Id);
                    table.CheckConstraint("CK_SpeakingQuestion_Kind", "[Kind] IN (1,2)");
                    table.CheckConstraint("CK_SpeakingQuestion_Status", "[PublicationStatus] BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_SpeakingQuestion_Timing", "[ResponseSeconds] BETWEEN 1 AND 45 AND [ScenePosition] BETWEEN 1 AND 7");
                    table.CheckConstraint("CK_SpeakingQuestion_Version", "[Version] > 0");
                });

            migrationBuilder.CreateTable(
                name: "SpeakingRuns",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    PathId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestartedFromId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakingRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpeakingRuns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpeakingRuns_SpeakingPaths_PathId",
                        column: x => x.PathId,
                        principalSchema: "learning",
                        principalTable: "SpeakingPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpeakingRuns_SpeakingRuns_RestartedFromId",
                        column: x => x.RestartedFromId,
                        principalSchema: "learning",
                        principalTable: "SpeakingRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpeakingPathSteps",
                schema: "learning",
                columns: table => new
                {
                    PathId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakingPathSteps", x => new { x.PathId, x.Position });
                    table.ForeignKey(
                        name: "FK_SpeakingPathSteps_SpeakingPaths_PathId",
                        column: x => x.PathId,
                        principalSchema: "learning",
                        principalTable: "SpeakingPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpeakingPathSteps_SpeakingQuestionVersions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "content",
                        principalTable: "SpeakingQuestionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpeakingRunSteps",
                schema: "learning",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Score = table.Column<int>(type: "int", nullable: true),
                    DurationMilliseconds = table.Column<int>(type: "int", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RubricVersion = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakingRunSteps", x => new { x.RunId, x.Position });
                    table.CheckConstraint("CK_SpeakingStep_Score", "[Score] IS NULL OR ([Score] BETWEEN 0 AND 5 AND [DurationMilliseconds] >= 0)");
                    table.CheckConstraint("CK_SpeakingStep_State", "([Score] IS NULL AND [CompletedAt] IS NULL AND [DurationMilliseconds] IS NULL AND [Model] IS NULL AND [RubricVersion] IS NULL AND (([SubmissionId] IS NULL AND [LeaseToken] IS NULL AND [LeaseUntil] IS NULL) OR ([SubmissionId] IS NOT NULL AND [LeaseToken] IS NOT NULL AND [LeaseUntil] IS NOT NULL))) OR ([Score] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [DurationMilliseconds] IS NOT NULL AND [Model] IS NOT NULL AND [RubricVersion] IS NOT NULL AND [SubmissionId] IS NOT NULL AND [LeaseToken] IS NULL AND [LeaseUntil] IS NULL)");
                    table.ForeignKey(
                        name: "FK_SpeakingRunSteps_SpeakingQuestionVersions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "content",
                        principalTable: "SpeakingQuestionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpeakingRunSteps_SpeakingRuns_RunId",
                        column: x => x.RunId,
                        principalSchema: "learning",
                        principalTable: "SpeakingRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "learning",
                table: "SpeakingPaths",
                columns: new[] { "Id", "IsPublished", "Kind", "Title", "Version" },
                values: new object[,]
                {
                    { new Guid("e1100000-0000-4000-8000-000000000001"), true, 1, "Campus learning center", 1 },
                    { new Guid("e2100000-0000-4000-8000-000000000001"), true, 2, "Learning new skills", 1 }
                });

            migrationBuilder.InsertData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                columns: new[] { "Id", "AudioFile", "Context", "Instructions", "Kind", "Prompt", "PublicationStatus", "QuestionId", "ResponseSeconds", "ScenePosition", "Title", "Version" },
                values: new object[,]
                {
                    { new Guid("e1200000-0000-4000-8000-000000000001"), "1-1-v1.wav", "Campus learning center", "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.", 1, "Welcome to the learning center.", 2, new Guid("e1200000-0000-4000-8000-000000000001"), 8, 1, "Campus learning center · 1", 1 },
                    { new Guid("e1200000-0000-4000-8000-000000000002"), "1-2-v1.wav", "Campus learning center", "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.", 1, "The information desk is on your left.", 2, new Guid("e1200000-0000-4000-8000-000000000002"), 8, 2, "Campus learning center · 2", 1 },
                    { new Guid("e1200000-0000-4000-8000-000000000003"), "1-3-v1.wav", "Campus learning center", "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.", 1, "You can borrow a laptop with your student card.", 2, new Guid("e1200000-0000-4000-8000-000000000003"), 10, 3, "Campus learning center · 3", 1 },
                    { new Guid("e1200000-0000-4000-8000-000000000004"), "1-4-v1.wav", "Campus learning center", "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.", 1, "Please keep your voice low near the individual study rooms.", 2, new Guid("e1200000-0000-4000-8000-000000000004"), 10, 4, "Campus learning center · 4", 1 },
                    { new Guid("e1200000-0000-4000-8000-000000000005"), "1-5-v1.wav", "Campus learning center", "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.", 1, "The computer workshop begins upstairs at ten tomorrow morning.", 2, new Guid("e1200000-0000-4000-8000-000000000005"), 10, 5, "Campus learning center · 5", 1 },
                    { new Guid("e1200000-0000-4000-8000-000000000006"), "1-6-v1.wav", "Campus learning center", "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.", 1, "If you need help with a research project, our staff can arrange a short appointment.", 2, new Guid("e1200000-0000-4000-8000-000000000006"), 12, 6, "Campus learning center · 6", 1 },
                    { new Guid("e1200000-0000-4000-8000-000000000007"), "1-7-v1.wav", "Campus learning center", "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.", 1, "Before leaving the building, make sure you return any borrowed equipment to the information desk.", 2, new Guid("e1200000-0000-4000-8000-000000000007"), 12, 7, "Campus learning center · 7", 1 },
                    { new Guid("e2200000-0000-4000-8000-000000000001"), "2-1-v1.wav", "Learning new skills", "Yeni beceriler öğrenmek üzerine yapılan bir araştırma görüşmesine katılıyorsun. Soruyu dinle; ardından kendi deneyim ve görüşlerini İngilizce anlat.", 2, "Think about a skill you learned recently. What was it, and how did you start learning it?", 2, new Guid("e2200000-0000-4000-8000-000000000001"), 45, 1, "Learning new skills · 1", 1 },
                    { new Guid("e2200000-0000-4000-8000-000000000002"), "2-2-v1.wav", "Learning new skills", "Yeni beceriler öğrenmek üzerine yapılan bir araştırma görüşmesine katılıyorsun. Soruyu dinle; ardından kendi deneyim ve görüşlerini İngilizce anlat.", 2, "Some people prefer to learn a skill with a teacher, while others prefer to learn on their own. Which do you prefer, and why?", 2, new Guid("e2200000-0000-4000-8000-000000000002"), 45, 2, "Learning new skills · 2", 1 },
                    { new Guid("e2200000-0000-4000-8000-000000000003"), "2-3-v1.wav", "Learning new skills", "Yeni beceriler öğrenmek üzerine yapılan bir araştırma görüşmesine katılıyorsun. Soruyu dinle; ardından kendi deneyim ve görüşlerini İngilizce anlat.", 2, "When learning a difficult skill, is it more helpful to practice a little every day or for a longer time once a week? Explain your opinion.", 2, new Guid("e2200000-0000-4000-8000-000000000003"), 45, 3, "Learning new skills · 3", 1 },
                    { new Guid("e2200000-0000-4000-8000-000000000004"), "2-4-v1.wav", "Learning new skills", "Yeni beceriler öğrenmek üzerine yapılan bir araştırma görüşmesine katılıyorsun. Soruyu dinle; ardından kendi deneyim ve görüşlerini İngilizce anlat.", 2, "Do you think universities should give students more opportunities to learn practical skills outside their main subject? Why or why not?", 2, new Guid("e2200000-0000-4000-8000-000000000004"), 45, 4, "Learning new skills · 4", 1 }
                });

            migrationBuilder.InsertData(
                schema: "learning",
                table: "SpeakingPathSteps",
                columns: new[] { "PathId", "Position", "QuestionId" },
                values: new object[,]
                {
                    { new Guid("e1100000-0000-4000-8000-000000000001"), 1, new Guid("e1200000-0000-4000-8000-000000000001") },
                    { new Guid("e1100000-0000-4000-8000-000000000001"), 2, new Guid("e1200000-0000-4000-8000-000000000002") },
                    { new Guid("e1100000-0000-4000-8000-000000000001"), 3, new Guid("e1200000-0000-4000-8000-000000000003") },
                    { new Guid("e1100000-0000-4000-8000-000000000001"), 4, new Guid("e1200000-0000-4000-8000-000000000004") },
                    { new Guid("e1100000-0000-4000-8000-000000000001"), 5, new Guid("e1200000-0000-4000-8000-000000000005") },
                    { new Guid("e1100000-0000-4000-8000-000000000001"), 6, new Guid("e1200000-0000-4000-8000-000000000006") },
                    { new Guid("e1100000-0000-4000-8000-000000000001"), 7, new Guid("e1200000-0000-4000-8000-000000000007") },
                    { new Guid("e2100000-0000-4000-8000-000000000001"), 1, new Guid("e2200000-0000-4000-8000-000000000001") },
                    { new Guid("e2100000-0000-4000-8000-000000000001"), 2, new Guid("e2200000-0000-4000-8000-000000000002") },
                    { new Guid("e2100000-0000-4000-8000-000000000001"), 3, new Guid("e2200000-0000-4000-8000-000000000003") },
                    { new Guid("e2100000-0000-4000-8000-000000000001"), 4, new Guid("e2200000-0000-4000-8000-000000000004") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingPaths_Kind_Version",
                schema: "learning",
                table: "SpeakingPaths",
                columns: new[] { "Kind", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingPathSteps_PathId_QuestionId",
                schema: "learning",
                table: "SpeakingPathSteps",
                columns: new[] { "PathId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingPathSteps_QuestionId",
                schema: "learning",
                table: "SpeakingPathSteps",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingQuestionVersions_QuestionId_Version",
                schema: "content",
                table: "SpeakingQuestionVersions",
                columns: new[] { "QuestionId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingRuns_PathId",
                schema: "learning",
                table: "SpeakingRuns",
                column: "PathId");

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingRuns_RestartedFromId",
                schema: "learning",
                table: "SpeakingRuns",
                column: "RestartedFromId",
                unique: true,
                filter: "[RestartedFromId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingRuns_UserId_Kind",
                schema: "learning",
                table: "SpeakingRuns",
                columns: new[] { "UserId", "Kind" },
                unique: true,
                filter: "[IsCompleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingRunSteps_QuestionId",
                schema: "learning",
                table: "SpeakingRunSteps",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingRunSteps_RunId_SubmissionId",
                schema: "learning",
                table: "SpeakingRunSteps",
                columns: new[] { "RunId", "SubmissionId" },
                unique: true,
                filter: "[SubmissionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpeakingPathSteps",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "SpeakingRunSteps",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "SpeakingQuestionVersions",
                schema: "content");

            migrationBuilder.DropTable(
                name: "SpeakingRuns",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "SpeakingPaths",
                schema: "learning");
        }
    }
}
