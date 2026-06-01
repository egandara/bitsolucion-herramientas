using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models.GestorProyectos;
using NotebookValidator.Web.Services;
using NotebookValidator.Web.ViewModels.GestorProyectos;
using System.Text.RegularExpressions;

namespace NotebookValidator.Web.Services.GestorProyectos
{
    public class LineageService
    {
        private readonly ApplicationDbContext _context;
        private readonly WorkspaceService _workspaceService;

        public LineageService(ApplicationDbContext context, WorkspaceService workspaceService)
        {
            _context = context;
            _workspaceService = workspaceService;
        }

        // ==========================================
        // ESCANEO DE LINAJE DESDE CÓDIGO
        // ==========================================
        public async Task<(bool Success, string Message, List<object> Tablas)> ScanLineageFromCodeAsync(int proyectoId)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null || string.IsNullOrEmpty(proyecto.RutaWorkspaceLocal))
                return (false, "El Workspace está vacío. Sube tu código primero.", new List<object>());

            try
            {
                byte[] fileBytes = _workspaceService.GetWorkspaceFile(proyecto.RutaWorkspaceLocal);
                string content = await ExtraerContenidoZipAsync(fileBytes);

                var (sources, targets) = ExtractTablesFromContent(content);

                var tablasDetectadas = new List<object>();
                var tablasVistas = new HashSet<string>();

                AgregarTablas(sources, "Origen", tablasDetectadas, tablasVistas);
                AgregarTablas(targets, "Salida", tablasDetectadas, tablasVistas);

                return (true, string.Empty, tablasDetectadas);
            }
            catch (Exception ex)
            {
                return (false, "Error escaneando el archivo: " + ex.Message, new List<object>());
            }
        }

        // ==========================================
        // ANÁLISIS DE MATCH ENTRE CÓDIGO Y CATÁLOGO
        // ==========================================
        public async Task<(bool Success, string Message, Dictionary<string, List<string>> MatchDBs, Dictionary<string, List<string>> MissingDBs, Dictionary<string, List<string>> GhostDBs)> AnalyzeJobMatchAsync(int proyectoId)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.TablasCatalogo)
                .ThenInclude(tc => tc.TablaMaestra)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == proyectoId);

            if (proyecto == null || string.IsNullOrEmpty(proyecto.RutaWorkspaceLocal))
                return (false, "Workspace vacío. Sube código primero.", null!, null!, null!);

            try
            {
                byte[] zipBytes = _workspaceService.GetWorkspaceFile(proyecto.RutaWorkspaceLocal);
                string content = await ExtraerContenidoZipAsync(zipBytes);

                var (sources, targets) = ExtractTablesFromContent(content);
                var allRaw = sources.Union(targets).ToHashSet(StringComparer.OrdinalIgnoreCase);

                var tablasEnCodigo = allRaw
                    .Select(x => x.Replace("{", "").Replace("}", "").Replace("$", "").Replace("`", "").ToLower())
                    .Where(x => !x.Equals("select", StringComparison.OrdinalIgnoreCase))
                    .ToHashSet();

                var tablasEnCatalogo = proyecto.TablasCatalogo
                    .Where(t => t.TablaMaestra != null)
                    .Select(t => t.TablaMaestra!.NombreTabla.ToLower())
                    .ToHashSet();

                var matchPerfecto = tablasEnCodigo.Intersect(tablasEnCatalogo).ToList();
                var faltanEnCatalogo = tablasEnCodigo.Except(tablasEnCatalogo).ToList();
                var tablasFantasmas = tablasEnCatalogo.Except(tablasEnCodigo).ToList();

                return (true, string.Empty,
                    AgruparPorBaseDeDatos(matchPerfecto),
                    AgruparPorBaseDeDatos(faltanEnCatalogo),
                    AgruparPorBaseDeDatos(tablasFantasmas));
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null!, null!, null!);
            }
        }

        // ==========================================
        // GUARDAR TABLAS ESCANEADAS EN EL CATÁLOGO
        // ==========================================
        public async Task<(bool Success, string Message)> SaveScannedLineageAsync(SaveScannedLineageRequest request)
        {
            if (request?.Tablas == null || !request.Tablas.Any())
                return (false, "No se recibieron tablas para guardar.");

            var proyecto = await _context.Proyectos.FindAsync(request.ProyectoId);
            if (proyecto == null)
                return (false, "Proyecto no encontrado.");

            int agregadas = 0;
            foreach (var t in request.Tablas)
            {
                var tablaMaestra = await _context.TablasMaestras
                    .FirstOrDefaultAsync(m => m.NombreTabla == t.NombreTabla && m.ClienteId == proyecto.ClienteId);

                if (tablaMaestra == null)
                {
                    tablaMaestra = new TablaMaestra { NombreTabla = t.NombreTabla, ClienteId = proyecto.ClienteId };
                    _context.TablasMaestras.Add(tablaMaestra);
                    await _context.SaveChangesAsync();
                }

                bool yaExiste = await _context.TablasProyecto.AnyAsync(tp =>
                    tp.ProyectoId == request.ProyectoId &&
                    tp.TablaMaestraId == tablaMaestra.Id &&
                    tp.TipoTabla == t.TipoTabla) ||
                    // Verificar también por nombre normalizado para evitar duplicados
                    await _context.TablasProyecto.AnyAsync(tp =>
                    tp.ProyectoId == request.ProyectoId &&
                    tp.TablaMaestra != null &&
                    tp.TablaMaestra.NombreTabla.ToLower() == t.NombreTabla.ToLower() &&
                    tp.TipoTabla == t.TipoTabla);

                if (!yaExiste)
                {
                    _context.TablasProyecto.Add(new TablaProyecto
                    {
                        ProyectoId = request.ProyectoId,
                        TablaMaestraId = tablaMaestra.Id,
                        TipoTabla = t.TipoTabla
                    });
                    agregadas++;
                }
            }

            await _context.SaveChangesAsync();
            return (true, $"Se agregaron {agregadas} tablas al catálogo.");
        }

        // ==========================================
        // CRUD CATÁLOGO DE TABLAS
        // ==========================================
        public async Task<(bool Success, int NewId)> AddTablaCatalogoAsync(int proyectoId, string nombreTabla, string tipoTabla, string? descripcion, string? rutaUbicacion)
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                return (false, 0);

            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null)
                return (false, 0);

            var tablaMaestra = await _context.TablasMaestras
                .FirstOrDefaultAsync(t => t.NombreTabla == nombreTabla.Trim() && t.ClienteId == proyecto.ClienteId);

            if (tablaMaestra == null)
            {
                tablaMaestra = new TablaMaestra
                {
                    NombreTabla = nombreTabla.Trim(),
                    Descripcion = descripcion?.Trim(),
                    ClienteId = proyecto.ClienteId
                };
                _context.TablasMaestras.Add(tablaMaestra);
                await _context.SaveChangesAsync();
            }

            var nuevaTabla = new TablaProyecto
            {
                ProyectoId = proyectoId,
                TablaMaestraId = tablaMaestra.Id,
                TipoTabla = tipoTabla,
                RutaUbicacion = rutaUbicacion?.Trim()
            };
            _context.TablasProyecto.Add(nuevaTabla);
            await _context.SaveChangesAsync();

            return (true, nuevaTabla.Id);
        }

        public async Task<bool> DeleteTablaCatalogoAsync(int tablaProyectoId)
        {
            var tabla = await _context.TablasProyecto.FindAsync(tablaProyectoId);
            if (tabla == null) return false;

            _context.TablasProyecto.Remove(tabla);
            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================
        // MÉTODOS PRIVADOS DE SOPORTE
        // ==========================================

        // Extrae todo el texto de código de un ZIP
        private static async Task<string> ExtraerContenidoZipAsync(byte[] zipBytes)
        {
            string content = "";
            using var ms = new MemoryStream(zipBytes);
            using var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read);

            foreach (var entry in archive.Entries.Where(e =>
                e.Name.EndsWith(".py") ||
                e.Name.EndsWith(".sql") ||
                e.Name.EndsWith(".ipynb") ||
                e.Name.EndsWith(".scala")))
            {
                using var reader = new StreamReader(entry.Open());
                content += await reader.ReadToEndAsync() + "\n";
            }

            // Normalizar concatenaciones de strings: "prefix" + var + "suffix" → ${var}
            content = Regex.Replace(content,
                @"[""']{1,3}\s*\+\s*([a-zA-Z0-9_]+)\s*\+\s*[""']{1,3}",
                "${$1}");

            return content;
        }

        // Extrae tablas origen y destino usando regex unificados (antes duplicados en Scan y Analyze)
        private static (HashSet<string> Sources, HashSet<string> Targets) ExtractTablesFromContent(string content)
        {
            var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Lecturas: FROM/JOIN, spark.read.table
            foreach (Match m in Regex.Matches(content, @"(?i)(?:FROM|JOIN)\s+([`a-zA-Z0-9_\.\{\}\$]+)"))
            {
                string t = m.Groups[1].Value.Replace("`", "").Trim();
                if (!t.Equals("SELECT", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(t))
                    sources.Add(t);
            }
            foreach (Match m in Regex.Matches(content, @"(?i)spark\.(?:read\.)?table\s*\(\s*f?[""']([^""']+)[""']\s*\)"))
                sources.Add(m.Groups[1].Value);

            // Escrituras: saveAsTable, insertInto, INSERT INTO, MERGE INTO, CREATE TABLE
            foreach (Match m in Regex.Matches(content, @"(?i)(?:saveAsTable|insertInto)\s*\(\s*f?[""']([^""']+)[""']\s*\)"))
                targets.Add(m.Groups[1].Value);
            foreach (Match m in Regex.Matches(content, @"(?i)(?:INSERT\s+(?:INTO|OVERWRITE)|MERGE\s+INTO|CREATE\s+(?:OR\s+REPLACE\s+)?TABLE\s+(?:IF\s+NOT\s+EXISTS)?)\s+([`a-zA-Z0-9_\.\{\}\$]+)"))
            {
                string t = m.Groups[1].Value.Replace("`", "").Trim();
                if (!string.IsNullOrWhiteSpace(t)) targets.Add(t);
            }

            // Filtrar temporales y basura
            sources = sources.Where(x => !IsTempOrJunk(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            targets = targets.Where(x => !IsTempOrJunk(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            return (sources, targets);
        }

        // Detecta si una tabla es temporal o basura — antes estaba duplicada en dos métodos
        private static bool IsTempOrJunk(string s)
        {
            string low = s.ToLower();
            return low.Contains("tmp")
                || low.Contains("temp")
                || low.Contains("vista")
                || low.StartsWith("v_")
                || low.StartsWith("vw_")
                || low.StartsWith("vt_")
                || low.Equals("dual")
                || low.Length <= 3
                || !Regex.IsMatch(low, "[a-z]")
                || !(s.Contains(".") || s.Contains("{") || s.Contains("$"));
        }

        // Agrupa lista de tablas por base de datos (prefijo antes del primer punto)
        private static Dictionary<string, List<string>> AgruparPorBaseDeDatos(List<string> tablas)
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

        private static void AgregarTablas(HashSet<string> lista, string tipo, List<object> resultado, HashSet<string> vistas)
        {
            foreach (var t in lista)
            {
                string nombreLimpio = t.Replace("{", "").Replace("}", "").Replace("$", "").Replace("`", "").Trim();
                // Key en minúsculas para evitar duplicados por diferencias de mayúsculas/minúsculas
                string key = $"{tipo}-{nombreLimpio.ToLower()}";
                if (!vistas.Contains(key))
                {
                    vistas.Add(key);
                    resultado.Add(new { nombreTabla = nombreLimpio, tipoTabla = tipo });
                }
            }
        }
    }
}
