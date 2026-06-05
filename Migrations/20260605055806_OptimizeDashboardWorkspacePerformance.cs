using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI_Readiness_Hub.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeDashboardWorkspacePerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReadinessScores_ClientCompanyId",
                table: "ReadinessScores");

            migrationBuilder.DropIndex(
                name: "IX_ReadinessAssessments_ClientCompanyId",
                table: "ReadinessAssessments");

            migrationBuilder.DropIndex(
                name: "IX_ClientWorkflowSteps_ClientCompanyId",
                table: "ClientWorkflowSteps");

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessScores_ClientCompanyId_CreatedAt",
                table: "ReadinessScores",
                columns: new[] { "ClientCompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessAssessments_ClientCompanyId_CreatedAt",
                table: "ReadinessAssessments",
                columns: new[] { "ClientCompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessAssessments_FormStatus_SentAt",
                table: "ReadinessAssessments",
                columns: new[] { "FormStatus", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientWorkflowSteps_ClientCompanyId_DisplayOrder",
                table: "ClientWorkflowSteps",
                columns: new[] { "ClientCompanyId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientTasks_Status_DueDate",
                table: "ClientTasks",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientReports_ReportStatus_GeneratedAt",
                table: "ClientReports",
                columns: new[] { "ReportStatus", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponses_Status_ReceivedAt",
                table: "AssessmentResponses",
                columns: new[] { "Status", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReadinessScores_ClientCompanyId_CreatedAt",
                table: "ReadinessScores");

            migrationBuilder.DropIndex(
                name: "IX_ReadinessAssessments_ClientCompanyId_CreatedAt",
                table: "ReadinessAssessments");

            migrationBuilder.DropIndex(
                name: "IX_ReadinessAssessments_FormStatus_SentAt",
                table: "ReadinessAssessments");

            migrationBuilder.DropIndex(
                name: "IX_ClientWorkflowSteps_ClientCompanyId_DisplayOrder",
                table: "ClientWorkflowSteps");

            migrationBuilder.DropIndex(
                name: "IX_ClientTasks_Status_DueDate",
                table: "ClientTasks");

            migrationBuilder.DropIndex(
                name: "IX_ClientReports_ReportStatus_GeneratedAt",
                table: "ClientReports");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResponses_Status_ReceivedAt",
                table: "AssessmentResponses");

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessScores_ClientCompanyId",
                table: "ReadinessScores",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessAssessments_ClientCompanyId",
                table: "ReadinessAssessments",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientWorkflowSteps_ClientCompanyId",
                table: "ClientWorkflowSteps",
                column: "ClientCompanyId");
        }
    }
}
