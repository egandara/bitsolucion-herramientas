using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TestAIController : Controller
    {
        private readonly TestAIService _testAIService;

        public TestAIController(TestAIService testAIService)
        {
            _testAIService = testAIService;
        }

        [HttpGet]
        public IActionResult Index(string apiResponse = null)
        {
            ViewBag.ApiResponse = apiResponse;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestPrompt(string prompt)
        {
            string responseMessage;
            try
            {
                responseMessage = await _testAIService.SendPromptAsync(prompt);
                TempData["ToastMessage"] = "Conexión con la API de IA exitosa.";
                TempData["ToastType"] = "success";
            }
            catch (Exception ex)
            {
                responseMessage = $"ERROR: {ex.Message}\n\nDetalles: {ex.InnerException?.Message}";
                TempData["ToastMessage"] = "Falló la conexión con la API de IA.";
                TempData["ToastType"] = "error";
            }
            return RedirectToAction("Index", new { apiResponse = responseMessage });
        }
    }
}