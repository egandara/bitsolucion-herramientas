using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.Services;
using System.Linq;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class NotebookGeneratorController : Controller
    {
        private readonly NotebookBuilderService _builderService;
        private readonly ApplicationDbContext _context;

        public NotebookGeneratorController(NotebookBuilderService builderService, ApplicationDbContext context)
        {
            _builderService = builderService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var allowedParams = await _context.AllowedParameters.ToListAsync();
            ViewBag.ParametersJson = System.Text.Json.JsonSerializer.Serialize(allowedParams);
            return View(new NotebookGeneratorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Generar(NotebookGeneratorViewModel modelo)
        {
            var fileBytes = _builderService.GenerarNotebook(modelo);

            string safeTitle = string.IsNullOrWhiteSpace(modelo.Titulo) ? "Nuevo_Notebook" : modelo.Titulo;
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                safeTitle = safeTitle.Replace(c, '_');
            }

            // Descargamos como .py (Databricks lo interpreta como notebook al importar)
            string fileName = $"{safeTitle.Replace(" ", "_")}.py";

            return File(fileBytes, "text/x-python", fileName);
        }
    }
}
