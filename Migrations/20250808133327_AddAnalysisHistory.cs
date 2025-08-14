using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotebookValidator.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AnalysisTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    TotalFilesAnalyzed = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalProblemsFound = table.Column<int>(type: "INTEGER", nullable: false),
                    ResultsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisRuns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRuns_UserId",
                table: "AnalysisRuns",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisRuns");
        }
    }
}
