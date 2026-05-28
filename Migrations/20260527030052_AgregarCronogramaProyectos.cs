using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotebookValidator.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCronogramaProyectos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFinEstimada",
                table: "Proyectos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicio",
                table: "Proyectos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPasoProduccion",
                table: "Proyectos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notas",
                table: "Proyectos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaFinEstimada",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "FechaInicio",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "FechaPasoProduccion",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "Notas",
                table: "Proyectos");
        }
    }
}
