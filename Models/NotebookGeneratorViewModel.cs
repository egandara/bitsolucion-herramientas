using System.Collections.Generic;

namespace NotebookValidator.Web.Models
{
    public class NotebookGeneratorViewModel
    {
        public string Titulo { get; set; } = string.Empty;
        public string EmailJefeProyecto { get; set; } = string.Empty;
        public string EmailAnalista { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty; // <-- Usaremos "Descripcion"

        public List<WidgetInput> Parametros { get; set; } = new List<WidgetInput>();

        public bool IncluirSqlSafe { get; set; } = true;
        public bool IncluirFuncionesFechas { get; set; } = true;

        public string CodigoUsuario { get; set; } = string.Empty;
    }

    public class WidgetInput
    {
        public string NombreWidget { get; set; } = string.Empty;
        public string ValorPorDefecto { get; set; } = string.Empty;
        public string Etiqueta { get; set; } = string.Empty;
    }
}
