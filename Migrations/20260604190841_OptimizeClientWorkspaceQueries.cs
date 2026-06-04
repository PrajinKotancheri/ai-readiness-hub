using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI_Readiness_Hub.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeClientWorkspaceQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeetingTranscripts_ClientCompanyId",
                table: "MeetingTranscripts");

            migrationBuilder.DropIndex(
                name: "IX_GapAnalysisItems_ClientCompanyId",
                table: "GapAnalysisItems");

            migrationBuilder.DropIndex(
                name: "IX_ConsultantNotes_ClientCompanyId",
                table: "ConsultantNotes");

            migrationBuilder.DropIndex(
                name: "IX_ClientTasks_ClientCompanyId",
                table: "ClientTasks");

            migrationBuilder.DropIndex(
                name: "IX_ClientReports_ClientCompanyId",
                table: "ClientReports");

            migrationBuilder.DropIndex(
                name: "IX_ClientDocuments_ClientCompanyId",
                table: "ClientDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ClientActivityLogs_ClientCompanyId",
                table: "ClientActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_AIAnalysisOutputs_ClientCompanyId",
                table: "AIAnalysisOutputs");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTranscripts_ClientCompanyId_SessionDate",
                table: "MeetingTranscripts",
                columns: new[] { "ClientCompanyId", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GapAnalysisItems_ClientCompanyId_Status",
                table: "GapAnalysisItems",
                columns: new[] { "ClientCompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantNotes_ClientCompanyId_CreatedAt",
                table: "ConsultantNotes",
                columns: new[] { "ClientCompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientTasks_ClientCompanyId_Status",
                table: "ClientTasks",
                columns: new[] { "ClientCompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientReports_ClientCompanyId_VersionNumber",
                table: "ClientReports",
                columns: new[] { "ClientCompanyId", "VersionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientDocuments_ClientCompanyId_UploadedAt",
                table: "ClientDocuments",
                columns: new[] { "ClientCompanyId", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientActivityLogs_ClientCompanyId_CreatedAt",
                table: "ClientActivityLogs",
                columns: new[] { "ClientCompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponses_ReadinessAssessmentId_ReceivedAt",
                table: "AssessmentResponses",
                columns: new[] { "ReadinessAssessmentId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AIAnalysisOutputs_ClientCompanyId_AnalysisType_VersionNumber",
                table: "AIAnalysisOutputs",
                columns: new[] { "ClientCompanyId", "AnalysisType", "VersionNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeetingTranscripts_ClientCompanyId_SessionDate",
                table: "MeetingTranscripts");

            migrationBuilder.DropIndex(
                name: "IX_GapAnalysisItems_ClientCompanyId_Status",
                table: "GapAnalysisItems");

            migrationBuilder.DropIndex(
                name: "IX_ConsultantNotes_ClientCompanyId_CreatedAt",
                table: "ConsultantNotes");

            migrationBuilder.DropIndex(
                name: "IX_ClientTasks_ClientCompanyId_Status",
                table: "ClientTasks");

            migrationBuilder.DropIndex(
                name: "IX_ClientReports_ClientCompanyId_VersionNumber",
                table: "ClientReports");

            migrationBuilder.DropIndex(
                name: "IX_ClientDocuments_ClientCompanyId_UploadedAt",
                table: "ClientDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ClientActivityLogs_ClientCompanyId_CreatedAt",
                table: "ClientActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResponses_ReadinessAssessmentId_ReceivedAt",
                table: "AssessmentResponses");

            migrationBuilder.DropIndex(
                name: "IX_AIAnalysisOutputs_ClientCompanyId_AnalysisType_VersionNumber",
                table: "AIAnalysisOutputs");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTranscripts_ClientCompanyId",
                table: "MeetingTranscripts",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_GapAnalysisItems_ClientCompanyId",
                table: "GapAnalysisItems",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantNotes_ClientCompanyId",
                table: "ConsultantNotes",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTasks_ClientCompanyId",
                table: "ClientTasks",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientReports_ClientCompanyId",
                table: "ClientReports",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientDocuments_ClientCompanyId",
                table: "ClientDocuments",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientActivityLogs_ClientCompanyId",
                table: "ClientActivityLogs",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AIAnalysisOutputs_ClientCompanyId",
                table: "AIAnalysisOutputs",
                column: "ClientCompanyId");
        }
    }
}
