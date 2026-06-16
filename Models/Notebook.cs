using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace NotebookValidator.Web.Models
{
    public class Notebook
    {
        [JsonPropertyName("nbformat")]
        public int Nbformat { get; set; } = 4;

        [JsonPropertyName("nbformat_minor")]
        public int NbformatMinor { get; set; } = 5;

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new()
        {
            { "language_info", new { name = "python" } }
        };

        [JsonPropertyName("cells")]
        public List<Cell> Cells { get; set; } = new();
    }

    public class Cell
    {
        [JsonPropertyName("cell_type")]
        public string CellType { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public List<string> Source { get; set; } = new();

        [JsonPropertyName("outputs")]
        public List<object> Outputs { get; set; } = new();

        [JsonPropertyName("execution_count")]
        public int? ExecutionCount { get; set; } = null;

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
