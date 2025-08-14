using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotebookValidator.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisQuotaToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnalysisQuota",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisQuota",
                table: "AspNetUsers");
        }
    }
}
