using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ToeflExam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WritingPractice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WritingPaths",
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
                    table.PrimaryKey("PK_WritingPaths", x => x.Id);
                    table.CheckConstraint("CK_WritingPath_KindVersion", "[Kind] IN (1,2) AND [Version] > 0");
                });

            migrationBuilder.CreateTable(
                name: "WritingQuestionVersions",
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
                    RequirementsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PostsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 6000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingQuestionVersions", x => x.Id);
                    table.CheckConstraint("CK_WritingQuestion_Json", "ISJSON([RequirementsJson], ARRAY) = 1 AND ISJSON([PostsJson], ARRAY) = 1");
                    table.CheckConstraint("CK_WritingQuestion_Kind", "[Kind] IN (1,2)");
                    table.CheckConstraint("CK_WritingQuestion_Status", "[PublicationStatus] BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_WritingQuestion_Version", "[Version] > 0");
                });

            migrationBuilder.CreateTable(
                name: "WritingRuns",
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
                    table.PrimaryKey("PK_WritingRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WritingRuns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WritingRuns_WritingPaths_PathId",
                        column: x => x.PathId,
                        principalSchema: "learning",
                        principalTable: "WritingPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WritingRuns_WritingRuns_RestartedFromId",
                        column: x => x.RestartedFromId,
                        principalSchema: "learning",
                        principalTable: "WritingRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WritingPathSteps",
                schema: "learning",
                columns: table => new
                {
                    PathId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingPathSteps", x => new { x.PathId, x.Position });
                    table.ForeignKey(
                        name: "FK_WritingPathSteps_WritingPaths_PathId",
                        column: x => x.PathId,
                        principalSchema: "learning",
                        principalTable: "WritingPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WritingPathSteps_WritingQuestionVersions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "content",
                        principalTable: "WritingQuestionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WritingRunSteps",
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
                    WordCount = table.Column<int>(type: "int", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RubricVersion = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingRunSteps", x => new { x.RunId, x.Position });
                    table.CheckConstraint("CK_WritingStep_Score", "[Score] IS NULL OR ([Score] BETWEEN 0 AND 5 AND [WordCount] >= 0)");
                    table.CheckConstraint("CK_WritingStep_State", "([Score] IS NULL AND [CompletedAt] IS NULL AND [WordCount] IS NULL AND [Model] IS NULL AND [RubricVersion] IS NULL AND (([SubmissionId] IS NULL AND [LeaseToken] IS NULL AND [LeaseUntil] IS NULL) OR ([SubmissionId] IS NOT NULL AND [LeaseToken] IS NOT NULL AND [LeaseUntil] IS NOT NULL))) OR ([Score] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [WordCount] IS NOT NULL AND [Model] IS NOT NULL AND [RubricVersion] IS NOT NULL AND [SubmissionId] IS NOT NULL AND [LeaseToken] IS NULL AND [LeaseUntil] IS NULL)");
                    table.ForeignKey(
                        name: "FK_WritingRunSteps_WritingQuestionVersions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "content",
                        principalTable: "WritingQuestionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WritingRunSteps_WritingRuns_RunId",
                        column: x => x.RunId,
                        principalSchema: "learning",
                        principalTable: "WritingRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "learning",
                table: "WritingPaths",
                columns: new[] { "Id", "IsPublished", "Kind", "Title", "Version" },
                values: new object[,]
                {
                    { new Guid("c1100000-0000-4000-8000-000000000001"), true, 1, "Write an Email — Başlangıç", 1 },
                    { new Guid("c2100000-0000-4000-8000-000000000001"), true, 2, "Academic Discussion — Başlangıç", 1 }
                });

            migrationBuilder.InsertData(
                schema: "content",
                table: "WritingQuestionVersions",
                columns: new[] { "Id", "Context", "Instructions", "Kind", "PostsJson", "PublicationStatus", "QuestionId", "RequirementsJson", "Title", "Version" },
                values: new object[,]
                {
                    { new Guid("c1200000-0000-4000-8000-000000000001"), "You organize a study group of six students. Your usual room is unavailable next Thursday. Write to the university library coordinator, Ms. Chen.", "Write an email in English. Address all three points and use an appropriate tone for the recipient.", 1, "[]", 2, new Guid("c1200000-0000-4000-8000-000000000001"), "[\"Explain why your group needs a room.\",\"Describe the time and equipment you need.\",\"Ask about booking an alternative room.\"]", "Request a study room", 1 },
                    { new Guid("c1200000-0000-4000-8000-000000000002"), "A required laboratory session overlaps with a workshop you registered for. Write to the workshop organizer, Mr. Rivera.", "Write an email in English. Address all three points and use an appropriate tone for the recipient.", 1, "[]", 2, new Guid("c1200000-0000-4000-8000-000000000002"), "[\"Explain the scheduling conflict.\",\"Explain why attending the workshop matters to you.\",\"Suggest a practical alternative and ask whether it is possible.\"]", "Resolve a timetable conflict", 1 },
                    { new Guid("c1200000-0000-4000-8000-000000000003"), "The shared kitchen in your university residence has had a broken refrigerator for two days. Write to the housing manager, Ms. Ali.", "Write an email in English. Address all three points and use an appropriate tone for the recipient.", 1, "[]", 2, new Guid("c1200000-0000-4000-8000-000000000003"), "[\"Describe the problem and its impact.\",\"Explain what residents have already tried.\",\"Request a repair and suggest a temporary solution.\"]", "Report a housing problem", 1 },
                    { new Guid("c1200000-0000-4000-8000-000000000004"), "You attended a university careers event. The talks were helpful, but students had little opportunity to ask questions. Write to the event coordinator, Dr. Brooks.", "Write an email in English. Address all three points and use an appropriate tone for the recipient.", 1, "[]", 2, new Guid("c1200000-0000-4000-8000-000000000004"), "[\"Mention something you appreciated.\",\"Explain the difficulty you experienced.\",\"Propose a specific improvement for the next event.\"]", "Improve a campus event", 1 },
                    { new Guid("c1200000-0000-4000-8000-000000000005"), "Your student club wants to clean a nearby park with a community organization. Write to its director, Ms. Patel.", "Write an email in English. Address all three points and use an appropriate tone for the recipient.", 1, "[]", 2, new Guid("c1200000-0000-4000-8000-000000000005"), "[\"Introduce the club and explain your goal.\",\"Propose a date and describe what volunteers can contribute.\",\"Ask what arrangements or permissions are needed.\"]", "Organize a volunteer project", 1 },
                    { new Guid("c2200000-0000-4000-8000-000000000001"), "Professor Allen: Some universities record every lecture, while others prioritize in-person attendance. Should recordings be available to all students? Explain your position.", "Write a contribution to the discussion in English. State and support your opinion in your own words. Aim for at least 100 words; length alone does not determine the score.", 2, "[{\"Name\":\"Maya\",\"Text\":\"Recordings help students review difficult topics and allow students who are ill to catch up.\"},{\"Name\":\"Leo\",\"Text\":\"Students may stop attending classes if recordings are always available, reducing participation.\"}]", 2, new Guid("c2200000-0000-4000-8000-000000000001"), "[\"State your position on the professor\\u0027s question.\",\"Develop your reasoning with a relevant explanation or example.\",\"Contribute your own ideas to the discussion.\"]", "Recorded university lectures", 1 },
                    { new Guid("c2200000-0000-4000-8000-000000000002"), "Professor Kim: A city is considering limiting private cars in its center. Would this policy improve life for residents? Explain your view.", "Write a contribution to the discussion in English. State and support your opinion in your own words. Aim for at least 100 words; length alone does not determine the score.", 2, "[{\"Name\":\"Nora\",\"Text\":\"Fewer cars would make streets quieter and safer for people walking or cycling.\"},{\"Name\":\"Omar\",\"Text\":\"Some residents cannot easily use public transportation, and local businesses need deliveries.\"}]", 2, new Guid("c2200000-0000-4000-8000-000000000002"), "[\"State your position on the professor\\u0027s question.\",\"Develop your reasoning with a relevant explanation or example.\",\"Contribute your own ideas to the discussion.\"]", "A car-free city center", 1 },
                    { new Guid("c2200000-0000-4000-8000-000000000003"), "Professor Singh: Should university courses assess students mainly through group projects or individual assignments? Explain your preference.", "Write a contribution to the discussion in English. State and support your opinion in your own words. Aim for at least 100 words; length alone does not determine the score.", 2, "[{\"Name\":\"Ella\",\"Text\":\"Group projects teach collaboration and expose students to different approaches.\"},{\"Name\":\"Mateo\",\"Text\":\"Individual assignments make it easier to recognize each student\\u0027s effort and understanding.\"}]", 2, new Guid("c2200000-0000-4000-8000-000000000003"), "[\"State your position on the professor\\u0027s question.\",\"Develop your reasoning with a relevant explanation or example.\",\"Contribute your own ideas to the discussion.\"]", "Group projects at university", 1 },
                    { new Guid("c2200000-0000-4000-8000-000000000004"), "Professor Davis: A town has funding for either more digital library services or longer opening hours at existing libraries. Which should it prioritize?", "Write a contribution to the discussion in English. State and support your opinion in your own words. Aim for at least 100 words; length alone does not determine the score.", 2, "[{\"Name\":\"Aisha\",\"Text\":\"Digital access reaches people who live far away and lets them study whenever they want.\"},{\"Name\":\"Ben\",\"Text\":\"Longer opening hours help people who need quiet space, reliable internet, or personal assistance.\"}]", 2, new Guid("c2200000-0000-4000-8000-000000000004"), "[\"State your position on the professor\\u0027s question.\",\"Develop your reasoning with a relevant explanation or example.\",\"Contribute your own ideas to the discussion.\"]", "Funding public libraries", 1 },
                    { new Guid("c2200000-0000-4000-8000-000000000005"), "Professor Rossi: Some people believe universities should require a practical life-skills course, such as personal budgeting. Should such a course be compulsory?", "Write a contribution to the discussion in English. State and support your opinion in your own words. Aim for at least 100 words; length alone does not determine the score.", 2, "[{\"Name\":\"Sofia\",\"Text\":\"A required course could help every student handle common responsibilities after graduation.\"},{\"Name\":\"Daniel\",\"Text\":\"Degree programs are already demanding, so students should choose optional workshops instead.\"}]", 2, new Guid("c2200000-0000-4000-8000-000000000005"), "[\"State your position on the professor\\u0027s question.\",\"Develop your reasoning with a relevant explanation or example.\",\"Contribute your own ideas to the discussion.\"]", "Learning practical skills", 1 }
                });

            migrationBuilder.InsertData(
                schema: "learning",
                table: "WritingPathSteps",
                columns: new[] { "PathId", "Position", "QuestionId" },
                values: new object[,]
                {
                    { new Guid("c1100000-0000-4000-8000-000000000001"), 1, new Guid("c1200000-0000-4000-8000-000000000001") },
                    { new Guid("c1100000-0000-4000-8000-000000000001"), 2, new Guid("c1200000-0000-4000-8000-000000000002") },
                    { new Guid("c1100000-0000-4000-8000-000000000001"), 3, new Guid("c1200000-0000-4000-8000-000000000003") },
                    { new Guid("c1100000-0000-4000-8000-000000000001"), 4, new Guid("c1200000-0000-4000-8000-000000000004") },
                    { new Guid("c1100000-0000-4000-8000-000000000001"), 5, new Guid("c1200000-0000-4000-8000-000000000005") },
                    { new Guid("c2100000-0000-4000-8000-000000000001"), 1, new Guid("c2200000-0000-4000-8000-000000000001") },
                    { new Guid("c2100000-0000-4000-8000-000000000001"), 2, new Guid("c2200000-0000-4000-8000-000000000002") },
                    { new Guid("c2100000-0000-4000-8000-000000000001"), 3, new Guid("c2200000-0000-4000-8000-000000000003") },
                    { new Guid("c2100000-0000-4000-8000-000000000001"), 4, new Guid("c2200000-0000-4000-8000-000000000004") },
                    { new Guid("c2100000-0000-4000-8000-000000000001"), 5, new Guid("c2200000-0000-4000-8000-000000000005") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WritingPaths_Kind_Version",
                schema: "learning",
                table: "WritingPaths",
                columns: new[] { "Kind", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WritingPathSteps_PathId_QuestionId",
                schema: "learning",
                table: "WritingPathSteps",
                columns: new[] { "PathId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WritingPathSteps_QuestionId",
                schema: "learning",
                table: "WritingPathSteps",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_WritingQuestionVersions_QuestionId_Version",
                schema: "content",
                table: "WritingQuestionVersions",
                columns: new[] { "QuestionId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WritingRuns_PathId",
                schema: "learning",
                table: "WritingRuns",
                column: "PathId");

            migrationBuilder.CreateIndex(
                name: "IX_WritingRuns_RestartedFromId",
                schema: "learning",
                table: "WritingRuns",
                column: "RestartedFromId",
                unique: true,
                filter: "[RestartedFromId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WritingRuns_UserId_Kind",
                schema: "learning",
                table: "WritingRuns",
                columns: new[] { "UserId", "Kind" },
                unique: true,
                filter: "[IsCompleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WritingRunSteps_QuestionId",
                schema: "learning",
                table: "WritingRunSteps",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_WritingRunSteps_RunId_SubmissionId",
                schema: "learning",
                table: "WritingRunSteps",
                columns: new[] { "RunId", "SubmissionId" },
                unique: true,
                filter: "[SubmissionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WritingPathSteps",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "WritingRunSteps",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "WritingQuestionVersions",
                schema: "content");

            migrationBuilder.DropTable(
                name: "WritingRuns",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "WritingPaths",
                schema: "learning");
        }
    }
}
