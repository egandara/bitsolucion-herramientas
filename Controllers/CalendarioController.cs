using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CalendarioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CalendarioController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ObtenerTareasGlobales()
        {
            try
            {
                // NOTA SOBRE ESCALA:
                // Esta vista es "panorámica/global", por eso carga todas las tareas en
                // una sola pasada: el front necesita el universo completo para poblar
                // los filtros (usuario/proyecto), calcular la sobrecarga por día y armar
                // el resumen del rango. Si en el futuro el volumen crece mucho, la mejora
                // es exponer un modo paginado por rango (recibiendo ?start=&end= que envía
                // FullCalendar) PERO manteniendo este endpoint para los filtros globales.
                var tareas = await _context.TareasProyecto
                    .Include(t => t.SubFase)
                        .ThenInclude(s => s.Fase)
                            .ThenInclude(f => f.Proyecto)
                    .Include(t => t.UsuarioAsignado)
                    .AsNoTracking()
                    .ToListAsync();

                var listaEventos = new List<object>();

                // 1. EVENTOS DE TAREAS (para la grilla de horas y días)
                foreach (var t in tareas)
                {
                    string email = t.UsuarioAsignado?.Email ?? "Sin_Asignar";
                    string iniciales = email.Length >= 2 ? email.Substring(0, 2).ToUpper() : "??";
                    string nombreTarea = string.IsNullOrWhiteSpace(t.Nombre) ? "Sin Título" : t.Nombre;

                    DateTime start = t.FechaInicioReal ?? t.FechaCreacion;
                    DateTime end;

                    if (t.Estado == "Terminada" && t.FechaFinReal.HasValue)
                    {
                        // FIX VISUAL: si la tarea se terminó en menos de 30 minutos reales,
                        // le damos un alto mínimo de 30 mins en el calendario para que se
                        // lea el texto.
                        var duracionReal = (t.FechaFinReal.Value - start).TotalMinutes;
                        end = duracionReal < 30 ? start.AddMinutes(30) : t.FechaFinReal.Value;
                    }
                    else
                    {
                        // Si está pendiente o en progreso, usa el tiempo estimado.
                        decimal horas = t.HorasEstimadas > 0 ? t.HorasEstimadas : 1m;
                        end = start.AddHours((double)horas);
                    }

                    listaEventos.Add(new
                    {
                        id = "T_" + t.Id,
                        title = nombreTarea,
                        start = start.ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = end.ToString("yyyy-MM-ddTHH:mm:ss"),
                        allDay = false,
                        editable = true, // las tareas SÍ se pueden arrastrar/redimensionar
                        classNames = new[] { "evt-tarea" },
                        backgroundColor = t.Estado == "Terminada" ? "#198754" :
                                          t.Estado == "En Progreso" ? "#ffc107" : "#0dcaf0",
                        borderColor = t.Estado == "Terminada" ? "#198754" :
                                      t.Estado == "En Progreso" ? "#ffc107" : "#0dcaf0",
                        textColor = t.Estado == "En Progreso" ? "#000" : "#fff",
                        extendedProps = new
                        {
                            tipo = "tarea",
                            iniciales = iniciales,
                            email = email,
                            proyecto = t.SubFase?.Fase?.Proyecto?.Nombre ?? "Desconocido",
                            estado = t.Estado,
                            horasEstimadas = t.HorasEstimadas,
                            horasReales = t.HorasRealesDeducidas
                        }
                    });
                }

                // 2. EVENTOS DE RESUMEN DIARIO (franja "Todo el día" de Semana/Día)
                var resumenes = tareas
                    .Where(t => t.UsuarioAsignado != null)
                    .GroupBy(t => new
                    {
                        Email = t.UsuarioAsignado.Email,
                        Fecha = (t.FechaInicioReal ?? t.FechaCreacion).Date
                    })
                    .Select(g => new
                    {
                        Email = g.Key.Email,
                        Fecha = g.Key.Fecha,
                        TotalEstimado = g.Sum(x => x.HorasEstimadas),
                        TotalReal = g.Sum(x => x.HorasRealesDeducidas)
                    }).ToList();

                foreach (var r in resumenes)
                {
                    string iniciales = (r.Email != null && r.Email.Length >= 2)
                        ? r.Email.Substring(0, 2).ToUpper() : "??";

                    listaEventos.Add(new
                    {
                        id = $"R_{iniciales}_{r.Fecha:yyyyMMdd}",
                        title = $"Resumen {iniciales}",
                        start = r.Fecha.ToString("yyyy-MM-dd"),
                        allDay = true,
                        editable = false, // los resúmenes NO se arrastran
                        classNames = new[] { "evt-resumen" },
                        backgroundColor = "#111424",
                        borderColor = "#6c757d",
                        textColor = "#fff",
                        extendedProps = new
                        {
                            tipo = "resumen",
                            email = r.Email,
                            iniciales = iniciales,
                            totalEstimado = r.TotalEstimado,
                            totalReal = r.TotalReal
                        }
                    });
                }

                return Json(listaEventos);
            }
            catch (Exception ex)
            {
                // Devolvemos un 500 real para que el JS muestre el aviso de error
                // y ofrezca "Reintentar" en vez de fallar en silencio.
                Console.WriteLine($"Error en CalendarioController: {ex.Message}");
                return StatusCode(500, new { error = "No se pudieron cargar las tareas del calendario." });
            }
        }

        // ===== Reprogramar una tarea (drag & drop / resize desde el calendario) =====
        // Recibe el id y las nuevas fechas. Mantiene la duración al mover; al
        // redimensionar ajusta FechaFinReal (si está terminada) u HorasEstimadas
        // (si está pendiente/en progreso).
        public class ReprogramarRequest
        {
            public int Id { get; set; }
            public DateTime Start { get; set; }
            public DateTime? End { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReprogramarTarea([FromBody] ReprogramarRequest req)
        {
            if (req == null || req.Id <= 0)
                return BadRequest(new { error = "Solicitud inválida." });

            var tarea = await _context.TareasProyecto.FindAsync(req.Id);
            if (tarea == null)
                return NotFound(new { error = "La tarea no existe o fue eliminada." });

            tarea.FechaInicioReal = req.Start;

            if (req.End.HasValue)
            {
                if (tarea.Estado == "Terminada")
                {
                    tarea.FechaFinReal = req.End.Value;
                }
                else
                {
                    var horas = (decimal)(req.End.Value - req.Start).TotalHours;
                    if (horas > 0)
                        tarea.HorasEstimadas = Math.Round(horas, 2);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al reprogramar tarea {req.Id}: {ex.Message}");
                return StatusCode(500, new { error = "No se pudo guardar el cambio." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuplicarTarea([FromBody] ReprogramarRequest req)
        {
            if (req == null || req.Id <= 0)
                return BadRequest(new { error = "Solicitud inválida." });

            // Consultamos la tarea de forma desadjuntada (AsNoTracking) para usar la misma instancia como clon
            var tareaClon = await _context.TareasProyecto
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == req.Id);

            if (tareaClon == null)
                return NotFound(new { error = "La tarea original no existe o fue eliminada." });

            // Guardamos el punto de inicio previo para calcular la duración exacta del clon
            DateTime inicioOriginal = tareaClon.FechaInicioReal ?? tareaClon.FechaCreacion;

            // Restablecemos los parámetros necesarios para que EF Core entienda que es un registro nuevo
            tareaClon.Id = 0;
            tareaClon.FechaCreacion = DateTime.Now;
            tareaClon.FechaInicioReal = req.Start;

            // Si estaba terminada y tiene fecha de fin, le asignamos una nueva proporcional a la original
            if (tareaClon.Estado == "Terminada" && tareaClon.FechaFinReal.HasValue)
            {
                var duracionOriginal = tareaClon.FechaFinReal.Value - inicioOriginal;
                tareaClon.FechaFinReal = req.Start.Add(duracionOriginal);
            }

            try
            {
                await _context.TareasProyecto.AddAsync(tareaClon);
                await _context.SaveChangesAsync();
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al duplicar la tarea {req.Id}: {ex.Message}");
                return StatusCode(500, new { error = "No se pudo procesar la duplicación de la tarea." });
            }
        }

        // Clase para recibir la petición de eliminar
        public class EliminarTareaRequest
        {
            public int Id { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarTarea([FromBody] EliminarTareaRequest req)
        {
            if (req.Id <= 0) return BadRequest(new { error = "Solicitud inválida." });

            var tarea = await _context.TareasProyecto.FindAsync(req.Id);
            if (tarea == null) return NotFound(new { error = "La tarea no existe o ya fue eliminada." });

            try
            {
                _context.TareasProyecto.Remove(tarea);
                await _context.SaveChangesAsync();
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar la tarea {req.Id}: {ex.Message}");
                return StatusCode(500, new { error = "No se pudo eliminar la tarea debido a un error del servidor." });
            }
        }
    }
}
