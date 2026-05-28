using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotebookValidator.Web.Migrations
{
    /// <inheritdoc />
    public partial class EvolucionCatalogoMaestro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM TablasProyecto;");
            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "TablasProyecto");

            migrationBuilder.DropColumn(
                name: "NombreTabla",
                table: "TablasProyecto");

            migrationBuilder.AddColumn<string>(
                name: "RutaUbicacion",
                table: "TablasProyecto",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TablaMaestraId",
                table: "TablasProyecto",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TablasMaestras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    NombreTabla = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetadataColumnasJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TablasMaestras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TablasMaestras_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TablasProyecto_TablaMaestraId",
                table: "TablasProyecto",
                column: "TablaMaestraId");

            migrationBuilder.CreateIndex(
                name: "IX_TablasMaestras_ClienteId",
                table: "TablasMaestras",
                column: "ClienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_TablasProyecto_TablasMaestras_TablaMaestraId",
                table: "TablasProyecto",
                column: "TablaMaestraId",
                principalTable: "TablasMaestras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TablasProyecto_TablasMaestras_TablaMaestraId",
                table: "TablasProyecto");

            migrationBuilder.DropTable(
                name: "TablasMaestras");

            migrationBuilder.DropIndex(
                name: "IX_TablasProyecto_TablaMaestraId",
                table: "TablasProyecto");

            migrationBuilder.DropColumn(
                name: "RutaUbicacion",
                table: "TablasProyecto");

            migrationBuilder.DropColumn(
                name: "TablaMaestraId",
                table: "TablasProyecto");

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "TablasProyecto",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreTabla",
                table: "TablasProyecto",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}
