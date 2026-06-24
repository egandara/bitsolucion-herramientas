using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotebookValidator.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddControlRecursos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HorasSemanalesContratadas",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TareasProyecto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubFaseProyectoId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UsuarioAsignadoId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaInicioPlanificada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaFinPlanificada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HorasEstimadas = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FechaInicioReal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaFinReal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HorasRealesDeducidas = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TareasProyecto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TareasProyecto_AspNetUsers_UsuarioAsignadoId",
                        column: x => x.UsuarioAsignadoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TareasProyecto_SubFasesProyecto_SubFaseProyectoId",
                        column: x => x.SubFaseProyectoId,
                        principalTable: "SubFasesProyecto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TareasProyecto_SubFaseProyectoId",
                table: "TareasProyecto",
                column: "SubFaseProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_TareasProyecto_UsuarioAsignadoId",
                table: "TareasProyecto",
                column: "UsuarioAsignadoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TareasProyecto");

            migrationBuilder.DropColumn(
                name: "HorasSemanalesContratadas",
                table: "AspNetUsers");
        }
    }
}
