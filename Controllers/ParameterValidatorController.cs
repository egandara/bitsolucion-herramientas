using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin, ParameterValidatorUser")]
    public class ParameterValidatorController : Controller
    {
        private readonly ParameterValidationService _service;
        public ParameterValidatorController(ParameterValidationService service) { _service = service; }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Validate(IFormFileCollection files)
        {
            if (files == null || files.Count == 0)
            {
                ViewBag.Error = "Por favor, selecciona al menos un archivo.";
                return View("Index");
            }
            var results = await _service.ValidateParametersAsync(files);
            return View("Index", results);
        }
    }
}