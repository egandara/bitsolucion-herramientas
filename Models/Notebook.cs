using System.Text.Json.Serialization;

public class Notebook
{
    [JsonPropertyName("cells")]
    public List<Cell> Cells { get; set; } = new();
}

public class Cell
{
    [JsonPropertyName("cell_type")]
    public string CellType { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public List<string> Source { get; set; } = new();
}