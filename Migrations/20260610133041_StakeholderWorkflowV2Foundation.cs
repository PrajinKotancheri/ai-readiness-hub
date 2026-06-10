using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AI_Readiness_Hub.Migrations
{
    /// <inheritdoc />
    public partial class StakeholderWorkflowV2Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "ReportSections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "ReportSections",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSummary",
                table: "ReportSections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GovernanceComplianceScore",
                table: "ReadinessScores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KnowledgeGapItemId",
                table: "MeetingTranscripts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KnowledgeGapItemId",
                table: "ConsultantNotes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AIOutputSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "integer", nullable: false),
                    OutputType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OutputId = table.Column<int>(type: "integer", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SourceCategory = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SourceLabel = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    SourceReference = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EvidenceText = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIOutputSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIOutputSources_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeGapItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "integer", nullable: false),
                    AssessmentResponseId = table.Column<int>(type: "integer", nullable: true),
                    GapArea = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MissingInformation = table.Column<string>(type: "text", nullable: false),
                    WhyItMatters = table.Column<string>(type: "text", nullable: true),
                    FollowUpQuestion = table.Column<string>(type: "text", nullable: true),
                    SuggestedEvidence = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeGapItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeGapItems_AssessmentResponses_AssessmentResponseId",
                        column: x => x.AssessmentResponseId,
                        principalTable: "AssessmentResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KnowledgeGapItems_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromptDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PromptName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Goal = table.Column<string>(type: "text", nullable: true),
                    Inputs = table.Column<string>(type: "text", nullable: true),
                    Outputs = table.Column<string>(type: "text", nullable: true),
                    PlatformLocation = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    PromptText = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportTemplateSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SectionTitle = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    SectionOrder = table.Column<int>(type: "integer", nullable: false),
                    SectionGoal = table.Column<string>(type: "text", nullable: true),
                    DefaultPrompt = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTemplateSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UseCaseLibraryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ApplicableIndustries = table.Column<string>(type: "text", nullable: true),
                    SuccessCriteria = table.Column<string>(type: "text", nullable: true),
                    TypicalRoi = table.Column<string>(type: "text", nullable: true),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    CaseStudies = table.Column<string>(type: "text", nullable: true),
                    Complexity = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Dependencies = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseCaseLibraryItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTranscripts_KnowledgeGapItemId",
                table: "MeetingTranscripts",
                column: "KnowledgeGapItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantNotes_KnowledgeGapItemId",
                table: "ConsultantNotes",
                column: "KnowledgeGapItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AIOutputSources_ClientCompanyId_OutputType_OutputId",
                table: "AIOutputSources",
                columns: new[] { "ClientCompanyId", "OutputType", "OutputId" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeGapItems_AssessmentResponseId",
                table: "KnowledgeGapItems",
                column: "AssessmentResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeGapItems_ClientCompanyId_Priority",
                table: "KnowledgeGapItems",
                columns: new[] { "ClientCompanyId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeGapItems_ClientCompanyId_Status",
                table: "KnowledgeGapItems",
                columns: new[] { "ClientCompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PromptDefinitions_PromptName_VersionNumber",
                table: "PromptDefinitions",
                columns: new[] { "PromptName", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportTemplateSections_SectionOrder_SectionTitle",
                table: "ReportTemplateSections",
                columns: new[] { "SectionOrder", "SectionTitle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseLibraryItems_Name",
                table: "UseCaseLibraryItems",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultantNotes_KnowledgeGapItems_KnowledgeGapItemId",
                table: "ConsultantNotes",
                column: "KnowledgeGapItemId",
                principalTable: "KnowledgeGapItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingTranscripts_KnowledgeGapItems_KnowledgeGapItemId",
                table: "MeetingTranscripts",
                column: "KnowledgeGapItemId",
                principalTable: "KnowledgeGapItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultantNotes_KnowledgeGapItems_KnowledgeGapItemId",
                table: "ConsultantNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_MeetingTranscripts_KnowledgeGapItems_KnowledgeGapItemId",
                table: "MeetingTranscripts");

            migrationBuilder.DropTable(
                name: "AIOutputSources");

            migrationBuilder.DropTable(
                name: "KnowledgeGapItems");

            migrationBuilder.DropTable(
                name: "PromptDefinitions");

            migrationBuilder.DropTable(
                name: "ReportTemplateSections");

            migrationBuilder.DropTable(
                name: "UseCaseLibraryItems");

            migrationBuilder.DropIndex(
                name: "IX_MeetingTranscripts_KnowledgeGapItemId",
                table: "MeetingTranscripts");

            migrationBuilder.DropIndex(
                name: "IX_ConsultantNotes_KnowledgeGapItemId",
                table: "ConsultantNotes");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "ReportSections");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "ReportSections");

            migrationBuilder.DropColumn(
                name: "SourceSummary",
                table: "ReportSections");

            migrationBuilder.DropColumn(
                name: "GovernanceComplianceScore",
                table: "ReadinessScores");

            migrationBuilder.DropColumn(
                name: "KnowledgeGapItemId",
                table: "MeetingTranscripts");

            migrationBuilder.DropColumn(
                name: "KnowledgeGapItemId",
                table: "ConsultantNotes");
        }
    }
}
