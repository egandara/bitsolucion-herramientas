using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NotebookValidator.Web.Data;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Services
{
    public class WorkspaceService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IServiceScopeFactory _scopeFactory;

        public WorkspaceService(IWebHostEnvironment env, IServiceScopeFactory scopeFactory)
        {
            _env = env;
            _scopeFactory = scopeFactory;
        }

        // ====================================================================
        // 1. GUARDAR LOCAL, EXTRAER Y DISPARAR SINCRONIZACIÓN FANTASMA
        // ====================================================================
        public async Task<string> SaveWorkspaceLocalAsync(int proyectoId, IFormFile file, string repoName, string rootDriveFolderId)
        {
            if (file == null || file.Length == 0) throw new ArgumentException("El archivo proporcionado está vacío.");

            // 1. Rutas
            string baseFolder = Path.Combine(_env.ContentRootPath, "WorkspaceLocal", $"PRJ_{proyectoId:D3}");
            string extractFolder = Path.Combine(baseFolder, "source_code");

            if (!Directory.Exists(baseFolder)) Directory.CreateDirectory(baseFolder);

            // 2. Limpieza de versiones anteriores (borramos ZIP y carpetas viejas)
            var dirInfo = new DirectoryInfo(baseFolder);
            foreach (var f in dirInfo.GetFiles()) { try { f.Delete(); } catch { } }
            foreach (var d in dirInfo.GetDirectories()) { try { d.Delete(true); } catch { } }

            // 3. Guardar el nuevo ZIP a máxima velocidad
            string fileName = $"codigo_fuente_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            string filePath = Path.Combine(baseFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }

            // 4. EXTRAER CONTENIDO PARA CREAR EL "SHADOW REPO" LOCAL DE FORMA SEGURA E INTELIGENTE
            Directory.CreateDirectory(extractFolder);
            using (var archive = ZipFile.OpenRead(filePath))
            {
                foreach (var entry in archive.Entries)
                {
                    // Ignorar entradas que son solo directorios (a veces vienen malformados desde Mac/Linux)
                    if (string.IsNullOrEmpty(entry.Name) || entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                    {
                        continue; // Los directorios se crearán automáticamente al extraer los archivos
                    }

                    // Calcular ruta final del archivo
                    string destinationPath = Path.GetFullPath(Path.Combine(extractFolder, entry.FullName));

                    // Seguridad: Prevenir vulnerabilidad "Zip Slip" (archivos maliciosos que intentan salir de la carpeta)
                    if (!destinationPath.StartsWith(Path.GetFullPath(extractFolder), StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Crear el directorio padre del archivo si no existe
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                    // Extraer archivo limpiamente
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }

            // 5. DISPARAR LA SINCRONIZACIÓN A GOOGLE DRIVE EN SEGUNDO PLANO
            if (!string.IsNullOrEmpty(rootDriveFolderId) && !string.IsNullOrEmpty(repoName))
            {
                _ = Task.Run(() => SyncWorkspaceToDriveAsync(extractFolder, repoName, rootDriveFolderId, proyectoId)); // <-- Agregamos proyectoId aquí
            }

            // Retornamos la ruta física absoluta del ZIP para que QA lo use rápido
            return filePath;
        }

        // ====================================================================
        // 2. LEER EL CÓDIGO DESDE EL DISCO (Para Validar, Linaje, Jobs)
        // ====================================================================
        public byte[] GetWorkspaceFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("El archivo del Workspace no existe o fue eliminado.");
            return File.ReadAllBytes(filePath);
        }

        // ====================================================================
        // 3. EL CLONADOR FANTASMA (PROCESO EN SEGUNDO PLANO)
        // ====================================================================
        private async Task SyncWorkspaceToDriveAsync(string extractFolder, string repoName, string parentDriveId, int proyectoId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var driveService = scope.ServiceProvider.GetRequiredService<GoogleDriveService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // Accedemos a la BD en segundo plano

                try
                {
                    // a) Navegar o Crear "4_Entregables"
                    string entregablesId = await driveService.GetOrCreateFolderAsync("4_Entregables", parentDriveId);

                    // b) Navegar o Crear la carpeta del Repositorio
                    string repoFolderId = await driveService.GetOrCreateFolderAsync(repoName, entregablesId);

                    // c) Sincronizar el árbol de carpetas recursivamente
                    await SyncDirectoryRecursiveAsync(driveService, extractFolder, repoFolderId);

                    // d) ¡ÉXITO! Actualizar la base de datos
                    var proyecto = await context.Proyectos.FindAsync(proyectoId);
                    if (proyecto != null)
                    {
                        proyecto.EstadoSincronizacionDrive = "Sincronizado";
                        proyecto.FechaSincronizacionDrive = DateTime.Now;
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    // e) ERROR: Avisar en la base de datos si falló la red
                    var proyecto = await context.Proyectos.FindAsync(proyectoId);
                    if (proyecto != null)
                    {
                        proyecto.EstadoSincronizacionDrive = "Error";
                        await context.SaveChangesAsync();
                    }
                    Console.WriteLine($"[CRÍTICO] Error en clonador de Drive: {ex.Message}");
                }
            }
        }

        private async Task SyncDirectoryRecursiveAsync(GoogleDriveService driveService, string localPath, string driveParentId)
        {
            // 1. Subir todos los archivos sueltos de este nivel
            foreach (var file in Directory.GetFiles(localPath))
            {
                byte[] fileBytes = await File.ReadAllBytesAsync(file);
                string fileName = Path.GetFileName(file);
                await driveService.UploadArtifactToFolderAsync(driveParentId, fileName, fileBytes);
            }

            // 2. Entrar a cada subcarpeta y hacer el mismo proceso mágico
            foreach (var dir in Directory.GetDirectories(localPath))
            {
                string dirName = new DirectoryInfo(dir).Name;
                string newDriveFolderId = await driveService.GetOrCreateFolderAsync(dirName, driveParentId);

                await SyncDirectoryRecursiveAsync(driveService, dir, newDriveFolderId);
            }
        }
    }
}
