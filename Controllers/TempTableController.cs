using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
// 1. Añadir estas importaciones
using System.Text.RegularExpressions;
using System.Net.Mime;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin, TempCleanerUser")]
    public class TempTableController : Controller
    {
        private readonly TempTableService _tempTableService;

        public TempTableController(TempTableService tempTableService)
        {
            _tempTableService = tempTableService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        // 2. Recibir los nuevos parámetros del FormData
        public async Task<IActionResult> Generate(IFormFileCollection files, string platinum_temp_db, string db_location_platinum_temp)
        {
            if (files == null || files.Count == 0)
            {
                TempData["ToastMessage"] = "Por favor, selecciona al menos un archivo para analizar.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            try
            {
                // 3. Generar el nombre base del notebook
                string baseFileName = GenerateNotebookBaseName(files);
                string outputFileName = $"{baseFileName}.ipynb"; // Asegurarse que sea .ipynb

                // 4. Llamar al servicio con los nuevos parámetros
                var generatedNotebookJson = await _tempTableService.GenerateDeletionNotebookAsync(
                    files,
                    baseFileName,
                    platinum_temp_db,
                    db_location_platinum_temp
                );

                // 5. [CORRECCIÓN] Devolver el archivo con el header Content-Disposition correcto
                // Esto soluciona el error "filename_=UTF-8..."
                Response.Headers.Add("Content-Disposition", new ContentDisposition
                {
                    FileName = outputFileName,
                    Inline = false // Fuerza la descarga
                }.ToString());

                // Devolver como application/json (que es el formato de .ipynb)
                return File(Encoding.UTF8.GetBytes(generatedNotebookJson), "application/json");
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Ocurrió un error: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }
        }

        // 6. [NUEVO] Método helper para la lógica de nombrado
        private string GenerateNotebookBaseName(IFormFileCollection files)
        {
            int maxNum = 0;
            int paddingLength = 3; // Padding por defecto (ej. 001)
            string processName = "Limpieza_Tablas_Temporales"; // Nombre base

            // Regex para capturar (BCI_ o BCI)(001 o 01 o 1)(_)
            // ^(BCI_?|BCI) -> BCI_ o BCI al inicio
            // (\d{1,3}) -> Captura 1 a 3 dígitos (ej. 001, 01, 1)
            // _.* -> Seguido de _ y cualquier cosa
            var regex = new Regex(@"^(BCI_?|BCI)(\d{1,3})_.*", RegexOptions.IgnoreCase);

            foreach (var file in files)
            {
                if (file.FileName == null) continue;

                var match = regex.Match(file.FileName);
                if (match.Success)
                {
                    string numStr = match.Groups[2].Value; // El número (ej. "001")
                    if (int.TryParse(numStr, out int currentNum))
                    {
                        if (currentNum > maxNum)
                        {
                            maxNum = currentNum;
                            paddingLength = numStr.Length; // Captura el padding (ej. 3 para "001")
                        }
                    }
                }
            }

            int newNum = maxNum + 1;
            string newNumStr = newNum.ToString($"D{paddingLength}"); // Aplica padding (ej. 5 -> "005")

            // Retorna solo el nombre base, sin extensión
            return $"BCI_{newNumStr}_{processName}";
        }
    }
}