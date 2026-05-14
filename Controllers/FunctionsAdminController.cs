using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FunctionsAdminController : Controller
    {
        private readonly FunctionsService _functionsService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService;

        public FunctionsAdminController(FunctionsService functionsService, UserManager<ApplicationUser> userManager, AuditService auditService)
        {
            _functionsService = functionsService;
            _userManager = userManager;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var notebook = await _functionsService.GetMasterNotebookAsync();
            var functions = notebook.Cells.Where(c => c.CellType == "code").ToList();
            ViewBag.FunctionsService = _functionsService;
            return View("~/Views/FunctionsAdmin/Index.cshtml", functions);
        }

        [HttpPost]
        public async Task<IActionResult> AddFunction(string sourceCode)
        {
            var user = await _userManager.GetUserAsync(User);
            var notebook = await _functionsService.GetMasterNotebookAsync();

            var lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                                  .Select(l => l + "\n").ToList();

            notebook.Cells.Add(new Cell { CellType = "code", Source = lines });
            await _functionsService.SaveMasterWithBackupAsync(notebook, user.UserName);

            await _auditService.LogActionAsync(user.Id, "MANTENEDOR FUNCIONES: AGREGAR", JsonSerializer.Serialize(new { Accion = "Nueva función agregada" }), HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = "Función agregada correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFunction(int cellIndex, string sourceCode)
        {
            var user = await _userManager.GetUserAsync(User);
            var notebook = await _functionsService.GetMasterNotebookAsync();

            var codeCells = notebook.Cells.Where(c => c.CellType == "code").ToList();
            if (cellIndex >= 0 && cellIndex < codeCells.Count)
            {
                var targetCell = codeCells[cellIndex];
                var realIndex = notebook.Cells.IndexOf(targetCell);

                var lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                                      .Select(l => l + "\n").ToList();

                notebook.Cells[realIndex].Source = lines;
                await _functionsService.SaveMasterWithBackupAsync(notebook, user.UserName);

                await _auditService.LogActionAsync(user.Id, "MANTENEDOR FUNCIONES: MODIFICAR", JsonSerializer.Serialize(new { Accion = "Función modificada", Indice = cellIndex }), HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["SuccessMessage"] = "Función actualizada correctamente.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFunction(int cellIndex)
        {
            var user = await _userManager.GetUserAsync(User);
            var notebook = await _functionsService.GetMasterNotebookAsync();

            var codeCells = notebook.Cells.Where(c => c.CellType == "code").ToList();
            if (cellIndex >= 0 && cellIndex < codeCells.Count)
            {
                var targetCell = codeCells[cellIndex];
                var funcName = _functionsService.GetFunctionName(targetCell);
                notebook.Cells.Remove(targetCell);
                await _functionsService.SaveMasterWithBackupAsync(notebook, user.UserName);

                await _auditService.LogActionAsync(user.Id, "MANTENEDOR FUNCIONES: ELIMINAR", JsonSerializer.Serialize(new { Accion = "Función eliminada", Nombre = funcName }), HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["SuccessMessage"] = $"Función '{funcName}' eliminada correctamente.";
            }
            return RedirectToAction("Index");
        }

        // --- NUEVA ACCIÓN: DESCARGAR ARCHIVO MAESTRO ---
        [HttpGet]
        public async Task<IActionResult> DownloadMaster([FromServices] IWebHostEnvironment env)
        {
            var user = await _userManager.GetUserAsync(User);
            var masterPath = Path.Combine(env.WebRootPath, "standards", "Funciones.ipynb");

            if (!System.IO.File.Exists(masterPath))
            {
                TempData["ErrorMessage"] = "El archivo maestro de funciones aún no está disponible.";
                return RedirectToAction("Index");
            }

            await _auditService.LogActionAsync(user.Id, "MANTENEDOR FUNCIONES: DESCARGA", JsonSerializer.Serialize(new { Accion = "Descarga del archivo maestro" }), HttpContext.Connection.RemoteIpAddress?.ToString());

            return PhysicalFile(masterPath, "application/octet-stream", "Funciones.ipynb");
        }
    }
}
