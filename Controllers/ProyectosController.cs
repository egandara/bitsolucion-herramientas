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
            NotificacionesService notifService)
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
        }

        // ==========================================
        // CRUD PROYECTOS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            bool isAdmin = User.IsInRole("Admin");

            var query = _context.Proyectos
                .Include(p => p.Cliente)
                .Include(p => p.Fases)
                .Include(p => p.UsuariosAsignados).ThenInclude(ua => ua.Usuario)
                .OrderByDescending(p => p.FechaCreacion)
                .AsNoTracking()
                .AsSplitQuery();

            var proyectos = isAdmin
                ? await query.ToListAsync()
                : await query.Where(p => p.UsuariosAsignados.Any(ua => ua.UsuarioId == currentUserId)).ToListAsync();

            return View(proyectos);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.UsuariosBanco = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.Email).ToListAsync();
            ViewBag.Clientes = new SelectList(await _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string nombre, string descripcion, int? clienteId, string? repositorioGitHub, string? contraparteCliente,
            DateTime? fechaInicio, DateTime? fechaFinEstimada, DateTime? fechaPasoProduccion, string notas,
            List<string> fasesSeleccionadas, List<string> usuariosAsignadosIds)
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

                var driveResult = await _driveService.CreateProjectStructureAsync(
                    $"PRJ_{nuevoProyecto.Id:D3}_{nuevoProyecto.Nombre.Replace(" ", "_")}", nombreCliente);

                nuevoProyecto.DriveFolderId = driveResult.RootFolderId;
                nuevoProyecto.DriveFolderUrl = driveResult.RootFolderUrl;
                _context.Entry(nuevoProyecto).State = EntityState.Modified;

                // Crear las 4 fases fijas
                int orden = 1;
                foreach (var f in new[] { "1_Diseño_Arquitectura", "2_Desarrollo_Notebooks", "3_Pruebas_Certificacion", "4_Paso_A_Produccion" })
                    _context.FasesProyecto.Add(new FaseProyecto { ProyectoId = nuevoProyecto.Id, NombreFase = f, EstadoFase = "Pendiente", Orden = orden++ });

                if (fasesSeleccionadas != null)
                    foreach (var f in fasesSeleccionadas)
                        _context.FasesProyecto.Add(new FaseProyecto { ProyectoId = nuevoProyecto.Id, NombreFase = f, EstadoFase = "Pendiente", Orden = orden++ });

                // Asignar creador como Admin y compartir Drive
                string creadorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                _context.ProyectosUsuarios.Add(new ProyectoUsuario { ProyectoId = nuevoProyecto.Id, UsuarioId = creadorId, RolEnProyecto = "Admin", FechaAsignacion = DateTime.Now });

                var adminUser = await _userManager.FindByIdAsync(creadorId);
                if (adminUser?.Email != null)
                    await _driveService.ShareFolderWithUserAsync(nuevoProyecto.DriveFolderId, adminUser.Email, "writer");

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

            // ── Eventos del calendario para _TabCronograma ──────────────
            var eventosCalendario = new List<object>();

            // Inicio del proyecto
            if (proyecto.FechaInicio.HasValue)
                eventosCalendario.Add(new
                {
                    fecha = proyecto.FechaInicio.Value.ToString("yyyy-MM-dd"),
                    tipo = "inicio",
                    etiqueta = "Inicio del proyecto",
                    color = "#198754"
                });

            // Paso a producción estimado
            if (proyecto.FechaPasoProduccion.HasValue)
                eventosCalendario.Add(new
                {
                    fecha = proyecto.FechaPasoProduccion.Value.ToString("yyyy-MM-dd"),
                    tipo = "produccion",
                    etiqueta = "Paso a Producción",
                    color = "#dc3545"
                });

            // Cierre estimado
            if (proyecto.FechaFinEstimada.HasValue)
                eventosCalendario.Add(new
                {
                    fecha = proyecto.FechaFinEstimada.Value.ToString("yyyy-MM-dd"),
                    tipo = "cierre",
                    etiqueta = "Cierre estimado",
                    color = "#dc3545"
                });

            // Cambios de fase
            foreach (var fase in proyecto.Fases.Where(f => f.FechaActualizacion.HasValue))
            {
                eventosCalendario.Add(new
                {
                    fecha = fase.FechaActualizacion!.Value.ToString("yyyy-MM-dd"),
                    tipo = "fase",
                    etiqueta = $"Fase {fase.Orden}: {fase.NombreFase.Replace("_", " ")} → {fase.EstadoFase}",
                    color = "#0dcaf0"
                });
            }

            // Validaciones QA
            foreach (var val in proyecto.Validaciones)
            {
                eventosCalendario.Add(new
                {
                    fecha = val.FechaValidacion.ToString("yyyy-MM-dd"),
                    tipo = "validacion",
                    etiqueta = $"QA {(val.PasoValidacion ? "Aprobado" : "Rechazado")} — Score: {val.Score}%",
                    color = val.PasoValidacion ? "#6f42c1" : "#dc3545"
                });
            }

            // Alertas resueltas
            foreach (var c in proyecto.Comentarios.Where(c =>
                c.Tipo == "Recordatorio" && c.Resuelto && c.FechaVencimiento.HasValue))
            {
                eventosCalendario.Add(new
                {
                    fecha = c.FechaVencimiento!.Value.ToString("yyyy-MM-dd"),
                    tipo = "alerta",
                    etiqueta = $"Alerta resuelta: {(c.Texto.Length > 40 ? c.Texto.Substring(0, 40) + "..." : c.Texto)}",
                    color = "#fd7e14"
                });
            }

            // Feed de actividad reciente (todos los eventos ordenados desc)
            var feedActividad = eventosCalendario
                .Select(e => (dynamic)e)
                .OrderByDescending(e => e.fecha)
                .Take(15)
                .ToList();

            ViewBag.EventosCalendario = System.Text.Json.JsonSerializer.Serialize(eventosCalendario);
            ViewBag.FeedActividad = feedActividad;

            // Meses con actividad (para navegación del calendario)
            var mesesConActividad = eventosCalendario
                .Select(e => ((dynamic)e).fecha.ToString().Substring(0, 7))
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            ViewBag.MesesConActividad = System.Text.Json.JsonSerializer.Serialize(mesesConActividad);

            return View(proyecto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.UsuariosAsignados)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto == null) return NotFound();

            ViewBag.Clientes = new SelectList(await _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", proyecto.ClienteId);
            ViewBag.UsuariosBanco = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.Email).ToListAsync();
            return View(proyecto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string descripcion, int? clienteId, string estado, string? repositorioGitHub,
            string? contraparteCliente, DateTime? fechaInicio, DateTime? fechaFinEstimada, DateTime? fechaPasoProduccion,
            string notas, int maxWarningsPermitidos, int maxInfosPermitidos, List<string> usuariosAsignadosIds)
        {
            var proyecto = await _context.Proyectos.Include(p => p.UsuariosAsignados).FirstOrDefaultAsync(p => p.Id == id);
            if (proyecto == null) return NotFound();

            proyecto.Descripcion = descripcion?.Trim() ?? string.Empty;
            proyecto.ClienteId = clienteId;
            proyecto.Estado = estado;
            proyecto.RepositorioGitHub = repositorioGitHub?.Trim();
            proyecto.ContraparteCliente = contraparteCliente?.Trim();
            proyecto.FechaInicio = fechaInicio;
            proyecto.FechaFinEstimada = fechaFinEstimada;
            proyecto.FechaPasoProduccion = fechaPasoProduccion;
            proyecto.Notas = notas?.Trim() ?? string.Empty;
            proyecto.MaxWarningsPermitidos = maxWarningsPermitidos;
            proyecto.MaxInfosPermitidos = maxInfosPermitidos;

            _context.ProyectosUsuarios.RemoveRange(proyecto.UsuariosAsignados.Where(u => u.RolEnProyecto == "Developer"));

            if (usuariosAsignadosIds != null)
            {
                foreach (var uId in usuariosAsignadosIds)
                {
                    if (!proyecto.UsuariosAsignados.Any(u => u.UsuarioId == uId && u.RolEnProyecto == "Admin"))
                    {
                        bool esNuevo = !proyecto.UsuariosAsignados.Any(u => u.UsuarioId == uId);
                        _context.ProyectosUsuarios.Add(new ProyectoUsuario { ProyectoId = proyecto.Id, UsuarioId = uId, RolEnProyecto = "Developer", FechaAsignacion = DateTime.Now });

                        if (esNuevo && !string.IsNullOrEmpty(proyecto.DriveFolderId))
                        {
                            var devUser = await _userManager.FindByIdAsync(uId);
                            if (devUser?.Email != null)
                                await _driveService.ShareFolderWithUserAsync(proyecto.DriveFolderId, devUser.Email, "writer");
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                "GESTOR: PROYECTO EDITADO",
                JsonSerializer.Serialize(new { ProyectoId = proyecto.Id, Nombre = proyecto.Nombre, Estado = estado }),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                proyecto.Id.ToString());

            return RedirectToAction(nameof(Details), new { id = proyecto.Id });
        }

        // ==========================================
        // FASES
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

            // Procesar el ZIP directamente desde los bytes, sin depender de rutas del disco
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

            // Limpiar los streams tras procesar
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
                // Deserializar con opciones que aceptan tanto PascalCase como camelCase
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var hallazgos = System.Text.Json.JsonSerializer.Deserialize<List<Finding>>(
                    validacion.DetalleErrores, options);

                // Re-serializar en camelCase para que el JS lo reciba correctamente
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
                // Si el JSON no parsea, devolver array vacío en lugar de colgarse
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

            // Resolver carpeta destino en Drive
            string driveUrl = "";
            try
            {
                if (!string.IsNullOrEmpty(proyecto.DriveFolderId) &&
                    NotebookValidator.Web.Models.GestorProyectos.DocumentoSubcategorias.Mapa.TryGetValue(
                        subcategoria, out var destino))
                {
                    // Buscar o crear carpeta padre (ej: 1_Diseño) dentro del proyecto
                    string carpetaPadreId = await _driveService.GetOrCreateFolderAsync(
                        destino.CarpetaPadre, proyecto.DriveFolderId);

                    // Buscar o crear subcarpeta (ej: Analisis_Tecnico)
                    string subcarpetaId = await _driveService.GetOrCreateFolderAsync(
                        destino.SubCarpeta, carpetaPadreId);

                    // Subir el archivo
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
                // Drive falló pero igual registramos en bitácora
                driveUrl = "";
                Console.WriteLine($"Error subiendo documento a Drive: {ex.Message}");
            }

            // Registrar en la bitácora
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

            // Auditoría
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

        // ② GlobalSearch — queries secuenciales (EF Core no soporta paralelo en mismo DbContext)
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

    }

    // DTO local para AddTablaCatalogo (antes era AddTablaProyecto con params sueltos)
    public class AddTablaRequest
    {
        public int ProyectoId { get; set; }
        public string NombreTabla { get; set; } = string.Empty;
        public string TipoTabla { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

}
