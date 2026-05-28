using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotebookValidator.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregarWorkspaceProyecto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstadoValidacionWorkspace",
                table: "Proyectos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaActualizacionWorkspace",
                table: "Proyectos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RutaWorkspaceLocal",
                table: "Proyectos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoValidacionWorkspace",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "FechaActualizacionWorkspace",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "RutaWorkspaceLocal",
                table: "Proyectos");
        }
    }
}
