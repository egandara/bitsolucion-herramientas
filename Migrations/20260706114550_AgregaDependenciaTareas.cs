using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotebookValidator.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregaDependenciaTareas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TareaPredecesoraId",
                table: "TareasProyecto",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TareasProyecto_TareaPredecesoraId",
                table: "TareasProyecto",
                column: "TareaPredecesoraId");

            migrationBuilder.AddForeignKey(
                name: "FK_TareasProyecto_TareasProyecto_TareaPredecesoraId",
                table: "TareasProyecto",
                column: "TareaPredecesoraId",
                principalTable: "TareasProyecto",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TareasProyecto_TareasProyecto_TareaPredecesoraId",
                table: "TareasProyecto");

            migrationBuilder.DropIndex(
                name: "IX_TareasProyecto_TareaPredecesoraId",
                table: "TareasProyecto");

            migrationBuilder.DropColumn(
                name: "TareaPredecesoraId",
                table: "TareasProyecto");
        }
    }
}
