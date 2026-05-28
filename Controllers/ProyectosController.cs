using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.Models.GestorProyectos;
using NotebookValidator.Web.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class ProyectosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleDriveService _driveService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotebookValidatorService _validatorService;
        private readonly JobTransformationService _transformationService;
        private readonly WorkspaceService _workspaceService;

        public ProyectosController(
            ApplicationDbContext context,
            GoogleDriveService driveService,
            UserManager<ApplicationUser> userManager,
            NotebookValidatorService validatorService,
            JobTransformationService transformationService,
            WorkspaceService workspaceService)
        {
            _context = context;
            _driveService = driveService;
            _userManager = userManager;
            _validatorService = validatorService;
            _transformationService = transformationService;
            _workspaceService = workspaceService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            bool isAdmin = User.IsInRole("Admin");
            List<Proyecto> proyectos;

            if (isAdmin)
            {
                proyectos = await _context.Proyectos
                    .Include(p => p.Cliente)
                    .Include(p => p.Fases)
                    .Include(p => p.UsuariosAsignados).ThenInclude(ua => ua.Usuario)
                    .OrderByDescending(p => p.FechaCreacion)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .ToListAsync();
            }
            else
            {
                proyectos = await _context.Proyectos
                    .Include(p => p.Cliente)
                    .Include(p => p.Fases)
                    .Include(p => p.UsuariosAsignados).ThenInclude(ua => ua.Usuario)
                    .Where(p => p.UsuariosAsignados.Any(ua => ua.UsuarioId == currentUserId))
                    .OrderByDescending(p => p.FechaCreacion)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .ToListAsync();
            }
            return View(proyectos);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var usuariosBanco = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.Email).ToListAsync();
            var clientes = await _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync();
            ViewBag.UsuariosBanco = usuariosBanco;
            ViewBag.Clientes = new SelectList(clientes, "Id", "Nombre");
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
                if (clienteId.HasValue) { var c = await _context.Clientes.FindAsync(clienteId.Value); if (c != null) nombreCliente = c.Nombre; }

                var driveResult = await _driveService.CreateProjectStructureAsync($"PRJ_{nuevoProyecto.Id:D3}_{nuevoProyecto.Nombre.Replace(" ", "_")}", nombreCliente);
                nuevoProyecto.DriveFolderId = driveResult.RootFolderId; nuevoProyecto.DriveFolderUrl = driveResult.RootFolderUrl;
                _context.Entry(nuevoProyecto).State = EntityState.Modified;

                int o = 1;
                foreach (var f in new List<string> { "1_Diseño_Arquitectura", "2_Desarrollo_Notebooks", "3_Pruebas_Certificacion", "4_Paso_A_Produccion" })
                    _context.FasesProyecto.Add(new FaseProyecto { ProyectoId = nuevoProyecto.Id, NombreFase = f, EstadoFase = "Pendiente", Orden = o++ });

                if (fasesSeleccionadas != null) foreach (var f in fasesSeleccionadas) _context.FasesProyecto.Add(new FaseProyecto { ProyectoId = nuevoProyecto.Id, NombreFase = f, EstadoFase = "Pendiente", Orden = o++ });

                string cId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                _context.ProyectosUsuarios.Add(new ProyectoUsuario { ProyectoId = nuevoProyecto.Id, UsuarioId = cId, RolEnProyecto = "Admin", FechaAsignacion = DateTime.Now });

                // =========================================================
                // MAGIA: ASIGNAR PERMISOS EN DRIVE AUTOMÁTICAMENTE
                // =========================================================
                var adminUser = await _userManager.FindByIdAsync(cId);
                if (adminUser != null && !string.IsNullOrEmpty(adminUser.Email))
                {
                    await _driveService.ShareFolderWithUserAsync(nuevoProyecto.DriveFolderId, adminUser.Email, "writer");
                }

                if (usuariosAsignadosIds != null)
                {
                    foreach (var uId in usuariosAsignadosIds)
                    {
                        if (uId != cId)
                        {
                            _context.ProyectosUsuarios.Add(new ProyectoUsuario { ProyectoId = nuevoProyecto.Id, UsuarioId = uId, RolEnProyecto = "Developer", FechaAsignacion = DateTime.Now });

                            var devUser = await _userManager.FindByIdAsync(uId);
                            if (devUser != null && !string.IsNullOrEmpty(devUser.Email))
                            {
                                await _driveService.ShareFolderWithUserAsync(nuevoProyecto.DriveFolderId, devUser.Email, "writer");
                            }
                        }
                    }
                }
                // =========================================================

                await _context.SaveChangesAsync(); await transaction.CommitAsync();
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
                        if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\") || entry.FullName.Contains("__MACOSX")) continue;

                        totalArchivos++;
                        string ext = Path.GetExtension(entry.Name).ToLower();
                        if (string.IsNullOrEmpty(ext)) ext = "otros";

                        if (conteoExtensiones.ContainsKey(ext)) conteoExtensiones[ext]++;
                        else conteoExtensiones[ext] = 1;

                        if (ext == ".ipynb" || ext == ".py" || ext == ".sql" || ext == ".scala")
                        {
                            archivosAnalizables.Add(entry.Name);
                        }
                    }
                }
                catch { }
            }

            ViewBag.TotalArchivos = totalArchivos;
            ViewBag.ConteoExtensiones = conteoExtensiones;
            ViewBag.ArchivosAnalizables = archivosAnalizables;

            return View(proyecto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFaseStatus(int faseId, string nuevoEstado)
        {
            var fase = await _context.FasesProyecto.FindAsync(faseId);
            if (fase == null) return Json(new { success = false, message = "Fase no encontrada" });

            string usuarioEmail = User.Identity?.Name ?? "Usuario Anónimo";
            fase.EstadoFase = nuevoEstado;
            fase.FechaActualizacion = DateTime.Now;
            fase.UsuarioActualizacion = usuarioEmail.Split('@')[0];

            await _context.SaveChangesAsync();
            return Json(new { success = true });
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
        public async Task<IActionResult> Edit(int id, string descripcion, int? clienteId, string estado, string? repositorioGitHub, string? contraparteCliente,
            DateTime? fechaInicio, DateTime? fechaFinEstimada, DateTime? fechaPasoProduccion, string notas, int maxWarningsPermitidos, int maxInfosPermitidos, List<string> usuariosAsignadosIds)
        {
            var proyecto = await _context.Proyectos.Include(p => p.UsuariosAsignados).FirstOrDefaultAsync(p => p.Id == id);
            if (proyecto == null) return NotFound();

            proyecto.Descripcion = descripcion?.Trim() ?? string.Empty; proyecto.ClienteId = clienteId; proyecto.Estado = estado;
            proyecto.RepositorioGitHub = repositorioGitHub?.Trim(); proyecto.ContraparteCliente = contraparteCliente?.Trim();
            proyecto.FechaInicio = fechaInicio; proyecto.FechaFinEstimada = fechaFinEstimada; proyecto.FechaPasoProduccion = fechaPasoProduccion;
            proyecto.Notas = notas?.Trim() ?? string.Empty; proyecto.MaxWarningsPermitidos = maxWarningsPermitidos; proyecto.MaxInfosPermitidos = maxInfosPermitidos;

            _context.ProyectosUsuarios.RemoveRange(proyecto.UsuariosAsignados.Where(u => u.RolEnProyecto == "Developer"));

            if (usuariosAsignadosIds != null)
            {
                foreach (var uId in usuariosAsignadosIds)
                {
                    if (!proyecto.UsuariosAsignados.Any(u => u.UsuarioId == uId && u.RolEnProyecto == "Admin"))
                    {
                        // Detectamos si es un usuario que no estaba antes en el proyecto
                        bool esNuevo = !proyecto.UsuariosAsignados.Any(u => u.UsuarioId == uId);

                        _context.ProyectosUsuarios.Add(new ProyectoUsuario { ProyectoId = proyecto.Id, UsuarioId = uId, RolEnProyecto = "Developer", FechaAsignacion = DateTime.Now });

                        // MAGIA: ASIGNAR PERMISO EN DRIVE A LOS NUEVOS MIEMBROS
                        if (esNuevo && !string.IsNullOrEmpty(proyecto.DriveFolderId))
                        {
                            var devUser = await _userManager.FindByIdAsync(uId);
                            if (devUser != null && !string.IsNullOrEmpty(devUser.Email))
                            {
                                await _driveService.ShareFolderWithUserAsync(proyecto.DriveFolderId, devUser.Email, "writer");
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync(); return RedirectToAction(nameof(Details), new { id = proyecto.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateProjectNotebook(int proyectoId)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null || string.IsNullOrEmpty(proyecto.RutaWorkspaceLocal))
                return Json(new { success = false, message = "El Workspace está vacío. Sube tu código primero." });

            byte[] fileBytes = _workspaceService.GetWorkspaceFile(proyecto.RutaWorkspaceLocal);

            using var stream = new MemoryStream(fileBytes);
            IFormFile file = new FormFile(stream, 0, fileBytes.Length, "file", Path.GetFileName(proyecto.RutaWorkspaceLocal));

            var user = await _userManager.GetUserAsync(User);
            var (hallazgos, _, _) = await _validatorService.ProcessFilesAsync(new List<(Stream, string)> { (file.OpenReadStream(), file.FileName) });

            int criticos = hallazgos.Count(h => h.Severity == "Critical");
            int warnings = hallazgos.Count(h => h.Severity == "Warning");

            bool pasoValidacion = (criticos == 0) && (warnings <= proyecto.MaxWarningsPermitidos);
            int score = pasoValidacion ? 100 : Math.Max(0, 100 - (criticos * 10) - (warnings * 2));

            _context.NotebookValidaciones.Add(new NotebookValidacion
            {
                ProyectoId = proyecto.Id,
                NombreArchivo = file.FileName,
                FechaValidacion = DateTime.Now,
                Usuario = user?.Email ?? "Usuario Local",
                PasoValidacion = pasoValidacion,
                Score = score,
                DetalleErrores = System.Text.Json.JsonSerializer.Serialize(hallazgos)
            });

            proyecto.EstadoValidacionWorkspace = pasoValidacion ? "Validado" : "Rechazado";

            var faseQA = await _context.FasesProyecto.FirstOrDefaultAsync(f => f.ProyectoId == proyecto.Id && f.NombreFase.Contains("Pruebas_Certificacion"));
            if (faseQA != null)
            {
                faseQA.EstadoFase = pasoValidacion ? "Completado" : "En Progreso";
                faseQA.FechaActualizacion = DateTime.Now;
                faseQA.UsuarioActualizacion = user?.Email?.Split('@')[0] ?? "Sistema QA";
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, paso = pasoValidacion, score = score, criticos = criticos, warnings = warnings });
        }

        [HttpGet]
        public async Task<IActionResult> GetDetalleValidacion(int id)
        {
            var validacion = await _context.NotebookValidaciones.FindAsync(id);
            if (validacion == null || string.IsNullOrEmpty(validacion.DetalleErrores)) return Json(new List<object>());
            return Content(validacion.DetalleErrores, "application/json");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteValidacion(int id)
        {
            if (!User.IsInRole("Admin")) return Json(new { success = false, message = "Acceso denegado." });
            var validacion = await _context.NotebookValidaciones.FindAsync(id);
            if (validacion != null) { _context.NotebookValidaciones.Remove(validacion); await _context.SaveChangesAsync(); }
            return Json(new { success = true, message = "Revisión histórica eliminada." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTablaProyecto(int proyectoId, string nombreTabla, string tipoTabla, string descripcion, string? rutaUbicacion = null)
        {
            if (string.IsNullOrWhiteSpace(nombreTabla)) return Json(new { success = false, message = "El nombre de la tabla es obligatorio." });
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null) return Json(new { success = false });

            var tablaMaestra = await _context.TablasMaestras.FirstOrDefaultAsync(t => t.NombreTabla == nombreTabla.Trim() && t.ClienteId == proyecto.ClienteId);
            if (tablaMaestra == null)
            {
                tablaMaestra = new TablaMaestra { NombreTabla = nombreTabla.Trim(), Descripcion = descripcion?.Trim(), ClienteId = proyecto.ClienteId };
                _context.TablasMaestras.Add(tablaMaestra);
                await _context.SaveChangesAsync();
            }

            var nuevaTabla = new TablaProyecto { ProyectoId = proyectoId, TablaMaestraId = tablaMaestra.Id, TipoTabla = tipoTabla, RutaUbicacion = rutaUbicacion?.Trim() };
            _context.TablasProyecto.Add(nuevaTabla);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = nuevaTabla.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTablaProyecto(int id)
        {
            var tabla = await _context.TablasProyecto.FindAsync(id);
            if (tabla == null) return Json(new { success = false });
            _context.TablasProyecto.Remove(tabla);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScanLineageFromCode(int proyectoId)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null || string.IsNullOrEmpty(proyecto.RutaWorkspaceLocal))
                return Json(new { success = false, message = "El Workspace está vacío. Sube tu código primero." });

            byte[] fileBytes = _workspaceService.GetWorkspaceFile(proyecto.RutaWorkspaceLocal);

            using var stream = new MemoryStream(fileBytes);
            IFormFile file = new FormFile(stream, 0, fileBytes.Length, "file", Path.GetFileName(proyecto.RutaWorkspaceLocal));

            if (file == null || file.Length == 0) return Json(new { success = false, message = "Sube un archivo válido." });

            try
            {
                string content = "";

                if (file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    using var archive = new System.IO.Compression.ZipArchive(new MemoryStream(ms.ToArray()));
                    foreach (var entry in archive.Entries.Where(e => e.Name.EndsWith(".py") || e.Name.EndsWith(".sql") || e.Name.EndsWith(".ipynb")))
                    {
                        using var entryStream = new StreamReader(entry.Open());
                        content += await entryStream.ReadToEndAsync() + "\n";
                    }
                }
                else
                {
                    using var reader = new StreamReader(file.OpenReadStream());
                    content = await reader.ReadToEndAsync();
                }

                content = Regex.Replace(content, @"[""']{1,3}\s*\+\s*([a-zA-Z0-9_]+)\s*\+\s*[""']{1,3}", "${$1}");

                var rawSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var rawTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (Match m in Regex.Matches(content, @"(?i)(?:FROM|JOIN)\s+([`a-zA-Z0-9_\.\{\}\$]+)"))
                {
                    string match = m.Groups[1].Value.Replace("`", "").Trim();
                    if (!match.Equals("SELECT", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(match))
                        rawSources.Add(match);
                }
                foreach (Match m in Regex.Matches(content, @"(?i)spark\.(?:read\.)?table\s*\(\s*f?[""']([^""']+)[""']\s*\)"))
                    rawSources.Add(m.Groups[1].Value);
                foreach (Match m in Regex.Matches(content, @"(?i)(?:saveAsTable|insertInto)\s*\(\s*f?[""']([^""']+)[""']\s*\)"))
                    rawTargets.Add(m.Groups[1].Value);
                foreach (Match m in Regex.Matches(content, @"(?i)(?:INSERT\s+(?:INTO|OVERWRITE)|MERGE\s+INTO|CREATE\s+(?:OR\s+REPLACE\s+)?TABLE\s+(?:IF\s+NOT\s+EXISTS)?)\s+([`a-zA-Z0-9_\.\{\}\$]+)"))
                {
                    string match = m.Groups[1].Value.Replace("`", "").Trim();
                    if (!string.IsNullOrWhiteSpace(match)) rawTargets.Add(match);
                }

                Func<string, bool> isTempOrJunk = s => {
                    string low = s.ToLower();
                    return low.Contains("tmp") || low.Contains("temp") || low.Contains("vista") || low.StartsWith("v_") || low.StartsWith("vw_") || low.StartsWith("vt_") || low.Equals("dual") || low.Length <= 3 || !Regex.IsMatch(low, "[a-z]") || !(s.Contains(".") || s.Contains("{") || s.Contains("$"));
                };

                var cleanSources = rawSources.Where(x => !isTempOrJunk(x)).ToList();
                var cleanTargets = rawTargets.Where(x => !isTempOrJunk(x)).ToList();

                var tablasDetectadas = new List<object>();
                var tablasVistas = new HashSet<string>();

                void AgregarTablas(List<string> lista, string tipo)
                {
                    foreach (var t in lista)
                    {
                        string nombreLimpio = t.Replace("{", "").Replace("}", "").Replace("$", "").Replace("`", "");
                        string key = $"{tipo}-{nombreLimpio}";

                        if (!tablasVistas.Contains(key))
                        {
                            tablasVistas.Add(key);
                            tablasDetectadas.Add(new { nombreTabla = nombreLimpio, tipoTabla = tipo });
                        }
                    }
                }

                AgregarTablas(cleanSources, "Origen");
                AgregarTablas(cleanTargets, "Salida");

                return Json(new { success = true, tablas = tablasDetectadas });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error escaneando el archivo: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveScannedLineage([FromBody] SaveScannedLineageRequest request)
        {
            if (request == null || request.Tablas == null || !request.Tablas.Any())
                return Json(new { success = false, message = "No se recibieron tablas para guardar." });

            var proyecto = await _context.Proyectos.FindAsync(request.ProyectoId);
            if (proyecto == null) return Json(new { success = false, message = "Proyecto no encontrado." });

            int agregadas = 0;
            foreach (var t in request.Tablas)
            {
                var tablaMaestra = await _context.TablasMaestras.FirstOrDefaultAsync(m => m.NombreTabla == t.NombreTabla && m.ClienteId == proyecto.ClienteId);
                if (tablaMaestra == null)
                {
                    tablaMaestra = new TablaMaestra { NombreTabla = t.NombreTabla, ClienteId = proyecto.ClienteId };
                    _context.TablasMaestras.Add(tablaMaestra);
                    await _context.SaveChangesAsync();
                }

                bool yaExiste = await _context.TablasProyecto.AnyAsync(tp => tp.ProyectoId == request.ProyectoId && tp.TablaMaestraId == tablaMaestra.Id && tp.TipoTabla == t.TipoTabla);
                if (!yaExiste)
                {
                    _context.TablasProyecto.Add(new TablaProyecto { ProyectoId = request.ProyectoId, TablaMaestraId = tablaMaestra.Id, TipoTabla = t.TipoTabla });
                    agregadas++;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Se agregaron {agregadas} tablas al catálogo." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnalyzeJobMatch(int proyectoId)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.TablasCatalogo)
                .ThenInclude(tc => tc.TablaMaestra)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == proyectoId);

            if (proyecto == null || string.IsNullOrEmpty(proyecto.RutaWorkspaceLocal))
                return Json(new { success = false, message = "Workspace vacío. Sube código primero." });

            try
            {
                byte[] zipBytes = _workspaceService.GetWorkspaceFile(proyecto.RutaWorkspaceLocal);
                using var ms = new MemoryStream(zipBytes);
                using var archive = new System.IO.Compression.ZipArchive(ms);

                string content = "";
                foreach (var entry in archive.Entries.Where(e => e.Name.EndsWith(".py") || e.Name.EndsWith(".sql") || e.Name.EndsWith(".ipynb")))
                {
                    using var entryStream = new StreamReader(entry.Open());
                    content += await entryStream.ReadToEndAsync() + "\n";
                }

                content = Regex.Replace(content, @"[""']{1,3}\s*\+\s*([a-zA-Z0-9_]+)\s*\+\s*[""']{1,3}", "${$1}");
                var rawTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (Match m in Regex.Matches(content, @"(?i)(?:FROM|JOIN)\s+([`a-zA-Z0-9_\.\{\}\$]+)")) rawTables.Add(m.Groups[1].Value.Replace("`", "").Trim());
                foreach (Match m in Regex.Matches(content, @"(?i)spark\.(?:read\.)?table\s*\(\s*f?[""']([^""']+)[""']\s*\)")) rawTables.Add(m.Groups[1].Value);
                foreach (Match m in Regex.Matches(content, @"(?i)(?:saveAsTable|insertInto)\s*\(\s*f?[""']([^""']+)[""']\s*\)")) rawTables.Add(m.Groups[1].Value);
                foreach (Match m in Regex.Matches(content, @"(?i)(?:INSERT\s+(?:INTO|OVERWRITE)|MERGE\s+INTO|CREATE\s+(?:OR\s+REPLACE\s+)?TABLE\s+(?:IF\s+NOT\s+EXISTS)?)\s+([`a-zA-Z0-9_\.\{\}\$]+)")) rawTables.Add(m.Groups[1].Value.Replace("`", "").Trim());

                Func<string, bool> isTempOrJunk = s => { string low = s.ToLower(); return low.Contains("tmp") || low.Contains("temp") || low.StartsWith("vw_") || low.StartsWith("vt_") || low.Equals("dual") || low.Length <= 3 || !Regex.IsMatch(low, "[a-z]") || !(s.Contains(".") || s.Contains("{") || s.Contains("$")); };

                var tablasEnCodigo = rawTables.Where(x => !isTempOrJunk(x) && !x.Equals("SELECT", StringComparison.OrdinalIgnoreCase))
                                              .Select(x => x.Replace("{", "").Replace("}", "").Replace("$", "").Replace("`", "").ToLower())
                                              .ToHashSet();

                var tablasEnCatalogo = proyecto.TablasCatalogo
                                               .Where(t => t.TablaMaestra != null)
                                               .Select(t => t.TablaMaestra!.NombreTabla.ToLower())
                                               .ToHashSet();

                var matchPerfecto = tablasEnCodigo.Intersect(tablasEnCatalogo).ToList();
                var faltanEnCatalogo = tablasEnCodigo.Except(tablasEnCatalogo).ToList();
                var tablasFantasmas = tablasEnCatalogo.Except(tablasEnCodigo).ToList();

                var agrupadoMatch = AgruparPorBaseDeDatos(matchPerfecto);
                var agrupadoMissing = AgruparPorBaseDeDatos(faltanEnCatalogo);
                var agrupadoGhost = AgruparPorBaseDeDatos(tablasFantasmas);

                return Json(new { success = true, matchDBs = agrupadoMatch, missingDBs = agrupadoMissing, ghostDBs = agrupadoGhost });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private Dictionary<string, List<string>> AgruparPorBaseDeDatos(List<string> tablas)
        {
            var agrupado = new Dictionary<string, List<string>>();
            foreach (var t in tablas)
            {
                string dbName = "default";
                string tableName = t;
                if (t.Contains("."))
                {
                    var partes = t.Split('.');
                    dbName = partes[0];
                    tableName = string.Join(".", partes.Skip(1));
                }
                if (!agrupado.ContainsKey(dbName)) agrupado[dbName] = new List<string>();
                agrupado[dbName].Add(tableName);
            }
            return agrupado;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAndUploadJob(int proyectoId, string bundleName, string permLevel, string permUser, bool autocert, IFormFile yamlFile)
        {
            if (yamlFile == null || yamlFile.Length == 0) return Json(new { success = false, message = "Falta el archivo YAML base." });

            var proyecto = await _context.Proyectos
                .Include(p => p.Fases)
                .Include(p => p.TablasCatalogo).ThenInclude(tc => tc.TablaMaestra)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == proyectoId);

            if (proyecto == null || string.IsNullOrEmpty(proyecto.RutaWorkspaceLocal)) return Json(new { success = false, message = "Proyecto no encontrado o Workspace vacío." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                byte[] zipBytes = _workspaceService.GetWorkspaceFile(proyecto.RutaWorkspaceLocal);

                string yamlContent;
                using (var reader = new StreamReader(yamlFile.OpenReadStream())) { yamlContent = await reader.ReadToEndAsync(); }
                var match = Regex.Match(yamlContent, @"name:\s*[""']?([^""'\n]+)[""']?");
                string originalJobName = match.Success ? match.Groups[1].Value : Path.GetFileNameWithoutExtension(yamlFile.FileName);
                string cleanJobName = Regex.Replace(originalJobName, @"^\[dev\]|^\[DEV\]|^dev_|^dev-", "", RegexOptions.IgnoreCase).Trim();

                var origins = proyecto.TablasCatalogo.Where(t => t.TipoTabla == "Origen" && t.TablaMaestra != null).Select(t => t.TablaMaestra!.NombreTabla).ToList();
                var targets = proyecto.TablasCatalogo.Where(t => t.TipoTabla == "Salida" && t.TablaMaestra != null).Select(t => t.TablaMaestra!.NombreTabla).ToList();
                string sourceStr = string.Join(";", origins);
                string targetStr = string.Join(";", targets);

                var generatedFiles = _transformationService.GenerateBundleConfigs(
                    new List<string> { yamlContent },
                    new List<string> { cleanJobName }, new List<string> { cleanJobName }, new List<string> { cleanJobName },
                    new List<string> { permLevel }, new List<string> { permUser },
                    new List<bool> { autocert }, new List<bool> { autocert }, new List<bool> { autocert },
                    new List<string> { sourceStr }, new List<string> { targetStr },
                    bundleName
                );

                using var ms = new MemoryStream();
                using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    foreach (var gf in generatedFiles)
                    {
                        var entry = archive.CreateEntry(gf.Key);
                        using var writer = new StreamWriter(entry.Open());
                        writer.Write(gf.Value);
                    }

                    using var msZip = new MemoryStream(zipBytes);
                    using var sourceArchive = new System.IO.Compression.ZipArchive(msZip, System.IO.Compression.ZipArchiveMode.Read);
                    foreach (var entry in sourceArchive.Entries)
                    {
                        var newEntry = archive.CreateEntry("notebooks/" + entry.FullName);
                        using var sourceEntryStream = entry.Open();
                        using var destEntryStream = newEntry.Open();
                        sourceEntryStream.CopyTo(destEntryStream);
                    }
                }

                byte[] finalZipBytes = ms.ToArray();
                string fileName = $"Artefactos_{bundleName}_{DateTime.Now:yyyyMMdd_HHmm}.zip";

                string driveUrl = "";
                if (!string.IsNullOrEmpty(proyecto.DriveFolderId))
                {
                    string entregablesFolderId = await _driveService.GetOrCreateFolderAsync("4_Entregables", proyecto.DriveFolderId);
                    string repoFolderId = await _driveService.GetOrCreateFolderAsync(bundleName, entregablesFolderId);
                    driveUrl = await _driveService.UploadArtifactToFolderAsync(repoFolderId, fileName, finalZipBytes);
                }

                _context.ArtefactosJob.Add(new ArtefactoJob { ProyectoId = proyecto.Id, UsuarioGenerador = permUser, NombreBundle = bundleName, FechaGeneracion = DateTime.Now, ArchivoDriveUrl = driveUrl });

                var faseProduccion = proyecto.Fases.FirstOrDefault(f => f.NombreFase.Contains("Paso_A_Produccion"));
                if (faseProduccion != null)
                {
                    faseProduccion.EstadoFase = "Completado";
                    faseProduccion.FechaActualizacion = DateTime.Now;
                    faseProduccion.UsuarioActualizacion = permUser;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                string tempToken = Guid.NewGuid().ToString("N");
                System.IO.File.WriteAllBytes(Path.Combine(Path.GetTempPath(), tempToken + ".zip"), finalZipBytes);

                return Json(new { success = true, driveUrl = driveUrl, downloadToken = tempToken, fileName = fileName });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error en el motor de transformación: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DownloadArtifact(string token, string fileName)
        {
            string path = Path.Combine(Path.GetTempPath(), token + ".zip");
            if (!System.IO.File.Exists(path)) return NotFound();

            var bytes = System.IO.File.ReadAllBytes(path);
            System.IO.File.Delete(path);

            return File(bytes, "application/zip", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadToWorkspace(int proyectoId, IFormFile file)
        {
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No se detectó ningún archivo." });

            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null) return Json(new { success = false, message = "Proyecto no encontrado." });

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

                    var nombresArchivos = archive.Entries
                        .Where(e => !e.FullName.EndsWith("/") && !e.FullName.Contains("__MACOSX"))
                        .Select(e => e.Name)
                        .ToList();

                    archivosIndexados = string.Join(";", nombresArchivos);
                }
                catch { }

                proyecto.ArchivosIndexados = archivosIndexados;
                proyecto.RutaWorkspaceLocal = rutaLocal;
                proyecto.FechaActualizacionWorkspace = DateTime.Now;
                proyecto.EstadoValidacionWorkspace = "Pendiente_Validacion";
                proyecto.EstadoSincronizacionDrive = "Sincronizando";

                var faseDev = await _context.FasesProyecto.FirstOrDefaultAsync(f => f.ProyectoId == proyecto.Id && f.NombreFase.Contains("Desarrollo_Notebooks"));
                if (faseDev != null)
                {
                    faseDev.EstadoFase = "En Progreso";
                    faseDev.FechaActualizacion = DateTime.Now;
                    faseDev.UsuarioActualizacion = User.Identity?.Name?.Split('@')[0] ?? "Sistema";
                }

                var faseQAReset = await _context.FasesProyecto.FirstOrDefaultAsync(f => f.ProyectoId == proyecto.Id && f.NombreFase.Contains("Pruebas_Certificacion"));
                if (faseQAReset != null && faseQAReset.EstadoFase == "Completado")
                {
                    faseQAReset.EstadoFase = "Pendiente";
                    faseQAReset.FechaActualizacion = DateTime.Now;
                    faseQAReset.UsuarioActualizacion = "Sistema (Nuevo Código)";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Código cargado e indexado en el buscador." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error al guardar en Workspace: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComentario([FromBody] NuevoComentarioDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Texto))
                return Json(new { success = false, message = "El texto no puede estar vacío." });

            var comentario = new ComentarioProyecto
            {
                ProyectoId = request.ProyectoId,
                Usuario = User.Identity?.Name?.Split('@')[0] ?? "Anónimo",
                Texto = request.Texto.Trim(),
                Tipo = request.Tipo,
                FechaVencimiento = request.FechaVencimiento,
                FechaCreacion = DateTime.Now,
                Resuelto = false
            };

            _context.ComentariosProyecto.Add(comentario);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Comentario añadido con éxito." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolverAlerta(int id)
        {
            var comentario = await _context.ComentariosProyecto.FindAsync(id);
            if (comentario != null)
            {
                comentario.Resuelto = true;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComentario(int id)
        {
            var comentario = await _context.ComentariosProyecto.FindAsync(id);
            if (comentario != null)
            {
                _context.ComentariosProyecto.Remove(comentario);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

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
                .ToListAsync();

            foreach (var p in proyectos)
            {
                resultados.Add(new SearchResultDto
                {
                    Categoria = "Proyectos",
                    Titulo = p.Nombre,
                    Descripcion = p.Cliente?.Nombre ?? "Proyecto Interno",
                    Url = Url.Action("Details", "Proyectos", new { id = p.Id }),
                    Icono = "bi-briefcase-fill text-primary"
                });
            }

            var tablas = await _context.TablasProyecto
                .Include(t => t.TablaMaestra)
                .Include(t => t.Proyecto)
                .Where(t => (t.TablaMaestra != null && t.TablaMaestra.NombreTabla.ToLower().Contains(query)) ||
                            (t.TablaMaestra != null && t.TablaMaestra.Descripcion != null && t.TablaMaestra.Descripcion.ToLower().Contains(query)))
                .Take(5)
                .ToListAsync();

            foreach (var t in tablas)
            {
                string nombreTablaSafe = t.TablaMaestra?.NombreTabla ?? "Tabla Desconocida";
                string nombreProyectoSafe = t.Proyecto?.Nombre ?? "Proyecto Desconocido";

                resultados.Add(new SearchResultDto
                {
                    Categoria = "Catálogo de Linaje",
                    Titulo = nombreTablaSafe,
                    Descripcion = $"Proyecto: {nombreProyectoSafe}",
                    Url = Url.Action("Details", "Proyectos", new { id = t.ProyectoId }) + "#linaje",
                    Icono = "bi-table text-success"
                });
            }

            var proyectosConCodigo = await _context.Proyectos
                .Where(p => p.ArchivosIndexados != null && p.ArchivosIndexados.ToLower().Contains(query))
                .Take(5)
                .ToListAsync();

            foreach (var p in proyectosConCodigo)
            {
                var arrArchivos = p.ArchivosIndexados?.Split(';') ?? Array.Empty<string>();

                var matchedFiles = arrArchivos
                    .Where(f => f.ToLower().Contains(query))
                    .Take(2);

                foreach (var f in matchedFiles)
                {
                    resultados.Add(new SearchResultDto
                    {
                        Categoria = "Código y Notebooks",
                        Titulo = f,
                        Descripcion = $"En Workspace de: {p.Nombre ?? "Desconocido"}",
                        Url = Url.Action("Details", "Proyectos", new { id = p.Id }) + "#calidad",
                        Icono = "bi-file-earmark-code text-info"
                    });
                }
            }

            var comentarios = await _context.ComentariosProyecto
                .Include(c => c.Proyecto)
                .Where(c => c.Texto.ToLower().Contains(query) || (c.Usuario != null && c.Usuario.ToLower().Contains(query)))
                .Take(5)
                .ToListAsync();

            foreach (var c in comentarios)
            {
                string txtSafe = string.IsNullOrEmpty(c.Texto) ? "" : c.Texto;
                string usrSafe = string.IsNullOrEmpty(c.Usuario) ? "Desconocido" : c.Usuario;
                string proyNameSafe = c.Proyecto?.Nombre ?? "Desconocido";

                resultados.Add(new SearchResultDto
                {
                    Categoria = "Bitácora y Alertas",
                    Titulo = txtSafe.Length > 45 ? txtSafe.Substring(0, 45) + "..." : txtSafe,
                    Descripcion = $"@@{usrSafe} en {proyNameSafe}",
                    Url = Url.Action("Details", "Proyectos", new { id = c.ProyectoId }),
                    Icono = c.Tipo == "Recordatorio" ? "bi-clock-history text-warning" : (c.Tipo == "Advertencia" ? "bi-exclamation-triangle text-danger" : "bi-chat-left-text text-secondary")
                });
            }

            return Json(resultados);
        }
    }

    public class SaveScannedLineageRequest
    {
        public int ProyectoId { get; set; }
        public List<TablaScaneada> Tablas { get; set; } = new();
    }

    public class TablaScaneada
    {
        public string NombreTabla { get; set; } = string.Empty;
        public string TipoTabla { get; set; } = string.Empty;
    }

    public class NuevoComentarioDto
    {
        public int ProyectoId { get; set; }
        public string Texto { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public DateTime? FechaVencimiento { get; set; }
    }

    public class SearchResultDto
    {
        public string Categoria { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
    }
}
