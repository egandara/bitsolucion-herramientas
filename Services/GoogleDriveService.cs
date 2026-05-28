using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Services
{
    public class GoogleDriveService
    {
        private readonly string[] Scopes = { DriveService.Scope.Drive };
        private readonly string ApplicationName = "Gestor de Proyectos";
        private readonly IWebHostEnvironment _env;

        public GoogleDriveService(IWebHostEnvironment env)
        {
            _env = env;
        }

        private async Task<DriveService> GetDriveServiceAsync()
        {
            UserCredential credential;
            string credentialsPath = Path.Combine(_env.ContentRootPath, "credentials.json");
            string tokenPath = Path.Combine(_env.ContentRootPath, "token.json");

            if (!File.Exists(credentialsPath))
                throw new FileNotFoundException("No se encontró el archivo credentials.json.");

            using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(tokenPath, true));
            }

            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        // ==============================================================================
        // NUEVO: Método inteligente que busca una carpeta. Si no existe, la crea.
        // ==============================================================================
        private async Task<string> FindOrCreateFolderAsync(DriveService service, string folderName, string parentId = "root")
        {
            // Limpiar comillas para evitar que la query se rompa
            string safeName = folderName.Replace("'", "\\'");

            var request = service.Files.List();
            request.Q = $"mimeType='application/vnd.google-apps.folder' and name='{safeName}' and '{parentId}' in parents and trashed=false";
            request.Spaces = "drive";
            request.Fields = "files(id, name)";

            var result = await request.ExecuteAsync();

            // Si encontró la carpeta, devolvemos su ID
            if (result.Files != null && result.Files.Count > 0)
            {
                return result.Files[0].Id;
            }

            // Si no existe, la creamos físicamente
            var folderMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { parentId }
            };

            var createRequest = service.Files.Create(folderMetadata);
            createRequest.Fields = "id";
            var folder = await createRequest.ExecuteAsync();

            return folder.Id;
        }

        public async Task<(string FolderId, string FolderUrl)> CreateFolderAsync(string folderName, string? parentFolderId = null)
        {
            var service = await GetDriveServiceAsync();

            var folderMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            if (!string.IsNullOrEmpty(parentFolderId))
                folderMetadata.Parents = new List<string> { parentFolderId };

            var request = service.Files.Create(folderMetadata);
            request.Fields = "id, webViewLink";

            var folder = await request.ExecuteAsync();
            return (folder.Id, folder.WebViewLink);
        }

        // ==============================================================================
        // MODIFICADO: Ahora recibe el nombre del cliente para armar la ruta anidada
        // ==============================================================================
        public async Task<(string RootFolderId, string RootFolderUrl)> CreateProjectStructureAsync(string projectName, string clientName)
        {
            var service = await GetDriveServiceAsync();

            // 1. Encuentra o crea la carpeta "Proyectos" en la raíz (Mi unidad)
            string masterFolderId = await FindOrCreateFolderAsync(service, "Proyectos", "root");

            // 2. Encuentra o crea la carpeta del Cliente (Ej: "BCI", "Falabella") dentro de "Proyectos"
            // Si el cliente no trae nombre, lo metemos a "Sin Cliente"
            string safeClientName = string.IsNullOrWhiteSpace(clientName) ? "Sin_Cliente" : clientName.Trim();
            string clientFolderId = await FindOrCreateFolderAsync(service, safeClientName, masterFolderId);

            // 3. Crea la carpeta raíz del proyecto DENTRO de la carpeta del cliente
            var rootFolder = await CreateFolderAsync(projectName, clientFolderId);

            // 4. Crear las subcarpetas dentro del proyecto
            var subfolders = new List<string> { "1_Diseño", "2_Desarrollo", "3_Pruebas", "4_Entregables" };
            foreach (var folder in subfolders)
            {
                await CreateFolderAsync(folder, rootFolder.FolderId);
            }

            return rootFolder;
        }
        // ====================================================================
        // NUEVO: SUBIR ARTEFACTO (ZIP) A UNA CARPETA ESPECÍFICA DE DRIVE
        // ====================================================================
        public async Task<string> UploadArtifactToFolderAsync(string folderId, string fileName, byte[] fileBytes, string mimeType = "application/zip")
        {
            try
            {
                // 1. Obtenemos el servicio autorizado
                var service = await GetDriveServiceAsync();

                // 2. Definimos los metadatos del archivo (Nombre y en qué carpeta va)
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = fileName,
                    Parents = new List<string> { folderId }
                };

                // 3. Convertimos los bytes en un Stream de memoria
                using var stream = new System.IO.MemoryStream(fileBytes);

                // 4. Preparamos y ejecutamos la subida
                var request = service.Files.Create(fileMetadata, stream, mimeType);
                request.Fields = "id, webViewLink"; // Pedimos que nos devuelva el link para la UI

                var uploadProgress = await request.UploadAsync();

                if (uploadProgress.Status == Google.Apis.Upload.UploadStatus.Failed)
                {
                    throw new Exception(uploadProgress.Exception?.Message ?? "Error desconocido al subir a Drive.");
                }

                // 5. Retornamos la URL pública (o interna) del archivo
                var file = request.ResponseBody;
                return file.WebViewLink;
            }
            catch (Exception ex)
            {
                // Registramos el error pero no bloqueamos la app, por si Drive se cae
                // queremos que el usuario igual pueda descargar el ZIP en su PC.
                Console.WriteLine($"Error subiendo a Drive: {ex.Message}");
                return string.Empty;
            }
        }

        // ====================================================================
        // NUEVO: MÉTODO PÚBLICO PARA BUSCAR O CREAR SUBCARPETAS
        // ====================================================================
        public async Task<string> GetOrCreateFolderAsync(string folderName, string parentId)
        {
            var service = await GetDriveServiceAsync();
            return await FindOrCreateFolderAsync(service, folderName, parentId);
        }
    }
}
