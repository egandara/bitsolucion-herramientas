using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    public class JobConverterController : Controller
    {
        private readonly JobTransformationService _transformationService;

        public JobConverterController(JobTransformationService transformationService)
        {
            _transformationService = transformationService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Convert(IFormFile jsonFile, string certSparkConf, string prodSparkConf)
        {
            // 1. Validar que se haya subido un archivo
            if (jsonFile == null || jsonFile.Length == 0)
            {
                ModelState.AddModelError("", "Por favor sube un archivo JSON válido.");
                return View("Index");
            }

            try
            {
                // 2. Leer el contenido del JSON subido
                string jsonContent;
                using (var reader = new StreamReader(jsonFile.OpenReadStream()))
                {
                    jsonContent = await reader.ReadToEndAsync();
                }

                // 3. Llamar al servicio de transformación pasando el JSON y las configs de Spark opcionales
                var results = _transformationService.GenerateEnvironmentConfigs(jsonContent, certSparkConf, prodSparkConf);

                // 4. Crear el archivo ZIP en memoria con los resultados
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var entry in results)
                        {
                            // entry.Key es el nombre del archivo (ej: Produccion.json)
                            // entry.Value es el contenido del JSON
                            var zipEntry = archive.CreateEntry(entry.Key);

                            using (var entryStream = zipEntry.Open())
                            using (var streamWriter = new StreamWriter(entryStream))
                            {
                                await streamWriter.WriteAsync(entry.Value);
                            }
                        }
                    }

                    // 5. Retornar el archivo ZIP para descarga
                    // Usamos .ToArray() para asegurar que el stream se cierra correctamente antes de enviar
                    return File(memoryStream.ToArray(), "application/zip", "JobConfigs_Generados.zip");
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores: mostramos el mensaje en la vista para que el usuario sepa qué pasó
                ModelState.AddModelError("", $"Ocurrió un error al procesar el archivo: {ex.Message}");
                return View("Index");
            }
        }
    }
}