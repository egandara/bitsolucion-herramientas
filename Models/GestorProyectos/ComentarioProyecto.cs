using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class ComentarioProyecto
    {
        [Key]
        public int Id { get; set; }

        public int ProyectoId { get; set; }
        [ForeignKey(nameof(ProyectoId))]
        public Proyecto Proyecto { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Usuario { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Required]
        public string Texto { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Tipo { get; set; } = "Nota"; // Nota, Advertencia, Recordatorio, Documento

        public DateTime? FechaVencimiento { get; set; }
        public bool Resuelto { get; set; } = false;

        // ── Documento adjunto ─────────────────────────────────────
        [MaxLength(100)]
        public string? Subcategoria { get; set; }   // "Análisis Técnico", "NUAD", etc.

        [MaxLength(255)]
        public string? ArchivoNombre { get; set; }  // nombre original del archivo

        [MaxLength(500)]
        public string? ArchivoUrl { get; set; }     // URL en Google Drive

        // ── Menciones (@usuario) ──────────────────────────────────
        [MaxLength(500)]
        public string? Menciones { get; set; }      // JSON array de usernames mencionados
        public int? SubFaseProyectoId { get; set; }
        [ForeignKey(nameof(SubFaseProyectoId))]
        public SubFaseProyecto? SubFase { get; set; }
    }

    // Mapa estático: subcategoría → (carpeta padre en Drive, subcarpeta, icono Bootstrap)
    public static class DocumentoSubcategorias
    {
        public static readonly Dictionary<string, (string CarpetaPadre, string SubCarpeta, string Icono)> Mapa = new()
        {
            ["Análisis Técnico"] = ("1_Diseño", "Analisis_Tecnico", "bi-file-earmark-text"),
            ["Carta Gantt"] = ("1_Diseño", "Carta_Gantt", "bi-calendar-range"),
            ["Aprobación QA"] = ("3_Pruebas", "Aprobaciones", "bi-patch-check"),
            ["Matriz de Pruebas"] = ("3_Pruebas", "Matriz_Pruebas", "bi-table"),
            ["NUAD"] = ("4_Entregables", "NUAD", "bi-diagram-3"),
            ["RAT"] = ("4_Entregables", "RAT", "bi-shield-check"),
            ["Manual de Operación"] = ("4_Entregables", "Manuales", "bi-book"),
            ["Otro"] = ("4_Entregables", "Otros", "bi-paperclip"),
        };
    }
}
