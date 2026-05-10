using System.Collections.Generic;

namespace NotebookValidator.Web.Models.Cuadratura
{
    public class PreviewResponse
    {
        public List<string> Columns { get; set; } = new();
        public List<Dictionary<string, object>> Rows { get; set; } = new();
    }
}
