using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI_Readiness_Hub.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ClientReports_ClientCompanyId_ReportStatus",
                table: "ClientReports",
                columns: new[] { "ClientCompanyId", "ReportStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientCompanies_CurrentStage",
                table: "ClientCompanies",
                column: "CurrentStage");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCompanies_LastModifiedAt",
                table: "ClientCompanies",
                column: "LastModifiedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponses_ExternalResponseId",
                table: "AssessmentResponses",
                column: "ExternalResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponses_ReceivedAt",
                table: "AssessmentResponses",
                column: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClientReports_ClientCompanyId_ReportStatus",
                table: "ClientReports");

            migrationBuilder.DropIndex(
                name: "IX_ClientCompanies_CurrentStage",
                table: "ClientCompanies");

            migrationBuilder.DropIndex(
                name: "IX_ClientCompanies_LastModifiedAt",
                table: "ClientCompanies");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResponses_ExternalResponseId",
                table: "AssessmentResponses");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResponses_ReceivedAt",
                table: "AssessmentResponses");
        }
    }
}
