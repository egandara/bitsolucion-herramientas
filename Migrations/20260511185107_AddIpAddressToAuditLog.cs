using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotebookValidator.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIpAddressToAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ActionType",
                table: "AuditLogs",
                newName: "IpAddress");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Action",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "IpAddress",
                table: "AuditLogs",
                newName: "ActionType");
        }
    }
}
