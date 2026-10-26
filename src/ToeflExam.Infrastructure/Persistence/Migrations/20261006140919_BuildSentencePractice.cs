using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ToeflExam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BuildSentencePractice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "learning");

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BuildSentencePaths",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildSentencePaths", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuildSentencePathSteps",
                schema: "learning",
                columns: table => new
                {
                    PathVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    QuestionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildSentencePathSteps", x => new { x.PathVersionId, x.Position });
                    table.ForeignKey(
                        name: "FK_BuildSentencePathSteps_BuildSentencePaths_PathVersionId",
                        column: x => x.PathVersionId,
                        principalSchema: "learning",
                        principalTable: "BuildSentencePaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuildSentencePathSteps_BuildSentenceQuestionVersions_QuestionVersionId",
                        column: x => x.QuestionVersionId,
                        principalSchema: "content",
                        principalTable: "BuildSentenceQuestionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BuildSentenceRuns",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PathVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestartedFromId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildSentenceRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildSentenceRuns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuildSentenceRuns_BuildSentencePaths_PathVersionId",
                        column: x => x.PathVersionId,
                        principalSchema: "learning",
                        principalTable: "BuildSentencePaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuildSentenceRuns_BuildSentenceRuns_RestartedFromId",
                        column: x => x.RestartedFromId,
                        principalSchema: "learning",
                        principalTable: "BuildSentenceRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BuildSentenceRunSteps",
                schema: "learning",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    QuestionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RawScore = table.Column<int>(type: "int", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildSentenceRunSteps", x => new { x.RunId, x.Position });
                    table.CheckConstraint("CK_BuildSentenceRunSteps_Completion", "([RawScore] IS NULL AND [SubmissionId] IS NULL AND [CompletedAt] IS NULL) OR ([RawScore] IN (0, 1) AND [RawScore] IS NOT NULL AND [SubmissionId] IS NOT NULL AND [CompletedAt] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BuildSentenceRunSteps_BuildSentenceQuestionVersions_QuestionVersionId",
                        column: x => x.QuestionVersionId,
                        principalSchema: "content",
                        principalTable: "BuildSentenceQuestionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuildSentenceRunSteps_BuildSentenceRuns_RunId",
                        column: x => x.RunId,
                        principalSchema: "learning",
                        principalTable: "BuildSentenceRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "learning",
                table: "BuildSentencePaths",
                columns: new[] { "Id", "IsPublished", "Title", "Version" },
                values: new object[] { new Guid("b5100000-0000-4000-8000-000000000001"), true, "Build a Sentence — Başlangıç", 1 });

            migrationBuilder.InsertData(
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                columns: new[] { "Id", "AcceptedOrdersJson", "Explanation", "Prompt", "PublicationStatus", "QuestionId", "ResponsePartsJson", "TilesJson", "VersionNumber" },
                values: new object[,]
                {
                    { new Guid("b5200000-0000-4000-8000-000000000001"), "[[42,87,16]]", "Maya üçüncü tekil şahıs olduğu için geniş zamanda 'visits' kullanılır. Fiilden sonra nesne olan 'the library' gelir; zaman ifadesi cümlenin başındadır.", "What does Maya do before class?", 2, new Guid("b5200000-0000-4000-8000-000000000001"), "[{\"IsSlot\":false,\"Text\":\"Before class, Maya \"},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":false,\"Text\":\".\"}]", "[{\"Id\":87,\"Text\":\"the\"},{\"Id\":95,\"Text\":\"visit\"},{\"Id\":16,\"Text\":\"library\"},{\"Id\":42,\"Text\":\"visits\"}]", 1 },
                    { new Guid("b5200000-0000-4000-8000-000000000002"), "[[68,14]]", "Yer sorusunda 'be' fiili kullanılır: Where + is + özne. 'Does' tek başına bir konum belirtmez.", "Ask about the location of the science lab.", 2, new Guid("b5200000-0000-4000-8000-000000000002"), "[{\"IsSlot\":false,\"Text\":\"Where \"},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":false,\"Text\":\"?\"}]", "[{\"Id\":14,\"Text\":\"the science lab\"},{\"Id\":68,\"Text\":\"is\"},{\"Id\":39,\"Text\":\"does\"}]", 1 },
                    { new Guid("b5200000-0000-4000-8000-000000000003"), "[[93,27,61]]", "Geçmişteki bir olaydan önce tamamlanan eylemi past perfect anlatır: had + past participle. 'Has' çoğul özneyle uyuşmaz ve bu geçmiş zaman ilişkisini kurmaz.", "What happened before the lecture?", 2, new Guid("b5200000-0000-4000-8000-000000000003"), "[{\"IsSlot\":false,\"Text\":\"Before the lecture began, the students \"},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":false,\"Text\":\".\"}]", "[{\"Id\":45,\"Text\":\"has\"},{\"Id\":27,\"Text\":\"finished\"},{\"Id\":61,\"Text\":\"their homework\"},{\"Id\":93,\"Text\":\"had\"}]", 1 },
                    { new Guid("b5200000-0000-4000-8000-000000000004"), "[[31,82,47]]", "Dolaylı soruda yardımcı fiil ile özne yer değiştirmez: 'when the assignment is due'. 'Could you tell me' nazik bir sorudur.", "Ask politely about the deadline.", 2, new Guid("b5200000-0000-4000-8000-000000000004"), "[{\"IsSlot\":false,\"Text\":\"Could you \"},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":false,\"Text\":\"?\"}]", "[{\"Id\":19,\"Text\":\"is the assignment due\"},{\"Id\":31,\"Text\":\"tell me\"},{\"Id\":82,\"Text\":\"when\"},{\"Id\":47,\"Text\":\"the assignment is due\"}]", 1 },
                    { new Guid("b5200000-0000-4000-8000-000000000005"), "[[76,24,59]]", "'That I borrowed from the library' kitabı tanımlayan relative clause'dur. Bu yapıda 'what' yerine 'that' kullanılır.", "Describe the book you borrowed.", 2, new Guid("b5200000-0000-4000-8000-000000000005"), "[{\"IsSlot\":false,\"Text\":\"The book \"},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":false,\"Text\":\" from the library was very useful.\"}]", "[{\"Id\":24,\"Text\":\"I\"},{\"Id\":59,\"Text\":\"borrowed\"},{\"Id\":76,\"Text\":\"that\"},{\"Id\":83,\"Text\":\"what\"}]", 1 },
                    { new Guid("b5200000-0000-4000-8000-000000000006"), "[[18,64,91]]", "Birinci koşul yapısında if cümlesi geniş zaman, sonuç cümlesi will + fiil kullanır.", "What will you do if the weather improves?", 2, new Guid("b5200000-0000-4000-8000-000000000006"), "[{\"IsSlot\":false,\"Text\":\"If \"},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":false,\"Text\":\".\"}]", "[{\"Id\":64,\"Text\":\"we will have\"},{\"Id\":91,\"Text\":\"the meeting outside\"},{\"Id\":18,\"Text\":\"the weather improves,\"},{\"Id\":37,\"Text\":\"the weather will improve,\"}]", 1 },
                    { new Guid("b5200000-0000-4000-8000-000000000007"), "[[54,29,73]]", "Tekil 'this course' öznesi 'is' alır. Karşılaştırmalı 'more challenging' ifadesinden sonra karşılaştırılan ders 'than' ile belirtilir.", "Compare the two courses.", 2, new Guid("b5200000-0000-4000-8000-000000000007"), "[{\"IsSlot\":false,\"Text\":\"This course \"},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":false,\"Text\":\".\"}]", "[{\"Id\":11,\"Text\":\"are\"},{\"Id\":73,\"Text\":\"the introductory course\"},{\"Id\":54,\"Text\":\"is\"},{\"Id\":29,\"Text\":\"more challenging than\"}]", 1 },
                    { new Guid("b5200000-0000-4000-8000-000000000008"), "[[86,41,22]]", "'Decide' fiilinden sonra to-infinitive kullanılır. Bir burs için başvurmak 'apply for a scholarship' şeklindedir.", "Explain the student's decision.", 2, new Guid("b5200000-0000-4000-8000-000000000008"), "[{\"IsSlot\":false,\"Text\":\"She decided \"},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":false,\"Text\":\".\"}]", "[{\"Id\":41,\"Text\":\"apply for\"},{\"Id\":22,\"Text\":\"the scholarship\"},{\"Id\":86,\"Text\":\"to\"},{\"Id\":67,\"Text\":\"applying\"}]", 1 },
                    { new Guid("b5200000-0000-4000-8000-000000000009"), "[[35,92,48]]", "Tekil özneyle geçmiş zaman edilgen yapı 'was revised' olur. 'By' eylemi yapan kişiyi, 'yesterday' zamanı belirtir.", "Describe the new schedule.", 2, new Guid("b5200000-0000-4000-8000-000000000009"), "[{\"IsSlot\":false,\"Text\":\"Yesterday, the schedule \"},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":false,\"Text\":\".\"}]", "[{\"Id\":92,\"Text\":\"revised by\"},{\"Id\":48,\"Text\":\"the coordinator\"},{\"Id\":35,\"Text\":\"was\"},{\"Id\":17,\"Text\":\"were\"}]", 1 },
                    { new Guid("b5200000-0000-4000-8000-000000000010"), "[[63,26,81]]", "Geçmişte başlayıp süren eylem için present perfect continuous kullanılabilir. Çoğul 'researchers' öznesi 'have been' alır; başlangıç yılı 'since' ile belirtilir.", "Say how long the researchers have worked here.", 2, new Guid("b5200000-0000-4000-8000-000000000010"), "[{\"IsSlot\":false,\"Text\":\"Since 2021, the researchers \"},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":true,\"Text\":null},{\"IsSlot\":false,\"Text\":\".\"}]", "[{\"Id\":44,\"Text\":\"has been\"},{\"Id\":63,\"Text\":\"have been\"},{\"Id\":81,\"Text\":\"this university\"},{\"Id\":26,\"Text\":\"working at\"}]", 1 }
                });

            migrationBuilder.InsertData(
                schema: "learning",
                table: "BuildSentencePathSteps",
                columns: new[] { "PathVersionId", "Position", "QuestionVersionId" },
                values: new object[,]
                {
                    { new Guid("b5100000-0000-4000-8000-000000000001"), 1, new Guid("b5200000-0000-4000-8000-000000000001") },
                    { new Guid("b5100000-0000-4000-8000-000000000001"), 2, new Guid("b5200000-0000-4000-8000-000000000002") },
                    { new Guid("b5100000-0000-4000-8000-000000000001"), 3, new Guid("b5200000-0000-4000-8000-000000000003") },
                    { new Guid("b5100000-0000-4000-8000-000000000001"), 4, new Guid("b5200000-0000-4000-8000-000000000004") },
                    { new Guid("b5100000-0000-4000-8000-000000000001"), 5, new Guid("b5200000-0000-4000-8000-000000000005") },
                    { new Guid("b5100000-0000-4000-8000-000000000001"), 6, new Guid("b5200000-0000-4000-8000-000000000006") },
                    { new Guid("b5100000-0000-4000-8000-000000000001"), 7, new Guid("b5200000-0000-4000-8000-000000000007") },
                    { new Guid("b5100000-0000-4000-8000-000000000001"), 8, new Guid("b5200000-0000-4000-8000-000000000008") },
                    { new Guid("b5100000-0000-4000-8000-000000000001"), 9, new Guid("b5200000-0000-4000-8000-000000000009") },
                    { new Guid("b5100000-0000-4000-8000-000000000001"), 10, new Guid("b5200000-0000-4000-8000-000000000010") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BuildSentencePaths_Version",
                schema: "learning",
                table: "BuildSentencePaths",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildSentencePathSteps_PathVersionId_QuestionVersionId",
                schema: "learning",
                table: "BuildSentencePathSteps",
                columns: new[] { "PathVersionId", "QuestionVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildSentencePathSteps_QuestionVersionId",
                schema: "learning",
                table: "BuildSentencePathSteps",
                column: "QuestionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildSentenceRuns_PathVersionId",
                schema: "learning",
                table: "BuildSentenceRuns",
                column: "PathVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildSentenceRuns_RestartedFromId",
                schema: "learning",
                table: "BuildSentenceRuns",
                column: "RestartedFromId",
                unique: true,
                filter: "[RestartedFromId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BuildSentenceRuns_UserId",
                schema: "learning",
                table: "BuildSentenceRuns",
                column: "UserId",
                unique: true,
                filter: "[IsCompleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BuildSentenceRunSteps_QuestionVersionId",
                schema: "learning",
                table: "BuildSentenceRunSteps",
                column: "QuestionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildSentenceRunSteps_RunId_SubmissionId",
                schema: "learning",
                table: "BuildSentenceRunSteps",
                columns: new[] { "RunId", "SubmissionId" },
                unique: true,
                filter: "[SubmissionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BuildSentencePathSteps",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "BuildSentenceRunSteps",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "BuildSentenceRuns",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "BuildSentencePaths",
                schema: "learning");

            migrationBuilder.DeleteData(
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("b5200000-0000-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("b5200000-0000-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("b5200000-0000-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("b5200000-0000-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("b5200000-0000-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("b5200000-0000-4000-8000-000000000006"));

            migrationBuilder.DeleteData(
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("b5200000-0000-4000-8000-000000000007"));

            migrationBuilder.DeleteData(
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("b5200000-0000-4000-8000-000000000008"));

            migrationBuilder.DeleteData(
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("b5200000-0000-4000-8000-000000000009"));

            migrationBuilder.DeleteData(
                schema: "content",
                table: "BuildSentenceQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("b5200000-0000-4000-8000-000000000010"));

            migrationBuilder.DropColumn(
                name: "Explanation",
                schema: "content",
                table: "BuildSentenceQuestionVersions");
        }
    }
}
