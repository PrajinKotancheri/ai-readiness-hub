using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AI_Readiness_Hub.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Industry = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Region = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    CompanySizeRange = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    RevenueRange = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    BusinessModel = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ContactPersonName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ContactPersonEmail = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    ContactPersonPhone = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    ConsultingPackage = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    AssignedConsultant = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CurrentStage = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    OverallReadinessScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    KeyRisksSummary = table.Column<string>(type: "TEXT", nullable: true),
                    NextAction = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientCompanies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIAnalysisOutputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    AnalysisType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    InputSummary = table.Column<string>(type: "TEXT", nullable: true),
                    OutputContent = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GeneratedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIAnalysisOutputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIAnalysisOutputs_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIUseCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessProblem = table.Column<string>(type: "TEXT", nullable: true),
                    Department = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ExpectedBenefit = table.Column<string>(type: "TEXT", nullable: true),
                    RequiredData = table.Column<string>(type: "TEXT", nullable: true),
                    ImplementationComplexity = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    RiskLevel = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    TimeToValue = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIUseCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIUseCases_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityType = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientActivityLogs_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DocumentType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UploadedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    AiSummary = table.Column<string>(type: "TEXT", nullable: true),
                    KeyInsights = table.Column<string>(type: "TEXT", nullable: true),
                    UsedInReport = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientDocuments_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReportTitle = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    ReportStatus = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalReportContent = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientReports_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    TaskTitle = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    TaskDescription = table.Column<string>(type: "TEXT", nullable: true),
                    TaskType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    AssignedTo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientTasks_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientWorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    StageName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientWorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientWorkflowSteps_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetitorInsights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    CompetitorName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                    InsightText = table.Column<string>(type: "TEXT", nullable: false),
                    AiUseCasesObserved = table.Column<string>(type: "TEXT", nullable: true),
                    StrengthComparedToClient = table.Column<string>(type: "TEXT", nullable: true),
                    WeaknessComparedToClient = table.Column<string>(type: "TEXT", nullable: true),
                    SourceNotes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitorInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitorInsights_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsultantNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    NoteTitle = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    NoteText = table.Column<string>(type: "TEXT", nullable: false),
                    NoteType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultantNotes_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GapAnalysisItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    GapArea = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    IssueDescription = table.Column<string>(type: "TEXT", nullable: false),
                    Impact = table.Column<string>(type: "TEXT", nullable: true),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    SuggestedFollowUpQuestion = table.Column<string>(type: "TEXT", nullable: true),
                    SuggestedAction = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GapAnalysisItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GapAnalysisItems_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndustryInsights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Topic = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    InsightText = table.Column<string>(type: "TEXT", nullable: false),
                    Relevance = table.Column<string>(type: "TEXT", nullable: true),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndustryInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndustryInsights_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingTranscripts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionTitle = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    SessionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TranscriptText = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    KeyDecisions = table.Column<string>(type: "TEXT", nullable: true),
                    FollowUpQuestions = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingTranscripts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingTranscripts_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReadinessAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormStatus = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    FormUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RawResponseJson = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadinessAssessments_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReadinessScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    BusinessClarityScore = table.Column<int>(type: "INTEGER", nullable: false),
                    DataReadinessScore = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessReadinessScore = table.Column<int>(type: "INTEGER", nullable: false),
                    TechnologyReadinessScore = table.Column<int>(type: "INTEGER", nullable: false),
                    PeopleGovernanceScore = table.Column<int>(type: "INTEGER", nullable: false),
                    OverallScore = table.Column<int>(type: "INTEGER", nullable: false),
                    ScoreCategory = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ScoringSummary = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadinessScores_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SwotAnalysisItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceSource = table.Column<string>(type: "TEXT", nullable: true),
                    ConsultantComment = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwotAnalysisItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SwotAnalysisItems_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIRoadmapItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Phase = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    RelatedUseCaseId = table.Column<int>(type: "INTEGER", nullable: true),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ExpectedOutcome = table.Column<string>(type: "TEXT", nullable: true),
                    Dependencies = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRoadmapItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIRoadmapItems_AIUseCases_RelatedUseCaseId",
                        column: x => x.RelatedUseCaseId,
                        principalTable: "AIUseCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AIRoadmapItems_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIUseCaseScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AIUseCaseId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoiScore = table.Column<int>(type: "INTEGER", nullable: false),
                    FeasibilityScore = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskSafetyScore = table.Column<int>(type: "INTEGER", nullable: false),
                    StrategicFitScore = table.Column<int>(type: "INTEGER", nullable: false),
                    DataReadinessScore = table.Column<int>(type: "INTEGER", nullable: false),
                    PriorityScore = table.Column<decimal>(type: "TEXT", precision: 4, scale: 2, nullable: false),
                    ScoringComment = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIUseCaseScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIUseCaseScores_AIUseCases_AIUseCaseId",
                        column: x => x.AIUseCaseId,
                        principalTable: "AIUseCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientReportId = table.Column<int>(type: "INTEGER", nullable: false),
                    SectionTitle = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    SectionOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    SectionContent = table.Column<string>(type: "TEXT", nullable: true),
                    SectionStatus = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportSections_ClientReports_ClientReportId",
                        column: x => x.ClientReportId,
                        principalTable: "ClientReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReadinessAssessmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    SectionName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    QuestionText = table.Column<string>(type: "TEXT", nullable: false),
                    AnswerText = table.Column<string>(type: "TEXT", nullable: true),
                    AnswerType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    IsMandatory = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletenessStatus = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentAnswers_ReadinessAssessments_ReadinessAssessmentId",
                        column: x => x.ReadinessAssessmentId,
                        principalTable: "ReadinessAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIAnalysisOutputs_ClientCompanyId",
                table: "AIAnalysisOutputs",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AIRoadmapItems_ClientCompanyId",
                table: "AIRoadmapItems",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AIRoadmapItems_RelatedUseCaseId",
                table: "AIRoadmapItems",
                column: "RelatedUseCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_AIUseCases_ClientCompanyId",
                table: "AIUseCases",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AIUseCaseScores_AIUseCaseId",
                table: "AIUseCaseScores",
                column: "AIUseCaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAnswers_ReadinessAssessmentId",
                table: "AssessmentAnswers",
                column: "ReadinessAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientActivityLogs_ClientCompanyId",
                table: "ClientActivityLogs",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCompanies_CompanyName",
                table: "ClientCompanies",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_ClientDocuments_ClientCompanyId",
                table: "ClientDocuments",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientReports_ClientCompanyId",
                table: "ClientReports",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTasks_ClientCompanyId",
                table: "ClientTasks",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientWorkflowSteps_ClientCompanyId",
                table: "ClientWorkflowSteps",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorInsights_ClientCompanyId",
                table: "CompetitorInsights",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantNotes_ClientCompanyId",
                table: "ConsultantNotes",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_GapAnalysisItems_ClientCompanyId",
                table: "GapAnalysisItems",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_IndustryInsights_ClientCompanyId",
                table: "IndustryInsights",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTranscripts_ClientCompanyId",
                table: "MeetingTranscripts",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessAssessments_ClientCompanyId",
                table: "ReadinessAssessments",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessScores_ClientCompanyId",
                table: "ReadinessScores",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSections_ClientReportId",
                table: "ReportSections",
                column: "ClientReportId");

            migrationBuilder.CreateIndex(
                name: "IX_SwotAnalysisItems_ClientCompanyId",
                table: "SwotAnalysisItems",
                column: "ClientCompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIAnalysisOutputs");

            migrationBuilder.DropTable(
                name: "AIRoadmapItems");

            migrationBuilder.DropTable(
                name: "AIUseCaseScores");

            migrationBuilder.DropTable(
                name: "AssessmentAnswers");

            migrationBuilder.DropTable(
                name: "ClientActivityLogs");

            migrationBuilder.DropTable(
                name: "ClientDocuments");

            migrationBuilder.DropTable(
                name: "ClientTasks");

            migrationBuilder.DropTable(
                name: "ClientWorkflowSteps");

            migrationBuilder.DropTable(
                name: "CompetitorInsights");

            migrationBuilder.DropTable(
                name: "ConsultantNotes");

            migrationBuilder.DropTable(
                name: "GapAnalysisItems");

            migrationBuilder.DropTable(
                name: "IndustryInsights");

            migrationBuilder.DropTable(
                name: "MeetingTranscripts");

            migrationBuilder.DropTable(
                name: "ReadinessScores");

            migrationBuilder.DropTable(
                name: "ReportSections");

            migrationBuilder.DropTable(
                name: "SwotAnalysisItems");

            migrationBuilder.DropTable(
                name: "AIUseCases");

            migrationBuilder.DropTable(
                name: "ReadinessAssessments");

            migrationBuilder.DropTable(
                name: "ClientReports");

            migrationBuilder.DropTable(
                name: "ClientCompanies");
        }
    }
}
