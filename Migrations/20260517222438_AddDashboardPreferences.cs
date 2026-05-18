using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotebookValidator.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDashboardPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LayoutJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDashboardPreferences", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDashboardPreferences");
        }
    }
}
