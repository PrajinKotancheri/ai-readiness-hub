using System;
using AI_Readiness_Hub.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI_Readiness_Hub.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260603160000_AddReadinessFormIntegration")]
    public partial class AddReadinessFormIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientToken",
                table: "ReadinessAssessments",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomFormUrl",
                table: "ReadinessAssessments",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalResponseId",
                table: "ReadinessAssessments",
                type: "TEXT",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneratedFormUrl",
                table: "ReadinessAssessments",
                type: "TEXT",
                maxLength: 1200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSentAt",
                table: "ReadinessAssessments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponseReceivedAt",
                table: "ReadinessAssessments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SentToEmail",
                table: "ReadinessAssessments",
                type: "TEXT",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReadinessFormSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DefaultFormUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ClientReferenceEntryId = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    EmailSubjectTemplate = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    EmailBodyTemplate = table.Column<string>(type: "TEXT", nullable: false),
                    WebhookSecret = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessFormSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessAssessments_ClientToken",
                table: "ReadinessAssessments",
                column: "ClientToken");

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessAssessments_ExternalResponseId",
                table: "ReadinessAssessments",
                column: "ExternalResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessFormSettings_IsActive",
                table: "ReadinessFormSettings",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReadinessFormSettings");

            migrationBuilder.DropIndex(
                name: "IX_ReadinessAssessments_ClientToken",
                table: "ReadinessAssessments");

            migrationBuilder.DropIndex(
                name: "IX_ReadinessAssessments_ExternalResponseId",
                table: "ReadinessAssessments");

            migrationBuilder.DropColumn(
                name: "ClientToken",
                table: "ReadinessAssessments");

            migrationBuilder.DropColumn(
                name: "CustomFormUrl",
                table: "ReadinessAssessments");

            migrationBuilder.DropColumn(
                name: "ExternalResponseId",
                table: "ReadinessAssessments");

            migrationBuilder.DropColumn(
                name: "GeneratedFormUrl",
                table: "ReadinessAssessments");

            migrationBuilder.DropColumn(
                name: "LastReminderSentAt",
                table: "ReadinessAssessments");

            migrationBuilder.DropColumn(
                name: "ResponseReceivedAt",
                table: "ReadinessAssessments");

            migrationBuilder.DropColumn(
                name: "SentToEmail",
                table: "ReadinessAssessments");
        }
    }
}
