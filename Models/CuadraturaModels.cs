using System.Collections.Generic;

namespace NotebookValidator.Web.Models
{
    public class ResultadoCuadratura
    {
        public string AliasArchivo1 { get; set; } = "Archivo 1";
        public string AliasArchivo2 { get; set; } = "Archivo 2";
        public int TotalCoincidenciasExactas { get; set; }
        public List<RegistroCuadrado> RegistrosCuadrados { get; set; } = new();
        public List<DiferenciaRegistro> RegistrosConDiferencias { get; set; } = new();
        public List<string> HuerfanosArchivo1 { get; set; } = new();
        public List<string> HuerfanosArchivo2 { get; set; } = new();
    }

    public class RegistroCuadrado
    {
        public string LlaveIdentificadora { get; set; }
        public string DetalleValores { get; set; }
    }

    public class DiferenciaRegistro
    {
        public string LlaveIdentificadora { get; set; }
        public string ColumnaConFalla { get; set; }
        public string ValorArchivo1 { get; set; }
        public string ValorArchivo2 { get; set; }
        public string Diferencia { get; set; }

        // NUEVO: Guardamos el valor numérico puro para poder hacer sumatorias y filtros dinámicos
        public double? DiferenciaNumerica { get; set; }
    }

    public class SugerenciaMapeo
    {
        public string ColumnaArchivo1 { get; set; }
        public string ColumnaArchivo2 { get; set; }
        public int PorcentajeSimilitud { get; set; }
    }

    public class MapeoColumnasViewModel
    {
        public string AliasArchivo1 { get; set; } = "Archivo 1";
        public string AliasArchivo2 { get; set; } = "Archivo 2";
        public List<string> ColumnasArchivo1 { get; set; } = new();
        public List<string> ColumnasArchivo2 { get; set; } = new();
        public List<SugerenciaMapeo> Sugerencias { get; set; } = new();
        public string TempPathArchivo1 { get; set; } = string.Empty;
        public string TempPathArchivo2 { get; set; } = string.Empty;
        public bool TieneEncabezados1 { get; set; }
        public bool TieneEncabezados2 { get; set; }
        public bool ModoAgrupacion { get; set; }
    }
}
