using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class StandardsController : Controller
    {
        private readonly IWebHostEnvironment _hostEnvironment;

        public StandardsController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        // --- Acciones para "Documento de Estándares" ---
        public IActionResult Document()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetStandardsAsPdf()
        {
            var docxPath = Path.Combine(_hostEnvironment.WebRootPath, "standards", "Documento_Estandares.docx");
            return ConvertDocxToPdf(docxPath);
        }

        [HttpGet]
        public IActionResult DownloadStandardsAsDocx()
        {
            var docxPath = Path.Combine(_hostEnvironment.WebRootPath, "standards", "Documento_Estandares.docx");
            return DownloadDocx(docxPath, "Documento_Estandares_Bitsolucion.docx");
        }

        // --- Acciones para "Formato de Documentos" ---
        public IActionResult Format()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetFormatAsPdf()
        {
            var docxPath = Path.Combine(_hostEnvironment.WebRootPath, "standards", "Formato_Documentos.docx");
            return ConvertDocxToPdf(docxPath);
        }

        [HttpGet]
        public IActionResult DownloadFormatAsDocx()
        {
            var docxPath = Path.Combine(_hostEnvironment.WebRootPath, "standards", "Formato_Documentos.docx");
            return DownloadDocx(docxPath, "Formato_Documentos_Bitsolucion.docx");
        }

        // --- NUEVAS ACCIONES PARA "DOCUMENTOS JOBS" ---
        public IActionResult Jobs()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetJobsAsPdf()
        {
            var docxPath = Path.Combine(_hostEnvironment.WebRootPath, "standards", "Documentos_Jobs.docx");
            return ConvertDocxToPdf(docxPath);
        }

        [HttpGet]
        public IActionResult DownloadJobsAsDocx()
        {
            var docxPath = Path.Combine(_hostEnvironment.WebRootPath, "standards", "Documentos_Jobs.docx");
            return DownloadDocx(docxPath, "Documentos_Jobs_Bitsolucion.docx");
        }

        // --- Métodos Ayudantes Reutilizables ---
        private IActionResult ConvertDocxToPdf(string docxPath)
        {
            if (!System.IO.File.Exists(docxPath)) return NotFound($"El archivo no se encuentra en: {docxPath}");

            using WordDocument document = new WordDocument(docxPath);
            using DocIORenderer renderer = new DocIORenderer();
            using PdfDocument pdfDocument = renderer.ConvertToPDF(document);

            using MemoryStream memoryStream = new MemoryStream();
            pdfDocument.Save(memoryStream);
            memoryStream.Position = 0;

            return File(memoryStream.ToArray(), "application/pdf");
        }

        private IActionResult DownloadDocx(string docxPath, string downloadFileName)
        {
            if (!System.IO.File.Exists(docxPath)) return NotFound();

            var memory = new MemoryStream();
            using (var stream = new FileStream(docxPath, FileMode.Open))
            {
                stream.CopyTo(memory);
            }
            memory.Position = 0;

            return File(memory, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", downloadFileName);
        }
    }
}