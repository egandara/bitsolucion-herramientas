using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using Microsoft.AspNetCore.Identity;
using NotebookValidator.Web.Data;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Mime;
using System.Text.Json; // Vital para la auditoría

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin, TempCleanerUser")]
    public class TempTableController : Controller
    {
        private readonly TempTableService _tempTableService;
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        // CONSTRUCTOR ACTUALIZADO CON INYECCIÓN
        public TempTableController(
            TempTableService tempTableService,
            AuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _tempTableService = tempTableService;
            _auditService = auditService;
            _userManager = userManager;
        }

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Generate(IFormFileCollection files, string platinum_temp_db, string db_location_platinum_temp)
        {
            if (files == null || files.Count == 0) return RedirectToAction("Index");

            try
            {
                string baseFileName = GenerateNotebookBaseName(files);
                string outputFileName = $"{baseFileName}.ipynb";

                // LLAMADA AL SERVICIO (Recibimos JSON y Lista de Tablas)
                var (generatedJson, tablesDetected) = await _tempTableService.GenerateDeletionNotebookAsync(
                    files, baseFileName, platinum_temp_db, db_location_platinum_temp);

                // --- LOG DE AUDITORÍA (LO QUE FALTABA) ---
                var userId = _userManager.GetUserId(User);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                var auditDetails = new
                {
                    Modulo = "Limpieza de Tablas Temporales",
                    Accion = "Detección y Generación de Notebook",
                    ArchivosSubidos = files.Select(f => f.FileName).ToList(),
                    TotalHallazgos = tablesDetected.Count,
                    TablasEncontradas = tablesDetected,
                    ParametrosConfig = new { Database = platinum_temp_db, Location = db_location_platinum_temp }
                };

                await _auditService.LogActionAsync(
                    userId,
                    "Limpieza Tmp: Generación Script",
                    JsonSerializer.Serialize(auditDetails),
                    ip);
                // --- FIN LOG ---

                Response.Headers.Add("Content-Disposition", new ContentDisposition
                {
                    FileName = outputFileName,
                    Inline = false
                }.ToString());

                return File(Encoding.UTF8.GetBytes(generatedJson), "application/json");
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private string GenerateNotebookBaseName(IFormFileCollection files)
        {
            int maxNum = 0;
            var regex = new Regex(@"^(BCI_?|BCI)(\d{1,3})_.*", RegexOptions.IgnoreCase);
            foreach (var file in files)
            {
                var match = regex.Match(file.FileName);
                if (match.Success && int.TryParse(match.Groups[2].Value, out int currentNum))
                    maxNum = Math.Max(maxNum, currentNum);
            }
            return $"BCI_{(maxNum + 1):D3}_Limpieza_Tablas_Temporales";
        }
    }
}
