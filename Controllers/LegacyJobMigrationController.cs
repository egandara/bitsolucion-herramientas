using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    public class LegacyJobMigrationController : Controller
    {
        private readonly LegacyJobMigrationService _migrationService;
        private readonly AuditService _auditService;

        public LegacyJobMigrationController(LegacyJobMigrationService migrationService, AuditService auditService)
        {
            _migrationService = migrationService;
            _auditService = auditService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessFiles(List<IFormFile> jsonFiles, string targetUser, string targetRepo)
        {
            if (jsonFiles == null || jsonFiles.Count == 0)
                return Json(new { success = false, message = "No se seleccionaron archivos." });

            var results = new List<object>();
            int successCount = 0;

            var jobsMigrados = new List<string>();

            foreach (var file in jsonFiles)
            {
                if (file.Length > 0 && Path.GetExtension(file.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(file.OpenReadStream());
                    var content = await reader.ReadToEndAsync();

                    try
                    {
                        var resultObj = _migrationService.TransformJsonToYaml(content, targetUser, targetRepo);
                        var yamlFileName = Path.GetFileNameWithoutExtension(file.FileName) + ".yml";

                        results.Add(new
                        {
                            success = true,
                            originalName = file.FileName,
                            newName = yamlFileName,
                            yaml = resultObj.YamlContent,
                            warnings = resultObj.Warnings
                        });

                        successCount++;
                        // Añadimos el NOMBRE OFICIAL DEL JOB a nuestra lista de auditoría, en vez del archivo
                        jobsMigrados.Add(resultObj.JobName);
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            success = false,
                            originalName = file.FileName,
                            error = ex.Message
                        });
                    }
                }
            }

            if (successCount > 0)
            {
                string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
                    string repoText = string.IsNullOrWhiteSpace(targetRepo) ? "Heredado del Original" : targetRepo;

                    var auditDetails = new
                    {
                        Operacion = "Migración por Ingeniería Inversa (JSON a YAML)",
                        Archivos_Exitosos = successCount,
                        Usuario_Destino = targetUser,
                        Repositorio_Destino = repoText,
                        Jobs_Migrados = jobsMigrados
                    };

                    string jsonDetails = JsonSerializer.Serialize(auditDetails);

                    await _auditService.LogActionAsync(userId, "MIGRACION_JOBS_LEGACY", jsonDetails, ip);
                }
            }

            return Json(new { success = true, files = results });
        }
    }
}
