using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotebookValidator.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSubFases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubFaseProyectoId",
                table: "ComentariosProyecto",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubFasesProyecto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FaseProyectoId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResponsableId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DriveSubFolderId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubFasesProyecto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubFasesProyecto_AspNetUsers_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubFasesProyecto_FasesProyecto_FaseProyectoId",
                        column: x => x.FaseProyectoId,
                        principalTable: "FasesProyecto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosProyecto_SubFaseProyectoId",
                table: "ComentariosProyecto",
                column: "SubFaseProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_SubFasesProyecto_FaseProyectoId",
                table: "SubFasesProyecto",
                column: "FaseProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_SubFasesProyecto_ResponsableId",
                table: "SubFasesProyecto",
                column: "ResponsableId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComentariosProyecto_SubFasesProyecto_SubFaseProyectoId",
                table: "ComentariosProyecto",
                column: "SubFaseProyectoId",
                principalTable: "SubFasesProyecto",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComentariosProyecto_SubFasesProyecto_SubFaseProyectoId",
                table: "ComentariosProyecto");

            migrationBuilder.DropTable(
                name: "SubFasesProyecto");

            migrationBuilder.DropIndex(
                name: "IX_ComentariosProyecto_SubFaseProyectoId",
                table: "ComentariosProyecto");

            migrationBuilder.DropColumn(
                name: "SubFaseProyectoId",
                table: "ComentariosProyecto");
        }
    }
}
