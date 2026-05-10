using System;
using System.Text;
using NotebookValidator.Web.Models;

namespace NotebookValidator.Web.Services
{
    public class NotebookBuilderService
    {
        public byte[] GenerarNotebook(NotebookGeneratorViewModel modelo)
        {
            var sb = new StringBuilder();
            int sectionCounter = 1;
            string fechaActual = DateTime.Now.ToString("dd/MM/yyyy");

            // 1. Cabecera técnica indispensable para que Databricks lo reconozca como Notebook
            sb.AppendLine("# Databricks notebook source");

            // 2. Título Principal
            sb.AppendLine("# MAGIC %md");
            sb.AppendLine($"# MAGIC # {modelo.Titulo}");
            sb.AppendLine("# COMMAND ----------\n");

            // 3. Información del Notebook
            sb.AppendLine("# MAGIC %md");
            sb.AppendLine("# MAGIC ## Información del Notebook");
            sb.AppendLine("# COMMAND ----------\n");

            // 4. Encabezado Detallado (con asteriscos)
            sb.AppendLine("# MAGIC %md");
            sb.AppendLine("# MAGIC ### Encabezado");
            sb.AppendLine("# MAGIC **************************************************************************");
            sb.AppendLine($"# MAGIC * Nombre: {modelo.Titulo}.ipynb");
            sb.AppendLine("# MAGIC * Ruta: ");
            sb.AppendLine("# MAGIC * Autor: BitSolucion Spa");
            sb.AppendLine($"# MAGIC * Ing. SW BCI: Jefe Proyecto <{modelo.EmailJefeProyecto}> - Analista Banco <{modelo.EmailAnalista}>");
            sb.AppendLine($"# MAGIC * Fecha: {fechaActual}");
            sb.AppendLine($"# MAGIC * Descripción: {modelo.Descripcion}");
            sb.AppendLine("# MAGIC **************************************************************************");
            sb.AppendLine("# COMMAND ----------\n");

            // 5. Mantenciones
            sb.AppendLine("# MAGIC %md");
            sb.AppendLine("# MAGIC ### Mantenciones");
            sb.AppendLine("# MAGIC | ID | FECHA | AUTOR | ACCION |");
            sb.AppendLine("# MAGIC |---|---|---|---|");
            sb.AppendLine($"# MAGIC | 1.0 | {fechaActual} | BitSolucion Spa | Creación inicial del notebook |");
            sb.AppendLine("# COMMAND ----------\n");

            // 6. Tablas Entrada y Salida
            sb.AppendLine("# MAGIC %md");
            sb.AppendLine("# MAGIC ### Tablas Entrada y Salida");
            sb.AppendLine("# MAGIC #### Entrada");
            sb.AppendLine("# MAGIC | TABLA | DESCRIPCION |");
            sb.AppendLine("# MAGIC |---|---|");
            sb.AppendLine("# MAGIC ");
            sb.AppendLine("# MAGIC #### Salida");
            sb.AppendLine("# MAGIC | TABLA | DESCRIPCION |");
            sb.AppendLine("# MAGIC |---|---|");
            sb.AppendLine("# COMMAND ----------\n");

            // 7. Captura de Variables (Widgets)
            sb.AppendLine("# MAGIC %md");
            sb.AppendLine($"# MAGIC ## {sectionCounter++}. Captura de Variables");
            sb.AppendLine("# MAGIC ### Crear Widgets para Captura de Variables");
            sb.AppendLine("# COMMAND ----------\n");

            if (modelo.Parametros != null && modelo.Parametros.Count > 0)
            {
                foreach (var param in modelo.Parametros)
                {
                    if (string.IsNullOrWhiteSpace(param.NombreWidget)) continue;
                    sb.AppendLine($"dbutils.widgets.text(\"{param.NombreWidget}\", \"{param.ValorPorDefecto}\", \"{param.Etiqueta}\")");
                }
            }
            else
            {
                sb.AppendLine("# No se definieron widgets.");
            }
            sb.AppendLine("\n# COMMAND ----------\n");

            // 8. Asignar Objeto a Lectura de Widgets (W -> X y spark.conf.set)
            sb.AppendLine("# MAGIC %md");
            sb.AppendLine($"# MAGIC ## {sectionCounter++}. Asignar Objeto a Lectura de Widgets");
            sb.AppendLine("# COMMAND ----------\n");

            if (modelo.Parametros != null && modelo.Parametros.Count > 0)
            {
                foreach (var param in modelo.Parametros)
                {
                    if (string.IsNullOrWhiteSpace(param.NombreWidget)) continue;
                    string varName = ObtenerNombreVariable(param.NombreWidget);

                    sb.AppendLine($"{varName} = dbutils.widgets.get(\"{param.NombreWidget}\")");
                    sb.AppendLine($"spark.conf.set(\"bci.{varName}\", {varName})");
                }

                sb.AppendLine("\nprint(\"--- Parámetros Inicializados ---\")");
                foreach (var param in modelo.Parametros)
                {
                    if (string.IsNullOrWhiteSpace(param.NombreWidget)) continue;
                    string varName = ObtenerNombreVariable(param.NombreWidget);
                    sb.AppendLine($"print(f\"Parámetro {varName}: {{{varName}}}\")");
                }
            }
            else
            {
                sb.AppendLine("print(\"Sin parámetros para inicializar.\")");
            }
            sb.AppendLine("\n# COMMAND ----------\n");

            // 9. Funciones Auxiliares
            sb.AppendLine("# MAGIC %md");
            sb.AppendLine($"# MAGIC ## {sectionCounter++}. Funciones Auxiliares");
            sb.AppendLine("# COMMAND ----------\n");

            if (modelo.IncluirSqlSafe)
            {
                sb.AppendLine("def sqlSafe(texto):");
                sb.AppendLine("    try:");
                sb.AppendLine("        print(\"Ejecutando consulta SQL...\")");
                sb.AppendLine("        return spark.sql(str(texto))");
                sb.AppendLine("    except Exception as e:");
                sb.AppendLine("        dbutils.notebook.exit(\"{\\\"coderror\\\":\\\"20001\\\", \\\"msgerror\\\":\\\"Error en sqlSafe: \"+str(e)+\"\\\"}\")");
                sb.AppendLine();
            }
            if (modelo.IncluirFuncionesFechas)
            {
                sb.AppendLine("from pyspark.sql.functions import current_date, date_format");
                sb.AppendLine("def obtener_periodo_actual():");
                sb.AppendLine("    return spark.range(1).select(date_format(current_date(), \"yyyyMM\")).first()[0]");
                sb.AppendLine();
            }
            sb.AppendLine("# COMMAND ----------\n");

            // 10. Proceso General (Lógica del Usuario)
            sb.AppendLine("# MAGIC %md");
            sb.AppendLine($"# MAGIC ## {sectionCounter++}. Proceso General");
            sb.AppendLine("# COMMAND ----------\n");

            if (!string.IsNullOrWhiteSpace(modelo.CodigoUsuario))
            {
                sb.AppendLine(modelo.CodigoUsuario);
            }
            else
            {
                sb.AppendLine("print(\"Inicio del proceso general...\")");
            }
            sb.AppendLine("\n# COMMAND ----------\n");

            // 11. Mensaje Final
            sb.AppendLine("# MAGIC %md");
            sb.AppendLine("# MAGIC ## Mensaje Final");
            sb.AppendLine("# COMMAND ----------\n");
            sb.AppendLine("dbutils.notebook.exit(\"{\\\"coderror\\\":\\\"0\\\", \\\"msgerror\\\":\\\"Notebook termina ejecucion satisfactoriamente\\\"}\")");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private string ObtenerNombreVariable(string widgetName)
        {
            widgetName = widgetName.Trim();
            if (widgetName.EndsWith("W")) return widgetName.Substring(0, widgetName.Length - 1) + "X";
            if (widgetName.EndsWith("w")) return widgetName.Substring(0, widgetName.Length - 1) + "x";
            return widgetName + "_X";
        }
    }
}
