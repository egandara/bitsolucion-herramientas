using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.Models.GestorProyectos;
using NotebookValidator.Web.Services;
using NotebookValidator.Web.Services.GestorProyectos;
using NotebookValidator.Web.ViewModels.GestorProyectos;
using System.Security.Claims;
using System.Text.Json;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class ProyectosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleDriveService _driveService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotebookValidatorService _validatorService;
        private readonly WorkspaceService _workspaceService;
        private readonly LineageService _lineageService;
        private readonly JobGenerationService _jobGenerationService;
        private readonly ProyectosSearchService _searchService;
        private readonly AuditService _auditService;
        private readonly NotificacionesService _notifService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public ProyectosController(
            ApplicationDbContext context,
            GoogleDriveService driveService,
            UserManager<ApplicationUser> userManager,
            NotebookValidatorService validatorService,
            WorkspaceService workspaceService,
            LineageService lineageService,
            JobGenerationService jobGenerationService,
            ProyectosSearchService searchService,
            AuditService auditService,
            NotificacionesService notifService,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _context = context;
            _driveService = driveService;
            _userManager = userManager;
            _validatorService = validatorService;
            _workspaceService = workspaceService;
            _lineageService = lineageService;
            _jobGenerationService = jobGenerationService;
            _searchService = searchService;
            _auditService = auditService;
            _notifService = notifService;
            _env = env;
        }

        // ==========================================
        // CRUD PROYECTOS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index(string? filtro)
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            bool isAdmin = User.IsInRole("Admin");
            string shortUserName = (User.Identity?.Name ?? "").Split('@')[0];

            var query = _context.Proyectos
                .Include(p => p.Cliente)
                .Include(p => p.Fases)
                    .ThenInclude(f => f.SubFases)
                .Include(p => p.Comentarios)
                .Include(p => p.UsuariosAsignados).ThenInclude(ua => ua.Usuario)
                .OrderByDescending(p => p.FechaCreacion)
                .AsNoTracking()
                .AsSplitQuery();

            if (!isAdmin)
            {
                query = query.Where(p => p.UsuariosAsignados.Any(ua => ua.UsuarioId == currentUserId));
            }

            var proyectos = await query.ToListAsync();

            if (!string.IsNullOrEmpty(filtro))
            {
                proyectos = filtro.ToLower() switch
                {
                    "activos" => proyectos.Where(p => p.Estado == "Activo").ToList(),
                    "atiempo" => proyectos.Where(p => p.EstadoRiesgo == "A Tiempo" || p.EstadoRiesgo == "Completado").ToList(),
                    "enriesgo" => proyectos.Where(p => p.EstadoRiesgo == "En Riesgo").ToList(),
                    "atrasados" => proyectos.Where(p => p.EstadoRiesgo == "Atrasado").ToList(),
                    "qa_rechazados" => proyectos.Where(p => p.EstadoValidacionWorkspace == "Rechazado").ToList(),
                    "qa_pendientes" => proyectos.Where(p => p.EstadoValidacionWorkspace == "Pendiente_Validacion").ToList(),
                    "alertas" => proyectos.Where(p => p.Comentarios != null && p.Comentarios.Any(c =>
                                    !c.Resuelto &&
                                    (c.Tipo == "Advertencia" || c.Tipo == "Recordatorio") &&
                                    c.FechaVencimiento < DateTime.Now)).ToList(),
                    "menciones" => proyectos.Where(p => p.Comentarios != null && p.Comentarios.Any(c =>
                                    !c.Resuelto &&
                                    c.Menciones != null && c.Menciones.Contains(shortUserName))).ToList(),
                    _ => proyectos
                };
            }

            return View(proyectos);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Cargar usuarios para el combo de responsables
            var usuarios = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            ViewBag.UsuariosBanco = usuarios;

            // Cargar clientes para el combo principal
            var clientes = await _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync();
            ViewBag.Clientes = new SelectList(clientes, "Id", "Nombre");

            // EL PUENTE: Mandamos los horarios al Javascript en formato JSON
            var horarios = clientes.Select(c => new {
                id = c.Id.ToString(),
                entradaH = c.HoraEntrada.Hours,
                entradaM = c.HoraEntrada.Minutes,
                salidaNormalH = c.HoraSalidaNormal.Hours,
                salidaNormalM = c.HoraSalidaNormal.Minutes,
                salidaViernesH = c.HoraSalidaViernes.Hours,
                salidaViernesM = c.HoraSalidaViernes.Minutes
            }).ToList();

            ViewBag.ClientesHorariosJson = System.Text.Json.JsonSerializer.Serialize(horarios);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var proyecto = await _context.Proyectos.FindAsync(id);
                if (proyecto == null)
                {
                    return Json(new { success = false, message = "El proyecto no existe o ya fue eliminado." });
                }

                // Al eliminar el proyecto, la base de datos aplicará el borrado en cascada
                // a las fases, usuarios asignados y bitácoras gracias a tu ApplicationDbContext.
                _context.Proyectos.Remove(proyecto);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Proyecto eliminado de forma permanente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al eliminar el proyecto: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string nombre, string descripcion, int? clienteId, string? repositorioGitHub, string? contraparteCliente,
                    DateTime? fechaInicio, DateTime? fechaFinEstimada, DateTime? fechaPasoProduccion, string notas,
                    List<string> fasesSeleccionadas, List<string> usuariosAsignadosIds,
                    List<SubfaseInputDto> subfases)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return View();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var nuevoProyecto = new Proyecto
                {
                    Nombre = nombre.Trim(),
                    Descripcion = descripcion?.Trim() ?? string.Empty,
                    ClienteId = clienteId,
                    RepositorioGitHub = repositorioGitHub?.Trim(),
                    ContraparteCliente = contraparteCliente?.Trim(),
                    FechaInicio = fechaInicio,
                    FechaFinEstimada = fechaFinEstimada,
                    FechaPasoProduccion = fechaPasoProduccion,
                    Notas = notas?.Trim() ?? string.Empty,
                    FechaCreacion = DateTime.Now,
                    Estado = "Activo"
                };
                _context.Proyectos.Add(nuevoProyecto);
                await _context.SaveChangesAsync();

                string nombreCliente = "Sin_Cliente";
                if (clienteId.HasValue)
                {
                    var c = await _context.Clientes.FindAsync(clienteId.Value);
                    if (c != null) nombreCliente = c.Nombre;
                }

                // --- 1. SE CREA LA ESTRUCTURA EN DRIVE ---
                var driveResult = await _driveService.CreateProjectStructureAsync(
                    $"PRJ_{nuevoProyecto.Id:D3}_{nuevoProyecto.Nombre.Replace(" ", "_")}", nombreCliente);

                nuevoProyecto.DriveFolderId = driveResult.RootFolderId;
                nuevoProyecto.DriveFolderUrl = driveResult.RootFolderUrl;
                _context.Entry(nuevoProyecto).State = EntityState.Modified;

                // ====================================================================
                // NUEVO: SUBIR LA PRESENTACIÓN PPTX EN BLANCO A LA RAÍZ DEL PROYECTO
                // ====================================================================
                string localTemplatePath = System.IO.Path.Combine(_env.WebRootPath, "templates", "Planificacion_Template.pptx");
                string dateStr = DateTime.Now.ToString("yyyyMMdd");
                string driveFileName = $"Planificacion_{nuevoProyecto.Nombre}_{dateStr}.pptx";
                string pptxContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";

                // Ejecuta la subida del archivo usando el FolderId que devolvió tu método CreateProjectStructureAsync
                await _driveService.UploadFileToFolderAsync(localTemplatePath, driveFileName, nuevoProyecto.DriveFolderId, pptxContentType);
                // ====================================================================

                var fasesCreadas = new Dictionary<string, FaseProyecto>();
                int orden = 1;
                foreach (var f in new[] { "1_Diseño_Arquitectura", "2_Desarrollo_Notebooks", "3_Pruebas_Certificacion", "4_Paso_A_Produccion" })
                {
                    var nuevaFase = new FaseProyecto { ProyectoId = nuevoProyecto.Id, NombreFase = f, EstadoFase = "Pendiente", Orden = orden++ };
                    _context.FasesProyecto.Add(nuevaFase);
                    fasesCreadas[f] = nuevaFase;
                }

                if (fasesSeleccionadas != null)
                {
                    foreach (var f in fasesSeleccionadas)
                    {
                        var nuevaFase = new FaseProyecto { ProyectoId = nuevoProyecto.Id, NombreFase = f, EstadoFase = "Pendiente", Orden = orden++ };
                        _context.FasesProyecto.Add(nuevaFase);
                        fasesCreadas[f] = nuevaFase;
                    }
                }

                await _context.SaveChangesAsync();

                if (subfases != null && subfases.Any())
                {
                    foreach (var sub in subfases)
                    {
                        if (!string.IsNullOrWhiteSpace(sub.Nombre) && fasesCreadas.TryGetValue(sub.FasePadre, out var fasePadre))
                        {
                            var nuevaSubfase = new SubFaseProyecto
                            {
                                FaseProyectoId = fasePadre.Id,
                                Nombre = sub.Nombre,
                                Estado = "Pendiente",
                                ResponsableId = string.IsNullOrWhiteSpace(sub.ResponsableId) ? null : sub.ResponsableId,
                                FechaInicio = sub.FechaInicio,
                                FechaFinEstimada = sub.FechaFinEstimada
                            };
                            _context.SubFasesProyecto.Add(nuevaSubfase);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                string creadorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                _context.ProyectosUsuarios.Add(new ProyectoUsuario { ProyectoId = nuevoProyecto.Id, UsuarioId = creadorId, RolEnProyecto = "Admin", FechaAsignacion = DateTime.Now });

                var adminUser = await _userManager.FindByIdAsync(creadorId);
                if (adminUser?.Email != null)
                    await _driveService.ShareFolderWithUserAsync(nuevoProyecto.DriveFolderId, adminUser.Email, "writer");

                // --- NUEVA LÓGICA DE AUTO-ASIGNACIÓN ---
                if (usuariosAsignadosIds == null) usuariosAsignadosIds = new List<string>();

                // Si hay responsables en las subfases, los extraemos y los sumamos a la lista global
                if (subfases != null && subfases.Any())
                {
                    var responsablesExtras = subfases
                        .Where(s => !string.IsNullOrWhiteSpace(s.ResponsableId))
                        .Select(s => s.ResponsableId!)
                        .ToList();

                    usuariosAsignadosIds.AddRange(responsablesExtras);
                }

                // Eliminamos duplicados por si el usuario lo marcó abajo y también le dio una tarea
                usuariosAsignadosIds = usuariosAsignadosIds.Distinct().ToList();

                if (usuariosAsignadosIds != null)
                {
                    foreach (var uId in usuariosAsignadosIds.Where(id => id != creadorId))
                    {
                        _context.ProyectosUsuarios.Add(new ProyectoUsuario { ProyectoId = nuevoProyecto.Id, UsuarioId = uId, RolEnProyecto = "Developer", FechaAsignacion = DateTime.Now });
                        var devUser = await _userManager.FindByIdAsync(uId);
                        if (devUser?.Email != null)
                            await _driveService.ShareFolderWithUserAsync(nuevoProyecto.DriveFolderId, devUser.Email, "writer");
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogActionAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                    "GESTOR: PROYECTO CREADO",
                    JsonSerializer.Serialize(new { Proyecto = nuevoProyecto.Nombre, Cliente = nombreCliente, Drive = nuevoProyecto.DriveFolderUrl }),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    nuevoProyecto.Id.ToString());

                return RedirectToAction(nameof(Index));
            }
            catch { await transaction.RollbackAsync(); return View(); }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.Cliente)
                .Include(p => p.Fases.OrderBy(f => f.Orden))
                    .ThenInclude(f => f.SubFases)
                        .ThenInclude(s => s.Responsable)
                .Include(p => p.Fases.OrderBy(f => f.Orden))
                    .ThenInclude(f => f.SubFases)
                        .ThenInclude(s => s.Tareas) // <-- ESTO ES LO NUEVO
                            .ThenInclude(t => t.UsuarioAsignado) // <-- ESTO ES LO NUEVO
                .Include(p => p.UsuariosAsignados).ThenInclude(ua => ua.Usuario)
                .Include(p => p.Validaciones.OrderByDescending(v => v.FechaValidacion))
                .Include(p => p.TablasCatalogo).ThenInclude(tc => tc.TablaMaestra)
                .Include(p => p.Comentarios.OrderByDescending(c => c.FechaCreacion))
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto == null) return NotFound();

            int totalArchivos = 0;
            var conteoExtensiones = new Dictionary<string, int>();
            var archivosAnalizables = new List<string>();

            if (!string.IsNullOrEmpty(proyecto.RutaWorkspaceLocal) && System.IO.File.Exists(proyecto.RutaWorkspaceLocal))
            {
                try
                {
                    using var stream = new FileStream(proyecto.RutaWorkspaceLocal, FileMode.Open, FileAccess.Read);
                    using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);

                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith("/") || entry.FullName.Contains("__MACOSX")) continue;
                        totalArchivos++;
                        string ext = Path.GetExtension(entry.Name).ToLower();
                        if (string.IsNullOrEmpty(ext)) ext = "otros";
                        conteoExtensiones[ext] = conteoExtensiones.GetValueOrDefault(ext) + 1;
                        if (ext is ".ipynb" or ".py" or ".sql" or ".scala")
                            archivosAnalizables.Add(entry.Name);
                    }
                }
                catch { }
            }

            ViewBag.TotalArchivos = totalArchivos;
            ViewBag.ConteoExtensiones = conteoExtensiones;
            ViewBag.ArchivosAnalizables = archivosAnalizables;

            var eventosCalendario = new List<object>();

            if (proyecto.FechaInicio.HasValue)
                eventosCalendario.Add(new { fecha = proyecto.FechaInicio.Value.ToString("yyyy-MM-dd"), tipo = "inicio", etiqueta = "Inicio del proyecto", color = "#198754" });

            if (proyecto.FechaPasoProduccion.HasValue)
                eventosCalendario.Add(new { fecha = proyecto.FechaPasoProduccion.Value.ToString("yyyy-MM-dd"), tipo = "produccion", etiqueta = "Paso a Producción", color = "#dc3545" });

            if (proyecto.FechaFinEstimada.HasValue)
                eventosCalendario.Add(new { fecha = proyecto.FechaFinEstimada.Value.ToString("yyyy-MM-dd"), tipo = "cierre", etiqueta = "Cierre estimado", color = "#dc3545" });

            foreach (var fase in proyecto.Fases.Where(f => f.FechaActualizacion.HasValue))
            {
                eventosCalendario.Add(new { fecha = fase.FechaActualizacion!.Value.ToString("yyyy-MM-dd"), tipo = "fase", etiqueta = $"Fase {fase.Orden}: {fase.NombreFase.Replace("_", " ")} → {fase.EstadoFase}", color = "#0dcaf0" });
            }

            foreach (var val in proyecto.Validaciones)
            {
                eventosCalendario.Add(new { fecha = val.FechaValidacion.ToString("yyyy-MM-dd"), tipo = "validacion", etiqueta = $"QA {(val.PasoValidacion ? "Aprobado" : "Rechazado")} — Score: {val.Score}%", color = val.PasoValidacion ? "#6f42c1" : "#dc3545" });
            }

            foreach (var c in proyecto.Comentarios.Where(c => c.Tipo == "Recordatorio" && c.Resuelto && c.FechaVencimiento.HasValue))
            {
                eventosCalendario.Add(new { fecha = c.FechaVencimiento!.Value.ToString("yyyy-MM-dd"), tipo = "alerta", etiqueta = $"✓ Alerta resuelta: {(c.Texto.Length > 40 ? c.Texto.Substring(0, 40) + "..." : c.Texto)}", color = "#198754" });
            }

            foreach (var c in proyecto.Comentarios.Where(c => c.Tipo == "Recordatorio" && !c.Resuelto && c.FechaVencimiento.HasValue && c.FechaVencimiento.Value.Date >= DateTime.Now.Date))
            {
                eventosCalendario.Add(new { fecha = c.FechaVencimiento!.Value.ToString("yyyy-MM-dd"), tipo = "recordatorio", etiqueta = $"⏰ Recordatorio: {(c.Texto.Length > 40 ? c.Texto.Substring(0, 40) + "..." : c.Texto)}", color = "#fd7e14" });
            }

            foreach (var c in proyecto.Comentarios.Where(c => c.Tipo == "Recordatorio" && !c.Resuelto && c.FechaVencimiento.HasValue && c.FechaVencimiento.Value.Date < DateTime.Now.Date))
            {
                eventosCalendario.Add(new { fecha = c.FechaVencimiento!.Value.ToString("yyyy-MM-dd"), tipo = "vencido", etiqueta = $"⚠ Vencido sin resolver: {(c.Texto.Length > 40 ? c.Texto.Substring(0, 40) + "..." : c.Texto)}", color = "#dc3545" });
            }

            var feedActividad = eventosCalendario
                .Select(e => (dynamic)e)
                .OrderByDescending(e => e.fecha)
                .Take(15)
                .ToList();

            ViewBag.EventosCalendario = System.Text.Json.JsonSerializer.Serialize(eventosCalendario);
            ViewBag.FeedActividad = feedActividad;

            ViewBag.JobsCount = await _context.ArtefactosJob.CountAsync(j => j.ProyectoId == id);

            var mesesConActividad = eventosCalendario
                .Select(e => ((dynamic)e).fecha.ToString().Substring(0, 7))
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            ViewBag.MesesConActividad = System.Text.Json.JsonSerializer.Serialize(mesesConActividad);

            return View(proyecto);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.UsuariosAsignados)
                .Include(p => p.Fases.OrderBy(f => f.Orden))
                    .ThenInclude(f => f.SubFases)
                        .ThenInclude(s => s.Tareas) // <-- ¡VITAL!: Cargamos las tareas de cada subfase
                            .ThenInclude(t => t.UsuarioAsignado) // Cargamos el ejecutor de la tarea
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto == null)
            {
                return NotFound();
            }

            ViewBag.Clientes = new SelectList(await _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", proyecto.ClienteId);
            ViewBag.UsuariosBanco = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.Email).ToListAsync();

            // EL PUENTE: Mandamos los horarios al Javascript en formato JSON para el Motor de Horas
            var clientes = await _context.Clientes.Where(c => c.Activo).ToListAsync();
            var horarios = clientes.Select(c => new {
                id = c.Id.ToString(),
                entradaH = c.HoraEntrada.Hours,
                entradaM = c.HoraEntrada.Minutes,
                salidaNormalH = c.HoraSalidaNormal.Hours,
                salidaNormalM = c.HoraSalidaNormal.Minutes,
                salidaViernesH = c.HoraSalidaViernes.Hours,
                salidaViernesM = c.HoraSalidaViernes.Minutes
            }).ToList();

            ViewBag.ClientesHorariosJson = System.Text.Json.JsonSerializer.Serialize(horarios);

            return View(proyecto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, string descripcion, int? clienteId, string estado, string? repositorioGitHub,
                    string? contraparteCliente, DateTime? fechaInicio, DateTime? fechaFinEstimada, DateTime? fechaPasoProduccion,
                    string notas, int maxWarningsPermitidos, int maxInfosPermitidos, List<string> usuariosAsignadosIds,
                    List<SubfaseInputDto> subfases)
        {
            try
            {
                // 1. Cargamos el proyecto original con TODO su árbol (Fases -> Subfases -> Tareas)
                var proyectoDb = await _context.Proyectos
                    .Include(p => p.UsuariosAsignados)
                    .Include(p => p.Fases)
                        .ThenInclude(f => f.SubFases)
                            .ThenInclude(s => s.Tareas)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (proyectoDb == null) return NotFound();

                // 2. Actualizar Datos Generales
                proyectoDb.Descripcion = descripcion?.Trim() ?? string.Empty;
                proyectoDb.ClienteId = clienteId;
                proyectoDb.Estado = estado;
                proyectoDb.RepositorioGitHub = repositorioGitHub?.Trim();
                proyectoDb.ContraparteCliente = contraparteCliente?.Trim();
                proyectoDb.FechaInicio = fechaInicio;
                proyectoDb.FechaFinEstimada = fechaFinEstimada;
                proyectoDb.FechaPasoProduccion = fechaPasoProduccion;
                proyectoDb.Notas = notas?.Trim() ?? string.Empty;
                proyectoDb.MaxWarningsPermitidos = maxWarningsPermitidos;
                proyectoDb.MaxInfosPermitidos = maxInfosPermitidos;

                if (proyectoDb.Fases == null) proyectoDb.Fases = new List<FaseProyecto>();

                // 3. SINCRONIZADOR BLINDADO DE WBS (SUBFASES Y TAREAS)
                if (subfases != null)
                {
                    var subfasesEntrantesIds = subfases.Where(s => s.Id > 0).Select(s => s.Id).ToList();

                    // A) Eliminar subfases que el usuario borró en la interfaz
                    foreach (var fase in proyectoDb.Fases)
                    {
                        if (fase.SubFases != null)
                        {
                            var subfasesAEliminar = fase.SubFases.Where(s => !subfasesEntrantesIds.Contains(s.Id)).ToList();
                            foreach (var sub in subfasesAEliminar)
                            {
                                // Eliminar comentarios asociados para evitar errores de Foreign Key (si los hay)
                                var comentariosSub = await _context.ComentariosProyecto
                                    .Where(c => c.SubFaseProyectoId == sub.Id)
                                    .ToListAsync();
                                if (comentariosSub.Any())
                                {
                                    _context.ComentariosProyecto.RemoveRange(comentariosSub);
                                }

                                if (sub.Tareas != null && sub.Tareas.Any())
                                {
                                    _context.TareasProyecto.RemoveRange(sub.Tareas);
                                }
                                _context.SubFasesProyecto.Remove(sub);
                            }
                        }
                    }

                    // B) Agregar o Actualizar Subfases y Tareas
                    foreach (var subForm in subfases)
                    {
                        if (string.IsNullOrWhiteSpace(subForm.Nombre)) continue;

                        // Buscar subfase existente
                        var subDb = proyectoDb.Fases
                            .Where(f => f.SubFases != null)
                            .SelectMany(f => f.SubFases)
                            .FirstOrDefault(s => s.Id == subForm.Id && subForm.Id != 0);

                        var faseTarget = proyectoDb.Fases.FirstOrDefault(f => f.NombreFase == subForm.FasePadre) ?? proyectoDb.Fases.First();
                        if (faseTarget.SubFases == null) faseTarget.SubFases = new List<SubFaseProyecto>();

                        if (subDb != null)
                        {
                            // Actualizar propiedades de la Subfase (incluyendo Horas)
                            subDb.Nombre = subForm.Nombre;
                            subDb.ResponsableId = string.IsNullOrWhiteSpace(subForm.ResponsableId) ? null : subForm.ResponsableId;
                            subDb.FechaInicio = subForm.FechaInicio;
                            subDb.FechaFinEstimada = subForm.FechaFinEstimada;
                            subDb.HorasEstimadas = subForm.HorasEstimadas;

                            // Cambiar de fase si es necesario
                            if (subDb.FaseProyectoId != faseTarget.Id)
                            {
                                var oldFase = proyectoDb.Fases.FirstOrDefault(f => f.Id == subDb.FaseProyectoId);
                                if (oldFase != null && oldFase.SubFases != null) oldFase.SubFases.Remove(subDb);
                                faseTarget.SubFases.Add(subDb);
                                subDb.FaseProyectoId = faseTarget.Id;
                            }
                        }
                        else
                        {
                            // Crear nueva Subfase
                            subDb = new SubFaseProyecto
                            {
                                FaseProyectoId = faseTarget.Id,
                                Nombre = subForm.Nombre,
                                Estado = "Pendiente",
                                ResponsableId = string.IsNullOrWhiteSpace(subForm.ResponsableId) ? null : subForm.ResponsableId,
                                FechaInicio = subForm.FechaInicio,
                                FechaFinEstimada = subForm.FechaFinEstimada,
                                HorasEstimadas = subForm.HorasEstimadas,
                                Tareas = new List<TareaProyecto>()
                            };
                            _context.SubFasesProyecto.Add(subDb);
                            faseTarget.SubFases.Add(subDb);
                        }

                        // --- Sincronizar Tareas Hijas ---
                        if (subDb.Tareas == null) subDb.Tareas = new List<TareaProyecto>();

                        if (subForm.Tareas != null)
                        {
                            var tareasEntrantesIds = subForm.Tareas.Where(t => t.Id > 0).Select(t => t.Id).ToList();

                            // Borrar tareas eliminadas en la vista
                            var tareasAEliminar = subDb.Tareas.Where(t => !tareasEntrantesIds.Contains(t.Id) && t.Id != 0).ToList();
                            foreach (var tDel in tareasAEliminar) _context.TareasProyecto.Remove(tDel);

                            // Agregar o actualizar tareas
                            foreach (var tareaForm in subForm.Tareas)
                            {
                                if (string.IsNullOrWhiteSpace(tareaForm.Nombre)) continue;

                                var tareaDb = subDb.Tareas.FirstOrDefault(t => t.Id == tareaForm.Id && tareaForm.Id != 0);
                                if (tareaDb != null)
                                {
                                    tareaDb.Nombre = tareaForm.Nombre;
                                    tareaDb.FechaInicioPlanificada = tareaForm.FechaInicioPlanificada;
                                    tareaDb.FechaFinPlanificada = tareaForm.FechaFinPlanificada;
                                    tareaDb.HorasEstimadas = tareaForm.HorasEstimadas;
                                    tareaDb.UsuarioAsignadoId = string.IsNullOrWhiteSpace(tareaForm.UsuarioAsignadoId) ? null : tareaForm.UsuarioAsignadoId;
                                }
                                else
                                {
                                    var nuevaTarea = new TareaProyecto
                                    {
                                        Nombre = tareaForm.Nombre,
                                        FechaInicioPlanificada = tareaForm.FechaInicioPlanificada,
                                        FechaFinPlanificada = tareaForm.FechaFinPlanificada,
                                        HorasEstimadas = tareaForm.HorasEstimadas,
                                        UsuarioAsignadoId = string.IsNullOrWhiteSpace(tareaForm.UsuarioAsignadoId) ? null : tareaForm.UsuarioAsignadoId,
                                        Estado = "Pendiente",
                                        FechaCreacion = DateTime.Now
                                    };
                                    if (subDb.Id > 0) nuevaTarea.SubFaseProyectoId = subDb.Id;
                                    subDb.Tareas.Add(nuevaTarea);
                                }
                            }
                        }
                        else
                        {
                            // Si borraron todas las tareas de una subfase existente
                            if (subDb.Tareas.Any())
                            {
                                _context.TareasProyecto.RemoveRange(subDb.Tareas);
                                subDb.Tareas.Clear();
                            }
                        }
                    }
                }
                else
                {
                    // Si borraron TODAS las subfases del proyecto
                    foreach (var fase in proyectoDb.Fases)
                    {
                        if (fase.SubFases != null)
                        {
                            foreach (var sub in fase.SubFases.ToList())
                            {
                                var comentariosSub = await _context.ComentariosProyecto
                                    .Where(c => c.SubFaseProyectoId == sub.Id)
                                    .ToListAsync();
                                if (comentariosSub.Any()) _context.ComentariosProyecto.RemoveRange(comentariosSub);

                                if (sub.Tareas != null && sub.Tareas.Any()) _context.TareasProyecto.RemoveRange(sub.Tareas);
                                _context.SubFasesProyecto.Remove(sub);
                            }
                        }
                    }
                }

                // 4. SINCRONIZADOR DE EQUIPO GLOBAL (Asignación Automática a Drive)
                if (usuariosAsignadosIds == null) usuariosAsignadosIds = new List<string>();

                if (subfases != null)
                {
                    usuariosAsignadosIds.AddRange(subfases.Where(s => !string.IsNullOrWhiteSpace(s.ResponsableId)).Select(s => s.ResponsableId!));
                    var respTareas = subfases.Where(s => s.Tareas != null).SelectMany(s => s.Tareas).Where(t => !string.IsNullOrWhiteSpace(t.UsuarioAsignadoId)).Select(t => t.UsuarioAsignadoId!).ToList();
                    usuariosAsignadosIds.AddRange(respTareas);
                }

                usuariosAsignadosIds = usuariosAsignadosIds.Distinct().ToList();

                var adminIds = proyectoDb.UsuariosAsignados.Where(u => u.RolEnProyecto == "Admin").Select(u => u.UsuarioId).ToList();
                var devActuales = proyectoDb.UsuariosAsignados.Where(u => u.RolEnProyecto == "Developer").ToList();

                var devsParaEliminar = devActuales.Where(u => !usuariosAsignadosIds.Contains(u.UsuarioId)).ToList();
                _context.ProyectosUsuarios.RemoveRange(devsParaEliminar);

                foreach (var uId in usuariosAsignadosIds)
                {
                    if (!adminIds.Contains(uId) && !devActuales.Any(u => u.UsuarioId == uId))
                    {
                        _context.ProyectosUsuarios.Add(new ProyectoUsuario { ProyectoId = proyectoDb.Id, UsuarioId = uId, RolEnProyecto = "Developer", FechaAsignacion = DateTime.Now });

                        if (!string.IsNullOrEmpty(proyectoDb.DriveFolderId))
                        {
                            var devUser = await _userManager.FindByIdAsync(uId);
                            if (devUser?.Email != null) await _driveService.ShareFolderWithUserAsync(proyectoDb.DriveFolderId, devUser.Email, "writer");
                        }
                    }
                }

                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                    "GESTOR: PROYECTO EDITADO (WBS)",
                    JsonSerializer.Serialize(new { ProyectoId = proyectoDb.Id, Nombre = proyectoDb.Nombre, Estado = estado }),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    proyectoDb.Id.ToString());

                return RedirectToAction(nameof(Details), new { id = proyectoDb.Id });
            }
            catch (Exception ex)
            {
                var fullError = ex.Message;
                if (ex.InnerException != null) fullError += " | INNER: " + ex.InnerException.Message;
                return StatusCode(500, new { error = fullError, stackTrace = ex.StackTrace, innerStack = ex.InnerException?.StackTrace });
            }
        }

        // ==========================================
        // FASES Y SUBFASES
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFaseStatus(int faseId, string nuevoEstado)
        {
            var fase = await _context.FasesProyecto.FindAsync(faseId);
            if (fase == null) return Json(new { success = false, message = "Fase no encontrada" });

            fase.EstadoFase = nuevoEstado;
            fase.FechaActualizacion = DateTime.Now;
            fase.UsuarioActualizacion = (User.Identity?.Name ?? "").Split('@')[0];

            await _context.SaveChangesAsync();

            var proyectoFase = await _context.Proyectos.FindAsync(fase.ProyectoId);
            if (proyectoFase != null)
            {
                string actualizadorUid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                await _notifService.NotificarFaseCambiadaAsync(
                    proyectoFase, fase.NombreFase, nuevoEstado, actualizadorUid);
            }

            await _auditService.LogActionAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                "GESTOR: FASE ACTUALIZADA",
                JsonSerializer.Serialize(new { FaseId = faseId, NuevoEstado = nuevoEstado, Fase = fase.NombreFase, ProyectoId = fase.ProyectoId }),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                fase.ProyectoId.ToString());

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSubFaseStatus(int subFaseId, string nuevoEstado)
        {
            var subfase = await _context.SubFasesProyecto.FindAsync(subFaseId);
            if (subfase == null) return Json(new { success = false, message = "Subfase no encontrada" });

            subfase.Estado = nuevoEstado;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ==========================================
        // WORKSPACE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadToWorkspace(int proyectoId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No se detectó ningún archivo." });

            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null)
                return Json(new { success = false, message = "Proyecto no encontrado." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string repoName = !string.IsNullOrEmpty(proyecto.RepositorioGitHub)
                    ? proyecto.RepositorioGitHub.Split('/').Last().Replace(".git", "")
                    : "bundle-proyecto";

                string rutaLocal = await _workspaceService.SaveWorkspaceLocalAsync(proyectoId, file, repoName, proyecto.DriveFolderId ?? string.Empty);

                string archivosIndexados = "";
                try
                {
                    byte[] zipBytes = _workspaceService.GetWorkspaceFile(rutaLocal);
                    using var msZip = new MemoryStream(zipBytes);
                    using var archive = new System.IO.Compression.ZipArchive(msZip, System.IO.Compression.ZipArchiveMode.Read);
                    archivosIndexados = string.Join(";", archive.Entries
                        .Where(e => !e.FullName.EndsWith("/") && !e.FullName.Contains("__MACOSX"))
                        .Select(e => e.Name));
                }
                catch { }

                proyecto.ArchivosIndexados = archivosIndexados;
                proyecto.RutaWorkspaceLocal = rutaLocal;
                proyecto.FechaActualizacionWorkspace = DateTime.Now;
                proyecto.EstadoValidacionWorkspace = "Pendiente_Validacion";
                proyecto.EstadoSincronizacionDrive = "Sincronizando";

                var faseDev = await _context.FasesProyecto.FirstOrDefaultAsync(f => f.ProyectoId == proyectoId && f.NombreFase.Contains("Desarrollo_Notebooks"));
                if (faseDev != null)
                {
                    faseDev.EstadoFase = "En Progreso";
                    faseDev.FechaActualizacion = DateTime.Now;
                    faseDev.UsuarioActualizacion = (User.Identity?.Name ?? "").Split('@')[0];
                }

                var faseQA = await _context.FasesProyecto.FirstOrDefaultAsync(f => f.ProyectoId == proyectoId && f.NombreFase.Contains("Pruebas_Certificacion"));
                if (faseQA?.EstadoFase == "Completado")
                {
                    faseQA.EstadoFase = "Pendiente";
                    faseQA.FechaActualizacion = DateTime.Now;
                    faseQA.UsuarioActualizacion = "Sistema (Nuevo Código)";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                string uploaderUid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                await _notifService.NotificarCodigoSubidoAsync(proyecto, uploaderUid);

                await _auditService.LogActionAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                    "GESTOR: WORKSPACE ACTUALIZADO",
                    JsonSerializer.Serialize(new { ProyectoId = proyectoId, Archivo = file.FileName, TamanioKB = file.Length / 1024 }),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    proyectoId.ToString());

                return Json(new { success = true, message = "Código cargado e indexado en el buscador." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error al guardar en Workspace: " + ex.Message });
            }
        }

        // ==========================================
        // VALIDACIÓN QA
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateProjectNotebook(int proyectoId)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null || string.IsNullOrEmpty(proyecto.RutaWorkspaceLocal))
                return Json(new { success = false, message = "El Workspace está vacío. Sube tu código primero." });

            byte[] fileBytes;
            try
            {
                fileBytes = _workspaceService.GetWorkspaceFile(proyecto.RutaWorkspaceLocal);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"No se pudo leer el Workspace: {ex.Message}" });
            }

            if (fileBytes == null || fileBytes.Length == 0)
                return Json(new { success = false, message = "El archivo del Workspace está vacío o no existe." });

            var user = await _userManager.GetUserAsync(User);

            using var zipStream = new MemoryStream(fileBytes);
            using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);

            var fileStreams = new List<(Stream, string)>();
            foreach (var entry in archive.Entries.Where(e =>
                e.Name.EndsWith(".py") ||
                e.Name.EndsWith(".sql") ||
                e.Name.EndsWith(".ipynb") ||
                e.Name.EndsWith(".scala")))
            {
                var ms = new MemoryStream();
                using (var entryStream = entry.Open())
                    await entryStream.CopyToAsync(ms);
                ms.Position = 0;
                fileStreams.Add((ms, entry.Name));
            }

            if (!fileStreams.Any())
                return Json(new { success = false, message = "No se detectaron archivos de código válido (.ipynb, .py, .sql, .scala) en el Workspace." });

            var (hallazgos, _, _) = await _validatorService.ProcessFilesAsync(fileStreams);

            foreach (var (stream, _) in fileStreams)
                stream.Dispose();

            int criticos = hallazgos.Count(h => h.Severity == "Critical");
            int warnings = hallazgos.Count(h => h.Severity == "Warning");
            bool paso = criticos == 0 && warnings <= proyecto.MaxWarningsPermitidos;
            int score = paso ? 100 : Math.Max(0, 100 - criticos * 10 - warnings * 2);

            string nombreArchivo = Path.GetFileName(proyecto.RutaWorkspaceLocal);

            _context.NotebookValidaciones.Add(new NotebookValidacion
            {
                ProyectoId = proyecto.Id,
                NombreArchivo = nombreArchivo,
                FechaValidacion = DateTime.Now,
                Usuario = user?.Email ?? "Usuario Local",
                PasoValidacion = paso,
                Score = score,
                DetalleErrores = System.Text.Json.JsonSerializer.Serialize(hallazgos)
            });

            proyecto.EstadoValidacionWorkspace = paso ? "Validado" : "Rechazado";

            var faseQA = await _context.FasesProyecto
                .FirstOrDefaultAsync(f => f.ProyectoId == proyecto.Id && f.NombreFase.Contains("Pruebas_Certificacion"));
            if (faseQA != null)
            {
                faseQA.EstadoFase = paso ? "Completado" : "En Progreso";
                faseQA.FechaActualizacion = DateTime.Now;
                faseQA.UsuarioActualizacion = user?.Email?.Split('@')[0] ?? "Sistema QA";
            }

            await _context.SaveChangesAsync();

            if (!paso)
            {
                await _notifService.NotificarValidacionRechazadaAsync(
                    proyecto,
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            }


            await _auditService.LogActionAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                paso ? "GESTOR: VALIDACIÓN QA APROBADA" : "GESTOR: VALIDACIÓN QA RECHAZADA",
                JsonSerializer.Serialize(new { ProyectoId = proyecto.Id, Score = score, Criticos = criticos, Warnings = warnings, Archivo = nombreArchivo }),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                proyecto.Id.ToString());

            return Json(new { success = true, paso, score, criticos, warnings });
        }

        [HttpGet]
        public async Task<IActionResult> GetHallazgosValidacion(int id)
        {
            var validacion = await _context.NotebookValidaciones.FindAsync(id);
            if (validacion == null || string.IsNullOrEmpty(validacion.DetalleErrores))
                return Json(new List<object>());

            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var hallazgos = System.Text.Json.JsonSerializer.Deserialize<List<Finding>>(
                    validacion.DetalleErrores, options);

                var camelOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };

                return Content(
                    System.Text.Json.JsonSerializer.Serialize(
                        hallazgos?.Select(h => new {
                            tipoHallazgo = h.FindingType,
                            severidad = h.Severity,
                            lineaCodigo = h.LineNumber,
                            mensajeError = h.Details,
                            reglaViolada = h.FindingType
                        }),
                        camelOptions),
                    "application/json");
            }
            catch
            {
                return Json(new List<object>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteValidacion(int id)
        {
            if (!User.IsInRole("Admin"))
                return Json(new { success = false, message = "Acceso denegado." });

            var validacion = await _context.NotebookValidaciones.FindAsync(id);
            if (validacion != null)
            {
                int proyId = validacion.ProyectoId;
                _context.NotebookValidaciones.Remove(validacion);
                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                    "GESTOR: VALIDACIÓN HISTÓRICA ELIMINADA (ADMIN)",
                    JsonSerializer.Serialize(new { ValidacionId = id, ProyectoId = proyId, Archivo = validacion.NombreArchivo }),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    proyId.ToString());
            }
            return Json(new { success = true, message = "Revisión histórica eliminada." });
        }

        // ==========================================
        // LINAJE — delega a LineageService
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScanLineageFromCode(int proyectoId)
        {
            var (success, message, tablas) = await _lineageService.ScanLineageFromCodeAsync(proyectoId);
            return success
                ? Json(new { success = true, tablas })
                : Json(new { success = false, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveScannedLineage([FromBody] SaveScannedLineageRequest request)
        {
            var (success, message) = await _lineageService.SaveScannedLineageAsync(request);

            if (success)
                await _auditService.LogActionAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                    "GESTOR: LINAJE IMPORTADO DESDE CÓDIGO",
                    JsonSerializer.Serialize(new { ProyectoId = request.ProyectoId, TablasImportadas = request.Tablas?.Count ?? 0 }),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    request.ProyectoId.ToString());

            return Json(new { success, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnalyzeJobMatch(int proyectoId)
        {
            var (success, message, matchDBs, missingDBs, ghostDBs) = await _lineageService.AnalyzeJobMatchAsync(proyectoId);
            return success
                ? Json(new { success = true, matchDBs, missingDBs, ghostDBs })
                : Json(new { success = false, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTablaCatalogo([FromBody] AddTablaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreTabla))
                return Json(new { success = false, message = "El nombre de la tabla es obligatorio." });

            var (success, newId) = await _lineageService.AddTablaCatalogoAsync(request.ProyectoId, request.NombreTabla, request.TipoTabla, request.Descripcion, null);
            return success
                ? Json(new { success = true, id = newId })
                : Json(new { success = false, message = "Proyecto no encontrado." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTablaCatalogo(int id)
        {
            await _lineageService.DeleteTablaCatalogoAsync(id);
            return Json(new { success = true });
        }

        // ==========================================
        // GENERACIÓN DE JOBS — delega a JobGenerationService
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAndUploadJob(int proyectoId, string bundleName, string permLevel, string permUser, bool autocert, IFormFile yamlFile)
        {
            if (yamlFile == null || yamlFile.Length == 0)
                return Json(new { success = false, message = "Falta el archivo YAML base." });

            var (success, message, driveUrl, downloadToken, fileName) = await _jobGenerationService.GenerateAndUploadJobAsync(
                proyectoId, bundleName, permLevel, permUser, autocert,
                yamlFile.OpenReadStream(), yamlFile.FileName);

            if (success)
                await _auditService.LogActionAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                    "GESTOR: ARTEFACTO JOB GENERADO",
                    JsonSerializer.Serialize(new { ProyectoId = proyectoId, Bundle = bundleName, Archivo = fileName, Drive = driveUrl }),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    proyectoId.ToString());

            return success
                ? Json(new { success = true, driveUrl, downloadToken, fileName })
                : Json(new { success = false, message });
        }

        [HttpGet]
        public IActionResult DownloadArtifact(string token, string fileName)
        {
            var (exists, bytes) = _jobGenerationService.GetArtifactForDownload(token);
            return exists ? File(bytes, "application/zip", fileName) : NotFound();
        }

        // ==========================================
        // COMENTARIOS Y BITÁCORA
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComentario([FromBody] NuevoComentarioDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Texto))
                return Json(new { success = false, message = "El texto no puede estar vacío." });

            string usuario = (User.Identity?.Name ?? "Anónimo").Split('@')[0];

            // Extraer menciones del texto (@nombre)
            var menciones = System.Text.RegularExpressions.Regex
                .Matches(request.Texto, @"@([\w\.]+)")
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            var comentario = new ComentarioProyecto
            {
                ProyectoId = request.ProyectoId,
                Usuario = usuario,
                Texto = request.Texto.Trim(),
                Tipo = request.Tipo,
                FechaVencimiento = request.FechaVencimiento,
                FechaCreacion = DateTime.Now,
                Resuelto = false,
                Menciones = menciones.Any()
                    ? System.Text.Json.JsonSerializer.Serialize(menciones)
                    : null
            };

            _context.ComentariosProyecto.Add(comentario);
            await _context.SaveChangesAsync();

            if (menciones.Any())
            {
                var proyectoNotif = await _context.Proyectos.FindAsync(request.ProyectoId);
                if (proyectoNotif != null)
                {
                    string autorUsername = (User.Identity?.Name ?? "").Split('@')[0];
                    foreach (var mencionado in menciones)
                        await _notifService.NotificarMencionAsync(proyectoNotif, mencionado, autorUsername);
                }
            }

            return Json(new
            {
                success = true,
                message = "Comentario añadido con éxito.",
                comentario = new
                {
                    id = comentario.Id,
                    usuario = comentario.Usuario,
                    texto = comentario.Texto,
                    tipo = comentario.Tipo,
                    fechaCreacionStr = comentario.FechaCreacion.ToString("dd/MM HH:mm"),
                    fechaVencimiento = comentario.FechaVencimiento?.ToString("yyyy-MM-dd"),
                    resuelto = false,
                    subcategoria = (string?)null,
                    archivoNombre = (string?)null,
                    archivoUrl = (string?)null,
                    menciones = menciones
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolverAlerta(int id)
        {
            var c = await _context.ComentariosProyecto.FindAsync(id);
            if (c != null) { c.Resuelto = true; await _context.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComentario(int id)
        {
            var c = await _context.ComentariosProyecto.FindAsync(id);
            if (c != null) { _context.ComentariosProyecto.Remove(c); await _context.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirDocumento(
            int proyectoId, string subcategoria, string descripcion, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return Json(new { success = false, message = "No se recibió ningún archivo." });

            if (archivo.Length > 10 * 1024 * 1024)
                return Json(new { success = false, message = "El archivo supera el límite de 10 MB." });

            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null)
                return Json(new { success = false, message = "Proyecto no encontrado." });

            string driveUrl = "";
            try
            {
                if (!string.IsNullOrEmpty(proyecto.DriveFolderId) &&
                    NotebookValidator.Web.Models.GestorProyectos.DocumentoSubcategorias.Mapa.TryGetValue(
                        subcategoria, out var destino))
                {
                    string carpetaPadreId = await _driveService.GetOrCreateFolderAsync(
                        destino.CarpetaPadre, proyecto.DriveFolderId);

                    string subcarpetaId = await _driveService.GetOrCreateFolderAsync(
                        destino.SubCarpeta, carpetaPadreId);

                    byte[] fileBytes;
                    using (var ms = new MemoryStream())
                    {
                        await archivo.CopyToAsync(ms);
                        fileBytes = ms.ToArray();
                    }

                    string mimeType = archivo.ContentType ?? "application/octet-stream";
                    driveUrl = await _driveService.UploadArtifactToFolderAsync(
                        subcarpetaId, archivo.FileName, fileBytes, mimeType);
                }
            }
            catch (Exception ex)
            {
                driveUrl = "";
                Console.WriteLine($"Error subiendo documento a Drive: {ex.Message}");
            }

            string usuario = (User.Identity?.Name ?? "Anónimo").Split('@')[0];
            string textoMuro = !string.IsNullOrWhiteSpace(descripcion)
                ? descripcion.Trim()
                : $"Documento subido: {archivo.FileName}";

            var comentario = new NotebookValidator.Web.Models.GestorProyectos.ComentarioProyecto
            {
                ProyectoId = proyectoId,
                Usuario = usuario,
                Texto = textoMuro,
                Tipo = "Documento",
                FechaCreacion = DateTime.Now,
                Subcategoria = subcategoria,
                ArchivoNombre = archivo.FileName,
                ArchivoUrl = driveUrl,
                Resuelto = false
            };

            _context.ComentariosProyecto.Add(comentario);

            await _auditService.LogActionAsync(
                User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "",
                "GESTOR: DOCUMENTO SUBIDO",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    ProyectoId = proyectoId,
                    Proyecto = proyecto.Nombre,
                    Subcategoria = subcategoria,
                    Archivo = archivo.FileName,
                    Drive = driveUrl
                }),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                proyectoId.ToString());

            await _context.SaveChangesAsync();

            string subiUid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await _notifService.NotificarDocumentoSubidoAsync(proyecto, subcategoria, subiUid);

            NotebookValidator.Web.Models.GestorProyectos.DocumentoSubcategorias.Mapa
                .TryGetValue(subcategoria, out var info);

            return Json(new
            {
                success = true,
                message = string.IsNullOrEmpty(driveUrl)
                    ? "Documento registrado (Drive no disponible)."
                    : "Documento subido y registrado en la bitácora.",
                comentario = new
                {
                    id = comentario.Id,
                    usuario = comentario.Usuario,
                    texto = comentario.Texto,
                    tipo = "Documento",
                    fechaCreacionStr = comentario.FechaCreacion.ToString("dd/MM HH:mm"),
                    fechaVencimiento = (string?)null,
                    resuelto = false,
                    subcategoria = subcategoria,
                    archivoNombre = comentario.ArchivoNombre,
                    archivoUrl = driveUrl,
                    icono = info.Icono ?? "bi-paperclip",
                    menciones = new List<string>()
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LimpiarLinaje(int id)
        {
            var proyecto = await _context.Proyectos.FindAsync(id);
            if (proyecto == null)
                return Json(new { success = false, message = "Proyecto no encontrado." });

            var tablas = await _context.TablasProyecto
                .Where(t => t.ProyectoId == id)
                .ToListAsync();

            int total = tablas.Count;
            if (total == 0)
                return Json(new { success = true, total = 0, message = "El catálogo ya estaba vacío." });

            _context.TablasProyecto.RemoveRange(tablas);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                "GESTOR: LINAJE LIMPIADO (ADMIN)",
                JsonSerializer.Serialize(new { ProyectoId = id, Proyecto = proyecto.Nombre, TablasEliminadas = total }),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                id.ToString());

            return Json(new { success = true, total, message = $"Se eliminaron {total} tablas del catálogo." });
        }

        // ==========================================
        // BÚSQUEDA GLOBAL — delega a ProyectosSearchService
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> GlobalSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(new List<SearchResultDto>());

            string query = q.ToLower();
            var resultados = new List<SearchResultDto>();

            var proyectos = await _context.Proyectos
                .Include(p => p.Cliente)
                .Where(p => p.Nombre.ToLower().Contains(query) ||
                            (p.Cliente != null && p.Cliente.Nombre.ToLower().Contains(query)) ||
                            p.Descripcion.ToLower().Contains(query))
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            foreach (var p in proyectos)
                resultados.Add(new SearchResultDto
                {
                    Categoria = "Proyectos",
                    Titulo = p.Nombre,
                    Descripcion = p.Cliente?.Nombre ?? "Proyecto Interno",
                    Url = Url.Action("Details", "Proyectos", new { id = p.Id }) ?? "#",
                    Icono = "bi-briefcase-fill text-primary"
                });

            var tablas = await _context.TablasProyecto
                .Include(t => t.TablaMaestra)
                .Include(t => t.Proyecto)
                .Where(t => (t.TablaMaestra != null && t.TablaMaestra.NombreTabla.ToLower().Contains(query)) ||
                            (t.TablaMaestra != null && t.TablaMaestra.Descripcion != null && t.TablaMaestra.Descripcion.ToLower().Contains(query)))
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            foreach (var t in tablas)
                resultados.Add(new SearchResultDto
                {
                    Categoria = "Catálogo de Linaje",
                    Titulo = t.TablaMaestra?.NombreTabla ?? "Tabla Desconocida",
                    Descripcion = $"Proyecto: {t.Proyecto?.Nombre ?? "Desconocido"}",
                    Url = (Url.Action("Details", "Proyectos", new { id = t.ProyectoId }) ?? "#") + "#linaje",
                    Icono = "bi-table text-success"
                });

            var codigo = await _context.Proyectos
                .Where(p => p.ArchivosIndexados != null && p.ArchivosIndexados.ToLower().Contains(query))
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            foreach (var p in codigo)
            {
                var archivos = p.ArchivosIndexados?.Split(';') ?? Array.Empty<string>();
                foreach (var f in archivos.Where(f => f.ToLower().Contains(query)).Take(2))
                    resultados.Add(new SearchResultDto
                    {
                        Categoria = "Código y Notebooks",
                        Titulo = f,
                        Descripcion = $"En Workspace de: {p.Nombre ?? "Desconocido"}",
                        Url = (Url.Action("Details", "Proyectos", new { id = p.Id }) ?? "#") + "#calidad",
                        Icono = "bi-file-earmark-code text-info"
                    });
            }

            var comentarios = await _context.ComentariosProyecto
                .Include(c => c.Proyecto)
                .Where(c => c.Texto.ToLower().Contains(query) ||
                            (c.Usuario != null && c.Usuario.ToLower().Contains(query)))
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            foreach (var c in comentarios)
            {
                string txt = string.IsNullOrEmpty(c.Texto) ? "" : c.Texto;
                string usr = string.IsNullOrEmpty(c.Usuario) ? "Desconocido" : c.Usuario;
                resultados.Add(new SearchResultDto
                {
                    Categoria = "Bitácora y Alertas",
                    Titulo = txt.Length > 45 ? txt.Substring(0, 45) + "..." : txt,
                    Descripcion = $"@{usr} en {c.Proyecto?.Nombre ?? "Desconocido"}",
                    Url = Url.Action("Details", "Proyectos", new { id = c.ProyectoId }) ?? "#",
                    Icono = c.Tipo == "Recordatorio" ? "bi-clock-history text-warning"
                                : (c.Tipo == "Advertencia" ? "bi-exclamation-triangle text-danger"
                                : "bi-chat-left-text text-secondary")
                });
            }

            return Json(resultados);
        }

        // ==========================================
        // MÓDULO DE RECURSOS Y TELEMETRÍA DE TAREAS
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddTarea(int subFaseId, string nombre, decimal horasEstimadas, string? usuarioAsignadoId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return Json(new { success = false, message = "Falta el nombre." });

            var tarea = new NotebookValidator.Web.Models.GestorProyectos.TareaProyecto
            {
                SubFaseProyectoId = subFaseId,
                Nombre = nombre.Trim(),
                HorasEstimadas = horasEstimadas,
                UsuarioAsignadoId = string.IsNullOrWhiteSpace(usuarioAsignadoId) ? null : usuarioAsignadoId,
                Estado = "Pendiente",
                FechaCreacion = DateTime.Now,
                FechaInicioReal = fechaInicio,  // <-- NUEVO
                FechaFinReal = fechaFin         // <-- NUEVO
            };

            _context.TareasProyecto.Add(tarea);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Tarea registrada." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTareaStatus(int tareaId, string nuevoEstado)
        {
            var tarea = await _context.TareasProyecto.FindAsync(tareaId);
            if (tarea == null) return Json(new { success = false, message = "Tarea no encontrada." });

            tarea.Estado = nuevoEstado;

            // 🧠 MOTOR EXPERIMENTAL DE TELEMETRÍA 🧠
            if (nuevoEstado == "En Progreso" && !tarea.FechaInicioReal.HasValue)
            {
                // Si la pasa a En Progreso por primera vez, estampa el inicio
                tarea.FechaInicioReal = DateTime.Now;
            }
            else if (nuevoEstado == "Terminada")
            {
                // Al terminar, estampa el fin y calcula las horas automáticamente
                tarea.FechaFinReal = DateTime.Now;

                if (tarea.FechaInicioReal.HasValue)
                {
                    var tiempoTranscurrido = tarea.FechaFinReal.Value - tarea.FechaInicioReal.Value;
                    // Calcula las horas reales (puedes refinar esto a futuro para excluir fines de semana)
                    tarea.HorasRealesDeducidas = (decimal)Math.Round(tiempoTranscurrido.TotalHours, 2);
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

    }

    public class AddTablaRequest
    {
        public int ProyectoId { get; set; }
        public string NombreTabla { get; set; } = string.Empty;
        public string TipoTabla { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    public class SubfaseInputDto
    {
        public int Id { get; set; }
        public string FasePadre { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? ResponsableId { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinEstimada { get; set; }
        public decimal HorasEstimadas { get; set; }

        public List<TareaInputDto>? Tareas { get; set; }
    }

    public class TareaInputDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime? FechaInicioPlanificada { get; set; }
        public DateTime? FechaFinPlanificada { get; set; }
        public decimal HorasEstimadas { get; set; }
        public string? UsuarioAsignadoId { get; set; }
    }


}
