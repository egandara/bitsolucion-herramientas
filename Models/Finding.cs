using System.Text.Json.Serialization;

namespace NotebookValidator.Web.Models
{
    public class Finding
    {
        public string FileName { get; set; } = string.Empty;
        public string FindingType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;

        // Mapeo directo para el JSON generado por ASP.NET Core
        [JsonPropertyName("cellNumber")]
        public int? CellNumber { get; set; }

        [JsonPropertyName("lineNumber")]
        public int? LineNumber { get; set; }

        public string? Content { get; set; }
        public string? CellSourceCode { get; set; }
        public string Severity { get; set; } = "Warning";

        // Constructor vacío para que el sistema pueda procesar los datos
        public Finding() { }

        // Constructor principal para el Servicio
        public Finding(string fileName, string findingType, string details, int? cellNumber = null, int? lineNumber = null, string? content = null, string? cellSourceCode = null, string severity = "Warning")
        {
            FileName = fileName;
            FindingType = findingType;
            Details = details;
            CellNumber = cellNumber;
            LineNumber = lineNumber;
            Content = content;
            CellSourceCode = cellSourceCode;
            Severity = severity;
        }
    }
}
