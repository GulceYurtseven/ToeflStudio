using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToeflExam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SpeakingEnglishInstructions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000001"),
                column: "Instructions",
                value: "You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000002"),
                column: "Instructions",
                value: "You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000003"),
                column: "Instructions",
                value: "You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000004"),
                column: "Instructions",
                value: "You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000005"),
                column: "Instructions",
                value: "You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000006"),
                column: "Instructions",
                value: "You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000007"),
                column: "Instructions",
                value: "You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e2200000-0000-4000-8000-000000000001"),
                column: "Instructions",
                value: "You are taking part in a research interview about learning new skills. Listen to each question, then respond in English with relevant details and examples.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e2200000-0000-4000-8000-000000000002"),
                column: "Instructions",
                value: "You are taking part in a research interview about learning new skills. Listen to each question, then respond in English with relevant details and examples.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e2200000-0000-4000-8000-000000000003"),
                column: "Instructions",
                value: "You are taking part in a research interview about learning new skills. Listen to each question, then respond in English with relevant details and examples.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e2200000-0000-4000-8000-000000000004"),
                column: "Instructions",
                value: "You are taking part in a research interview about learning new skills. Listen to each question, then respond in English with relevant details and examples.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000001"),
                column: "Instructions",
                value: "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000002"),
                column: "Instructions",
                value: "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000003"),
                column: "Instructions",
                value: "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000004"),
                column: "Instructions",
                value: "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000005"),
                column: "Instructions",
                value: "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000006"),
                column: "Instructions",
                value: "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e1200000-0000-4000-8000-000000000007"),
                column: "Instructions",
                value: "Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e2200000-0000-4000-8000-000000000001"),
                column: "Instructions",
                value: "Yeni beceriler öğrenmek üzerine yapılan bir araştırma görüşmesine katılıyorsun. Soruyu dinle; ardından kendi deneyim ve görüşlerini İngilizce anlat.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e2200000-0000-4000-8000-000000000002"),
                column: "Instructions",
                value: "Yeni beceriler öğrenmek üzerine yapılan bir araştırma görüşmesine katılıyorsun. Soruyu dinle; ardından kendi deneyim ve görüşlerini İngilizce anlat.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e2200000-0000-4000-8000-000000000003"),
                column: "Instructions",
                value: "Yeni beceriler öğrenmek üzerine yapılan bir araştırma görüşmesine katılıyorsun. Soruyu dinle; ardından kendi deneyim ve görüşlerini İngilizce anlat.");

            migrationBuilder.UpdateData(
                schema: "content",
                table: "SpeakingQuestionVersions",
                keyColumn: "Id",
                keyValue: new Guid("e2200000-0000-4000-8000-000000000004"),
                column: "Instructions",
                value: "Yeni beceriler öğrenmek üzerine yapılan bir araştırma görüşmesine katılıyorsun. Soruyu dinle; ardından kendi deneyim ve görüşlerini İngilizce anlat.");
        }
    }
}
