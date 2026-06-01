using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models.GestorProyectos;
using NotebookValidator.Web.Services;
using System.Text.RegularExpressions;

namespace NotebookValidator.Web.Services.GestorProyectos
{
    public class JobGenerationService
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleDriveService _driveService;
        private readonly WorkspaceService _workspaceService;
        private readonly JobTransformationService _transformationService;

        public JobGenerationService(
            ApplicationDbContext context,
            GoogleDriveService driveService,
            WorkspaceService workspaceService,
            JobTransformationService transformationService)
        {
            _context = context;
            _driveService = driveService;
            _workspaceService = workspaceService;
            _transformationService = transformationService;
        }

        // ==========================================
        // GENERAR BUNDLE Y SUBIR A DRIVE
        // ==========================================
        public async Task<(bool Success, string Message, string DriveUrl, string DownloadToken, string FileName)> GenerateAndUploadJobAsync(
            int proyectoId, string bundleName, string permLevel, string permUser, bool autocert, Stream yamlStream, string yamlFileName)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.Fases)
                .Include(p => p.TablasCatalogo).ThenInclude(tc => tc.TablaMaestra)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == proyectoId);

            if (proyecto == null || string.IsNullOrEmpty(proyecto.RutaWorkspaceLocal))
                return (false, "Proyecto no encontrado o Workspace vacío.", "", "", "");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                byte[] zipBytes = _workspaceService.GetWorkspaceFile(proyecto.RutaWorkspaceLocal);

                // Leer y limpiar nombre del job desde el YAML
                string yamlContent;
                using (var reader = new StreamReader(yamlStream))
                    yamlContent = await reader.ReadToEndAsync();

                var match = Regex.Match(yamlContent, @"name:\s*[""']?([^""'\n]+)[""']?");
                string originalJobName = match.Success ? match.Groups[1].Value : Path.GetFileNameWithoutExtension(yamlFileName);
                string cleanJobName = Regex.Replace(originalJobName, @"^\[dev\]|^\[DEV\]|^dev_|^dev-", "", RegexOptions.IgnoreCase).Trim();

                // Construir strings de linaje desde el catálogo
                var origins = proyecto.TablasCatalogo
                    .Where(t => t.TipoTabla == "Origen" && t.TablaMaestra != null)
                    .Select(t => t.TablaMaestra!.NombreTabla).ToList();
                var targets = proyecto.TablasCatalogo
                    .Where(t => t.TipoTabla == "Salida" && t.TablaMaestra != null)
                    .Select(t => t.TablaMaestra!.NombreTabla).ToList();

                string sourceStr = string.Join(";", origins);
                string targetStr = string.Join(";", targets);

                // Generar configuraciones de entorno (dev/cert/prod)
                var generatedFiles = _transformationService.GenerateBundleConfigs(
                    new List<string> { yamlContent },
                    new List<string> { cleanJobName }, new List<string> { cleanJobName }, new List<string> { cleanJobName },
                    new List<string> { permLevel }, new List<string> { permUser },
                    new List<bool> { autocert }, new List<bool> { autocert }, new List<bool> { autocert },
                    new List<string> { sourceStr }, new List<string> { targetStr },
                    bundleName
                );

                // Empaquetar configs + notebooks en un ZIP final
                byte[] finalZipBytes = EnsamblarZipFinal(generatedFiles, zipBytes);
                string fileName = $"Artefactos_{bundleName}_{DateTime.Now:yyyyMMdd_HHmm}.zip";

                // Subir a la carpeta 4_Entregables de Drive
                string driveUrl = "";
                if (!string.IsNullOrEmpty(proyecto.DriveFolderId))
                {
                    string entregablesFolderId = await _driveService.GetOrCreateFolderAsync("4_Entregables", proyecto.DriveFolderId);
                    string repoFolderId = await _driveService.GetOrCreateFolderAsync(bundleName, entregablesFolderId);
                    driveUrl = await _driveService.UploadArtifactToFolderAsync(repoFolderId, fileName, finalZipBytes);
                }

                // Registrar artefacto y marcar fase Paso a Producción como completada
                _context.ArtefactosJob.Add(new ArtefactoJob
                {
                    ProyectoId = proyecto.Id,
                    UsuarioGenerador = permUser,
                    NombreBundle = bundleName,
                    FechaGeneracion = DateTime.Now,
                    ArchivoDriveUrl = driveUrl
                });

                var faseProduccion = proyecto.Fases.FirstOrDefault(f => f.NombreFase.Contains("Paso_A_Produccion"));
                if (faseProduccion != null)
                {
                    faseProduccion.EstadoFase = "Completado";
                    faseProduccion.FechaActualizacion = DateTime.Now;
                    faseProduccion.UsuarioActualizacion = permUser;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Guardar en temp para descarga inmediata
                string tempToken = Guid.NewGuid().ToString("N");
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), tempToken + ".zip"), finalZipBytes);

                return (true, string.Empty, driveUrl, tempToken, fileName);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "Error en el motor de transformación: " + ex.Message, "", "", "");
            }
        }

        // ==========================================
        // DESCARGA DE ARTEFACTO DESDE TEMP
        // ==========================================
        public (bool Exists, byte[] Bytes) GetArtifactForDownload(string token)
        {
            string path = Path.Combine(Path.GetTempPath(), token + ".zip");
            if (!File.Exists(path)) return (false, Array.Empty<byte>());

            var bytes = File.ReadAllBytes(path);
            File.Delete(path);
            return (true, bytes);
        }

        // ==========================================
        // MÉTODO PRIVADO: ENSAMBLAR ZIP FINAL
        // ==========================================
        private static byte[] EnsamblarZipFinal(Dictionary<string, string> generatedFiles, byte[] workspaceZipBytes)
        {
            using var ms = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
            {
                // Agregar archivos de configuración generados
                foreach (var gf in generatedFiles)
                {
                    var entry = archive.CreateEntry(gf.Key);
                    using var writer = new StreamWriter(entry.Open());
                    writer.Write(gf.Value);
                }

                // Agregar notebooks del workspace bajo la carpeta "notebooks/"
                using var msZip = new MemoryStream(workspaceZipBytes);
                using var sourceArchive = new System.IO.Compression.ZipArchive(msZip, System.IO.Compression.ZipArchiveMode.Read);
                foreach (var entry in sourceArchive.Entries)
                {
                    var newEntry = archive.CreateEntry("notebooks/" + entry.FullName);
                    using var sourceStream = entry.Open();
                    using var destStream = newEntry.Open();
                    sourceStream.CopyTo(destStream);
                }
            }
            return ms.ToArray();
        }
    }
}
