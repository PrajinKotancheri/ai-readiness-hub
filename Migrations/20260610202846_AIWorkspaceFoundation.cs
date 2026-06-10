using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AI_Readiness_Hub.Migrations
{
    /// <inheritdoc />
    public partial class AIWorkspaceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIWorkspaceSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "integer", nullable: false),
                    OutputType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OutputId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIWorkspaceSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIWorkspaceSessions_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIOutputRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientCompanyId = table.Column<int>(type: "integer", nullable: false),
                    OutputType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OutputId = table.Column<int>(type: "integer", nullable: true),
                    AIWorkspaceSessionId = table.Column<int>(type: "integer", nullable: true),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    DraftContent = table.Column<string>(type: "text", nullable: false),
                    ConsultantFeedback = table.Column<string>(type: "text", nullable: true),
                    Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIOutputRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIOutputRevisions_AIWorkspaceSessions_AIWorkspaceSessionId",
                        column: x => x.AIWorkspaceSessionId,
                        principalTable: "AIWorkspaceSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AIOutputRevisions_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIWorkspaceMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AIWorkspaceSessionId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MessageText = table.Column<string>(type: "text", nullable: false),
                    DraftContentSnapshot = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIWorkspaceMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIWorkspaceMessages_AIWorkspaceSessions_AIWorkspaceSessionId",
                        column: x => x.AIWorkspaceSessionId,
                        principalTable: "AIWorkspaceSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIOutputRevisions_AIWorkspaceSessionId",
                table: "AIOutputRevisions",
                column: "AIWorkspaceSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIOutputRevisions_ClientCompanyId_OutputType_OutputId_Versi~",
                table: "AIOutputRevisions",
                columns: new[] { "ClientCompanyId", "OutputType", "OutputId", "VersionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_AIWorkspaceMessages_AIWorkspaceSessionId_CreatedAt",
                table: "AIWorkspaceMessages",
                columns: new[] { "AIWorkspaceSessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AIWorkspaceSessions_ClientCompanyId_OutputType_OutputId_Sta~",
                table: "AIWorkspaceSessions",
                columns: new[] { "ClientCompanyId", "OutputType", "OutputId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIOutputRevisions");

            migrationBuilder.DropTable(
                name: "AIWorkspaceMessages");

            migrationBuilder.DropTable(
                name: "AIWorkspaceSessions");
        }
    }
}
