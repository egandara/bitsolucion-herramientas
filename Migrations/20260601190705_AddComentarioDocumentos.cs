using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotebookValidator.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddComentarioDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchivoNombre",
                table: "ComentariosProyecto",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivoUrl",
                table: "ComentariosProyecto",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Menciones",
                table: "ComentariosProyecto",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subcategoria",
                table: "ComentariosProyecto",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivoNombre",
                table: "ComentariosProyecto");

            migrationBuilder.DropColumn(
                name: "ArchivoUrl",
                table: "ComentariosProyecto");

            migrationBuilder.DropColumn(
                name: "Menciones",
                table: "ComentariosProyecto");

            migrationBuilder.DropColumn(
                name: "Subcategoria",
                table: "ComentariosProyecto");
        }
    }
}
