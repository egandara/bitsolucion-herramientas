using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotebookValidator.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregaHorasSubfases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HorasEstimadas",
                table: "SubFasesProyecto",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HorasReales",
                table: "SubFasesProyecto",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HorasEstimadas",
                table: "SubFasesProyecto");

            migrationBuilder.DropColumn(
                name: "HorasReales",
                table: "SubFasesProyecto");
        }
    }
}
