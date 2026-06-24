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
                var tareas = await _context.TareasProyecto
                    .Include(t => t.SubFase)
                        .ThenInclude(s => s.Fase)
                            .ThenInclude(f => f.Proyecto)
                    .Include(t => t.UsuarioAsignado)
                    .ToListAsync();

                var subfases = await _context.SubFasesProyecto
                    .Include(s => s.Fase)
                        .ThenInclude(f => f.Proyecto)
                    .Include(s => s.Responsable)
                    .Where(s => s.FechaInicio.HasValue)
                    .ToListAsync();

                // === DETECTOR Y MOTOR DE EFECTO DOMINÓ (SÁBADOS Y DOMINGOS) ===
                var tareasPorUsuario = tareas
                    .Where(t => t.UsuarioAsignado != null)
                    .GroupBy(t => t.UsuarioAsignadoId);

                foreach (var grupo in tareasPorUsuario)
                {
                    // Ordenamos las tareas cronológicamente para aplicar el empuje secuencial
                    var listaTareas = grupo.OrderBy(t => t.FechaInicioReal ?? t.FechaCreacion).ToList();
                    DateTime proximaDisponibilidad = DateTime.MinValue;

                    foreach (var t in listaTareas)
                    {
                        DateTime inicioProyectado = t.FechaInicioReal ?? t.FechaCreacion;

                        // 1. Si la tarea inicia originalmente un fin de semana, saltar al lunes a las 09:00 AM
                        if (inicioProyectado.DayOfWeek == DayOfWeek.Saturday)
                            inicioProyectado = inicioProyectado.AddDays(2).Date.AddHours(9);
                        else if (inicioProyectado.DayOfWeek == DayOfWeek.Sunday)
                            inicioProyectado = inicioProyectado.AddDays(1).Date.AddHours(9);

                        // 2. EFECTO DOMINÓ: Si se solapa con el fin de la tarea anterior, la empujamos
                        if (inicioProyectado < proximaDisponibilidad)
                        {
                            inicioProyectado = proximaDisponibilidad;
                        }

                        // 3. Re-verificar que tras el empuje no haya quedado atrapada en un fin de semana
                        while (inicioProyectado.DayOfWeek == DayOfWeek.Saturday || inicioProyectado.DayOfWeek == DayOfWeek.Sunday)
                        {
                            if (inicioProyectado.DayOfWeek == DayOfWeek.Saturday)
                                inicioProyectado = inicioProyectado.AddDays(2).Date.AddHours(9);
                            else if (inicioProyectado.DayOfWeek == DayOfWeek.Sunday)
                                inicioProyectado = inicioProyectado.AddDays(1).Date.AddHours(9);
                        }

                        // Asignamos las nuevas fechas corregidas al objeto
                        t.FechaInicioReal = inicioProyectado;

                        decimal horas = t.HorasEstimadas > 0 ? t.HorasEstimadas : 1m;
                        DateTime finProyectado = inicioProyectado.AddHours((double)horas);

                        // Si el término toca el fin de semana, lo movemos al lunes
                        if (finProyectado.DayOfWeek == DayOfWeek.Saturday)
                            finProyectado = finProyectado.AddDays(2);
                        else if (finProyectado.DayOfWeek == DayOfWeek.Sunday)
                            finProyectado = finProyectado.AddDays(1);

                        if (t.Estado == "Terminada" && t.FechaFinReal.HasValue)
                            t.FechaFinReal = finProyectado;

                        // La próxima tarea de este usuario no puede empezar antes de que termine esta
                        proximaDisponibilidad = finProyectado;
                    }
                }

                // Ajustar también tareas huérfanas fuera de fin de semana
                foreach (var t in tareas.Where(t => t.UsuarioAsignado == null))
                {
                    DateTime start = t.FechaInicioReal ?? t.FechaCreacion;
                    if (start.DayOfWeek == DayOfWeek.Saturday) start = start.AddDays(2).Date.AddHours(9);
                    if (start.DayOfWeek == DayOfWeek.Sunday) start = start.AddDays(1).Date.AddHours(9);
                    t.FechaInicioReal = start;
                }

                // Guardar auto-correcciones en la base de datos de forma transparente
                await _context.SaveChangesAsync();

                var listaEventos = new List<object>();

                // --- 1. EVENTOS DE FASE (Background) ---
                var fasesAgrupadas = subfases.GroupBy(s => s.Fase);
                foreach (var g in fasesAgrupadas)
                {
                    var fase = g.Key;
                    if (fase == null) continue;

                    var minStart = g.Min(s => s.FechaInicio);
                    var maxEnd = g.Max(s => s.FechaFinEstimada ?? s.FechaInicio);

                    listaEventos.Add(new
                    {
                        id = "F_" + fase.Id,
                        title = "Fase: " + fase.NombreFase.Replace("_", " "),
                        start = minStart?.ToString("yyyy-MM-dd"),
                        end = maxEnd?.AddDays(1).ToString("yyyy-MM-dd"),
                        display = "background",
                        backgroundColor = "rgba(13, 202, 240, 0.04)",
                        extendedProps = new { tipo = "fase", proyecto = fase.Proyecto?.Nombre }
                    });
                }

                // --- 2. EVENTOS DE SUBFASE ---
                foreach (var sub in subfases)
                {
                    string email = sub.Responsable?.Email ?? "Sin_Asignar";
                    string iniciales = email.Length >= 2 ? email.Substring(0, 2).ToUpper() : "??";
                    string startStr = sub.FechaInicio?.ToString("yyyy-MM-ddTHH:mm:ss");
                    string endStr = sub.FechaFinEstimada?.ToString("yyyy-MM-ddTHH:mm:ss");

                    listaEventos.Add(new
                    {
                        id = "S_" + sub.Id,
                        title = sub.Nombre,
                        start = startStr,
                        end = endStr,
                        allDay = false,
                        editable = false,
                        backgroundColor = "rgba(255, 193, 7, 0.12)",
                        borderColor = "transparent",
                        textColor = "#ffc107",
                        classNames = new[] { "evt-subfase" },
                        extendedProps = new
                        {
                            tipo = "subfase",
                            proyecto = sub.Fase?.Proyecto?.Nombre ?? "Desconocido",
                            iniciales = iniciales,
                            email = email,
                            estado = sub.Estado,
                            subfaseInicio = startStr,
                            subfaseFin = endStr,
                            fase = sub.Fase?.NombreFase?.Replace("_", " ") ?? "—",
                            subfase = sub.Nombre ?? "—"
                        }
                    });
                }

                // --- 3. EVENTOS DE TAREAS ---
                foreach (var t in tareas)
                {
                    string email = t.UsuarioAsignado?.Email ?? "";
                    string iniciales = email.Length >= 2 ? email.Substring(0, 2).ToUpper() : "??";
                    string nombreTarea = string.IsNullOrWhiteSpace(t.Nombre) ? "Sin Título" : t.Nombre;

                    DateTime start = t.FechaInicioReal ?? new DateTime(t.FechaCreacion.Year, t.FechaCreacion.Month, t.FechaCreacion.Day, 9, 0, 0);
                    DateTime end;

                    if (t.Estado == "Terminada" && t.FechaFinReal.HasValue)
                    {
                        end = t.FechaFinReal.Value;
                    }
                    else
                    {
                        decimal horas = t.HorasEstimadas > 0 ? t.HorasEstimadas : 1m;
                        end = start.AddHours((double)horas);
                    }

                    bool fueraDeRango = false;
                    DateTime? sfInicio = t.SubFase?.FechaInicio;
                    DateTime? sfFin = t.SubFase?.FechaFinEstimada;

                    if (sfInicio.HasValue && sfFin.HasValue)
                    {
                        if (start < sfInicio.Value || end > sfFin.Value) fueraDeRango = true;
                    }

                    string claseEstado = "evt-pendiente";
                    if (t.Estado == "En Progreso") claseEstado = "evt-progreso";
                    if (t.Estado == "Terminada") claseEstado = "evt-terminada";

                    var listaClases = new List<string> { "evt-tarea", claseEstado };
                    if (fueraDeRango) listaClases.Add("tarea-fuera-rango");

                    listaEventos.Add(new
                    {
                        id = "T_" + t.Id,
                        title = nombreTarea,
                        start = start.ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = end.ToString("yyyy-MM-ddTHH:mm:ss"),
                        allDay = false,
                        editable = true,
                        classNames = listaClases.ToArray(),
                        backgroundColor = "transparent",
                        borderColor = "transparent",
                        textColor = "#fff",
                        extendedProps = new
                        {
                            tipo = "tarea",
                            iniciales = iniciales,
                            email = email,
                            proyecto = t.SubFase?.Fase?.Proyecto?.Nombre ?? "Desconocido",
                            estado = t.Estado,
                            horasEstimadas = t.HorasEstimadas,
                            horasReales = t.HorasRealesDeducidas,
                            subfaseInicio = sfInicio?.ToString("yyyy-MM-ddTHH:mm:ss"),
                            subfaseFin = sfFin?.ToString("yyyy-MM-ddTHH:mm:ss"),
                            fueraDeRango = fueraDeRango,
                            fase = t.SubFase?.Fase?.NombreFase?.Replace("_", " ") ?? "—",
                            subfase = t.SubFase?.Nombre ?? "—"
                        }
                    });
                }

                // --- 4. RESÚMENES DIARIOS ---
                var resumenes = tareas
                    .Where(t => t.UsuarioAsignado != null)
                    .GroupBy(t => new { Email = t.UsuarioAsignado.Email, Fecha = (t.FechaInicioReal ?? t.FechaCreacion).Date })
                    .Select(g => new
                    {
                        Email = g.Key.Email,
                        Fecha = g.Key.Fecha,
                        TotalEstimado = g.Sum(x => x.HorasEstimadas),
                        TotalReal = g.Sum(x => x.HorasRealesDeducidas)
                    }).ToList();

                foreach (var r in resumenes)
                {
                    string iniciales = (r.Email != null && r.Email.Length >= 2) ? r.Email.Substring(0, 2).ToUpper() : "??";
                    listaEventos.Add(new
                    {
                        id = $"R_{iniciales}_{r.Fecha:yyyyMMdd}",
                        title = $"Resumen {iniciales}",
                        start = r.Fecha.ToString("yyyy-MM-dd"),
                        allDay = true,
                        editable = false,
                        classNames = new[] { "evt-resumen" },
                        backgroundColor = "#111424",
                        borderColor = "#6c757d",
                        textColor = "#fff",
                        extendedProps = new { tipo = "resumen", email = r.Email, iniciales = iniciales, totalEstimado = r.TotalEstimado, totalReal = r.TotalReal }
                    });
                }

                return Json(listaEventos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en CalendarioController: {ex.Message}");
                return StatusCode(500, new { error = "No se pudieron cargar las tareas." });
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

        // Clase para recibir el Payload del modal
        public class ActualizarTareaRequest
        {
            public int Id { get; set; }
            public string EmailResponsable { get; set; }
            public string Estado { get; set; }
            public DateTime Inicio { get; set; }
            public DateTime? Fin { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarTareaDetalle([FromBody] ActualizarTareaRequest req)
        {
            if (req.Id <= 0) return BadRequest(new { error = "Solicitud inválida." });

            var tarea = await _context.TareasProyecto
                .Include(t => t.UsuarioAsignado)
                .FirstOrDefaultAsync(t => t.Id == req.Id);

            if (tarea == null) return NotFound(new { error = "La tarea no existe." });

            // Actualizar datos básicos
            tarea.Estado = req.Estado;
            tarea.FechaInicioReal = req.Inicio;

            // Lógica de cálculo de finalización y horas
            if (req.Fin.HasValue)
            {
                if (req.Estado == "Terminada")
                {
                    tarea.FechaFinReal = req.Fin.Value;
                }

                var horas = (decimal)(req.Fin.Value - req.Inicio).TotalHours;
                if (horas > 0)
                    tarea.HorasEstimadas = Math.Round(horas, 2);
            }

            // Actualizar el responsable
            if (!string.IsNullOrEmpty(req.EmailResponsable) && (tarea.UsuarioAsignado == null || tarea.UsuarioAsignado.Email != req.EmailResponsable))
            {
                var user = await _userManager.FindByEmailAsync(req.EmailResponsable);
                if (user != null)
                    tarea.UsuarioAsignadoId = user.Id;
            }
            else if (string.IsNullOrEmpty(req.EmailResponsable))
            {
                tarea.UsuarioAsignadoId = null;
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar tarea detallada {req.Id}: {ex.Message}");
                return StatusCode(500, new { error = "No se pudo actualizar la tarea en la base de datos." });
            }
        }

        // Clase para recibir el Payload de creación
        public class CrearTareaRequest
        {
            public int SubFaseId { get; set; }
            public string Nombre { get; set; }
            public string EmailResponsable { get; set; }
            public decimal HorasEstimadas { get; set; }
            public DateTime Inicio { get; set; }
            public DateTime Fin { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearTareaGlobal([FromBody] CrearTareaRequest req)
        {
            if (req.SubFaseId <= 0 || string.IsNullOrWhiteSpace(req.Nombre))
                return BadRequest(new { error = "Datos inválidos." });

            var subfase = await _context.SubFasesProyecto.FindAsync(req.SubFaseId);
            if (subfase == null) return NotFound(new { error = "Subfase no encontrada." });

            // Buscar el ID del usuario en base a su email
            string userId = null;
            if (!string.IsNullOrEmpty(req.EmailResponsable))
            {
                var user = await _userManager.FindByEmailAsync(req.EmailResponsable);
                if (user != null) userId = user.Id;
            }

            var nuevaTarea = new NotebookValidator.Web.Models.GestorProyectos.TareaProyecto
            {
                SubFaseProyectoId = req.SubFaseId,
                Nombre = req.Nombre.Trim(),
                HorasEstimadas = req.HorasEstimadas,
                UsuarioAsignadoId = userId,
                Estado = "Pendiente",
                FechaCreacion = DateTime.Now,
                FechaInicioReal = req.Inicio,
                FechaFinReal = req.Fin
            };

            try
            {
                _context.TareasProyecto.Add(nuevaTarea);
                await _context.SaveChangesAsync();
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear tarea global: {ex.Message}");
                return StatusCode(500, new { error = "No se pudo guardar la tarea." });
            }
        }
    }
}
