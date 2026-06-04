using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AI_Readiness_Hub.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssessmentResponseId",
                table: "AssessmentAnswers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssessmentResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReadinessAssessmentId = table.Column<int>(type: "integer", nullable: false),
                    ResponseNumber = table.Column<int>(type: "integer", nullable: false),
                    ResponseLabel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ExternalResponseId = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    ClientToken = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnswerCount = table.Column<int>(type: "integer", nullable: false),
                    RawResponseJson = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentResponses_ReadinessAssessments_ReadinessAssessmen~",
                        column: x => x.ReadinessAssessmentId,
                        principalTable: "ReadinessAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "AssessmentResponses"
                    ("ReadinessAssessmentId", "ResponseNumber", "ResponseLabel", "Source", "ExternalResponseId", "ClientToken", "SubmittedAt", "ReceivedAt", "AnswerCount", "RawResponseJson", "Status", "CreatedAt", "LastModifiedAt")
                SELECT
                    answers."ReadinessAssessmentId",
                    1,
                    'First response',
                    'ExistingImport',
                    assessments."ExternalResponseId",
                    assessments."ClientToken",
                    COALESCE(assessments."CompletedAt", assessments."ImportedAt", assessments."ResponseReceivedAt", MIN(answers."CreatedAt")),
                    COALESCE(assessments."ResponseReceivedAt", assessments."CompletedAt", assessments."ImportedAt", MIN(answers."CreatedAt"), NOW()),
                    COUNT(*)::integer,
                    assessments."RawResponseJson",
                    CASE
                        WHEN assessments."FormStatus" = 'Completed' THEN 'Received'
                        ELSE 'Imported'
                    END,
                    COALESCE(MIN(answers."CreatedAt"), NOW()),
                    assessments."LastModifiedAt"
                FROM "AssessmentAnswers" AS answers
                INNER JOIN "ReadinessAssessments" AS assessments
                    ON assessments."Id" = answers."ReadinessAssessmentId"
                WHERE answers."AssessmentResponseId" IS NULL
                GROUP BY
                    answers."ReadinessAssessmentId",
                    assessments."ExternalResponseId",
                    assessments."ClientToken",
                    assessments."CompletedAt",
                    assessments."ImportedAt",
                    assessments."ResponseReceivedAt",
                    assessments."RawResponseJson",
                    assessments."FormStatus",
                    assessments."LastModifiedAt";

                UPDATE "AssessmentAnswers" AS answers
                SET "AssessmentResponseId" = responses."Id"
                FROM "AssessmentResponses" AS responses
                WHERE responses."ReadinessAssessmentId" = answers."ReadinessAssessmentId"
                    AND responses."ResponseNumber" = 1
                    AND answers."AssessmentResponseId" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAnswers_AssessmentResponseId",
                table: "AssessmentAnswers",
                column: "AssessmentResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponses_ReadinessAssessmentId_ExternalResponseId",
                table: "AssessmentResponses",
                columns: new[] { "ReadinessAssessmentId", "ExternalResponseId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponses_ReadinessAssessmentId_ResponseNumber",
                table: "AssessmentResponses",
                columns: new[] { "ReadinessAssessmentId", "ResponseNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentAnswers_AssessmentResponses_AssessmentResponseId",
                table: "AssessmentAnswers",
                column: "AssessmentResponseId",
                principalTable: "AssessmentResponses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentAnswers_AssessmentResponses_AssessmentResponseId",
                table: "AssessmentAnswers");

            migrationBuilder.DropTable(
                name: "AssessmentResponses");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentAnswers_AssessmentResponseId",
                table: "AssessmentAnswers");

            migrationBuilder.DropColumn(
                name: "AssessmentResponseId",
                table: "AssessmentAnswers");
        }
    }
}
