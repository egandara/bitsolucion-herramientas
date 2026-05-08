using System.Collections.Generic;

namespace NotebookValidator.Web.Models
{
    public class DataProfileResult
    {
        public string FileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }
        public List<ColumnProfile> ColumnProfiles { get; set; } = new();
    }

    public class ColumnProfile
    {
        public string ColumnName { get; set; } = string.Empty;
        public string InferredDataType { get; set; } = "Texto";

        public int NullOrEmptyCount { get; set; }
        public double NullPercentage { get; set; }

        public int UniqueValuesCount { get; set; }
        public double UniquePercentage { get; set; }

        public string MinValue { get; set; } = "N/A";
        public string MaxValue { get; set; } = "N/A";
        public double? Average { get; set; } = null;

        public string HealthStatus { get; set; } = "Óptima";

        public int ZeroCount { get; set; }
        public int NegativeCount { get; set; }
        public int WhitespaceCount { get; set; }

        // --- NUEVO: Detección de PII (Datos Sensibles) ---
        public bool IsPII { get; set; }
        public string PIIType { get; set; } = string.Empty;

        public List<TopValueItem> TopValues { get; set; } = new();
    }

    public class TopValueItem
    {
        public string Value { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}
