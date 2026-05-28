using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class Proyecto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Aquí guardaremos el ID real de la carpeta raíz de Google Drive
        [MaxLength(255)]
        public string? DriveFolderId { get; set; }

        // Aquí guardaremos la URL para poner un botón de "Abrir en Drive"
        [MaxLength(1000)]
        public string? DriveFolderUrl { get; set; }
        // =========================================================
        // CONTROL DE WORKSPACE Y MÁQUINA DE ESTADOS (DIRTY FLAG)
        // =========================================================

        [MaxLength(500)]
        public string? RutaWorkspaceLocal { get; set; } // Ruta donde se guarda el .zip en el Windows Server

        public DateTime? FechaActualizacionWorkspace { get; set; } // Cuándo se subió el último código

        [MaxLength(50)]
        public string EstadoValidacionWorkspace { get; set; } = "Sin_Codigo"; // Estados: Sin_Codigo, Pendiente_Validacion, Validado
        [MaxLength(50)]
        public string EstadoSincronizacionDrive { get; set; } = "Pendiente"; // Estados: Pendiente, Sincronizando, Sincronizado, Error

        public DateTime? FechaSincronizacionDrive { get; set; } // Cuándo terminó de subir a la nube

        [MaxLength(50)]
        public string Estado { get; set; } = "Activo"; // Activo, Pausado, Finalizado

        // =========================================================
        // NUEVOS CAMPOS: CRONOGRAMA Y NOTAS
        // =========================================================
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinEstimada { get; set; }
        public DateTime? FechaPasoProduccion { get; set; }

        // =========================================================
        // REGLAS DE TOLERANCIA DE CALIDAD (QA)
        // =========================================================
        // Los errores Críticos siempre deben ser 0.
        public int MaxWarningsPermitidos { get; set; } = 5; // Por defecto perdona 5 warnings
        public int MaxInfosPermitidos { get; set; } = 10;  // Por defecto perdona 10 infos

        public string? Notas { get; set; }

        // NUEVO: Relación con el Cliente (Empresa)
        public int? ClienteId { get; set; }
        [ForeignKey(nameof(ClienteId))]
        public Cliente? Cliente { get; set; }

        // Relaciones
        public ICollection<FaseProyecto> Fases { get; set; } = new List<FaseProyecto>();
        public ICollection<ProyectoUsuario> UsuariosAsignados { get; set; } = new List<ProyectoUsuario>();
        // Relación: Un proyecto puede tener muchas validaciones de notebooks históricas
        public ICollection<NotebookValidacion> Validaciones { get; set; } = new List<NotebookValidacion>();

        // =========================================================
        // NUEVO: PROPIEDAD CALCULADA DE PROGRESO (EN MEMORIA)
        // =========================================================
        [NotMapped] // Indica a Entity Framework que no intente crear esta columna en la BD
        public int PorcentajeProgreso
        {
            get
            {
                if (Fases == null || !Fases.Any()) return 0;

                int totalFases = Fases.Count;
                int completadas = Fases.Count(f => f.EstadoFase == "Completado");

                // Regla de tres simple: (Completadas / Total) * 100
                double calculo = ((double)completadas / totalFases) * 100;

                return (int)Math.Round(calculo);
            }
        }

        // =========================================================
        // NUEVO: INTELIGENCIA DE TIEMPO (SEMAFORIZACIÓN)
        // =========================================================
        [NotMapped]
        public string EstadoRiesgo
        {
            get
            {
                if (!FechaFinEstimada.HasValue) return "Sin Fecha";
                if (Estado == "Finalizado" || PorcentajeProgreso == 100) return "Completado";

                var diasRestantes = (FechaFinEstimada.Value.Date - DateTime.Now.Date).Days;

                if (diasRestantes < 0) return "Atrasado"; // Venció
                if (diasRestantes <= 5) return "En Riesgo"; // Quedan 5 días o menos
                return "A Tiempo"; // Todo normal
            }
        }

        [NotMapped]
        public string MensajeTiempo
        {
            get
            {
                if (!FechaFinEstimada.HasValue) return "No definida";
                if (Estado == "Finalizado" || PorcentajeProgreso == 100) return "Entregado";

                var diasRestantes = (FechaFinEstimada.Value.Date - DateTime.Now.Date).Days;

                if (diasRestantes < 0) return $"Atrasado por {Math.Abs(diasRestantes)} días";
                if (diasRestantes == 0) return "¡Vence hoy!";
                return $"Faltan {diasRestantes} días";
            }
        }

        // =========================================================
        // METADATOS CORPORATIVOS
        // =========================================================
        [MaxLength(255)]
        public string? RepositorioGitHub { get; set; }

        [MaxLength(100)]
        public string? ContraparteCliente { get; set; }

        // Relación: Un proyecto tiene un catálogo de tablas (Origen y Salida)
        public ICollection<TablaProyecto> TablasCatalogo { get; set; } = new List<TablaProyecto>();
        // Historial de Jobs/Artefactos generados
        public ICollection<ArtefactoJob> ArtefactosGenerados { get; set; } = new List<ArtefactoJob>();
        // Muro de Comentarios y Recordatorios
        public ICollection<ComentarioProyecto> Comentarios { get; set; } = new List<ComentarioProyecto>();

        // =========================================================
        // ÍNDICE DE BÚSQUEDA OMNI-SEARCH
        // =========================================================
        public string? ArchivosIndexados { get; set; } // Guarda los nombres de los notebooks internos para el buscador global
    }
}
