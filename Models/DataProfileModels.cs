using System.Collections.Generic;

namespace NotebookValidator.Web.Models
{
    public class DataProfileResult
    {
        public string FileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }
        public List<ColumnProfile> ColumnProfiles { get; set; } = new();

        // Nuevo: para permitir búsquedas posteriores de duplicados
        public string TempFileName { get; set; } = string.Empty;
        public bool HasHeaders { get; set; }

        // Resultado de duplicados (opcional, rellenado por la acción FindDuplicates)
        public List<DuplicateGroup> DuplicateGroups { get; set; } = new();
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

        // --- NUEVO: métricas para validación de "Texto" ---
        public int DetectedIntegerCount { get; set; }
        public int DetectedDecimalCount { get; set; }
        public int DetectedDateCount { get; set; }
        public int DetectedTextCount { get; set; }

        public int DetectedNumericCount => DetectedIntegerCount + DetectedDecimalCount;
        public double DetectedNumericPercentage { get; set; }

        // Muestra de filas que no coincidirían con un tipo objetivo (limitada)
        public List<MismatchRow> MismatchRows { get; set; } = new();
    }

    public class TopValueItem
    {
        public string Value { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    // Nuevo: Representa un grupo de filas duplicadas
    public class DuplicateGroup
    {
        // Clave concatenada usada para agrupar (cadena legible)
        public string Key { get; set; } = string.Empty;

        // Número de filas en el grupo
        public int Count { get; set; }

        // Índices de fila (1-based) en el archivo analizado
        public List<int> RowIndices { get; set; } = new();

        // Muestra de valores por columna para este grupo (clave legible)
        public string SampleDisplay { get; set; } = string.Empty;
    }

    // Nuevo: fila en mismatch
    public class MismatchRow
    {
        public int RowIndex { get; set; } // 1-based
        public string Value { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    // Resultado de la validación de una columna Texto
    public class ColumnValidationResult
    {
        public string ColumnName { get; set; } = string.Empty;
        public int TotalSampled { get; set; }

        public int IntegerCount { get; set; }
        public int DecimalCount { get; set; }
        public int DateCount { get; set; }
        public int TextCount { get; set; }

        public double IntegerPercentage { get; set; }
        public double DecimalPercentage { get; set; }
        public double DatePercentage { get; set; }
        public double TextPercentage { get; set; }

        public bool IsLikelyNumeric { get; set; }
        public string Recommendation { get; set; } = string.Empty;

        public List<MismatchRow> MismatchRows { get; set; } = new();

        // Nuevo: muestra de valores crudos para diagnóstico
        public List<string> SampleValues { get; set; } = new();
    }
}
