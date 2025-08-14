using Microsoft.AspNetCore.Http;

namespace NotebookValidator.Web.ViewModels
{
    public class DocumentationViewModel
    {
        public IFormFile NotebookFile { get; set; }
        public string GeneratedMarkdown { get; set; }
        public string OriginalFileName { get; set; }
    }
}