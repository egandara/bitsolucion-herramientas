namespace NotebookValidator.Web.ViewModels.GestorProyectos
{
    public class SaveScannedLineageRequest
    {
        public int ProyectoId { get; set; }
        public List<TablaScaneada> Tablas { get; set; } = new();
    }

    public class TablaScaneada
    {
        public string NombreTabla { get; set; } = string.Empty;
        public string TipoTabla { get; set; } = string.Empty;
    }

    public class NuevoComentarioDto
    {
        public int ProyectoId { get; set; }
        public string Texto { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public DateTime? FechaVencimiento { get; set; }
    }

    public class SearchResultDto
    {
        public string Categoria { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
    }
}
