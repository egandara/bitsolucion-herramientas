using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NotebookValidator.Web.Models;

namespace NotebookValidator.Web.Services
{
    public class JobTransformationService
    {
        public Dictionary<string, string> GenerateEnvironmentConfigs(
            string jsonContent,
            string? certSparkConfJson,
            string? prodSparkConfJson)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            var devConfig = JsonSerializer.Deserialize<DatabricksJobConfig>(jsonContent);
            if (devConfig == null) throw new ArgumentException("El JSON del Job no es válido.");

            Dictionary<string, string>? certSparkConf = null;
            Dictionary<string, string>? prodSparkConf = null;

            if (!string.IsNullOrWhiteSpace(certSparkConfJson))
            {
                try { certSparkConf = JsonSerializer.Deserialize<Dictionary<string, string>>(certSparkConfJson); }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(prodSparkConfJson))
            {
                try { prodSparkConf = JsonSerializer.Deserialize<Dictionary<string, string>>(prodSparkConfJson); }
                catch { }
            }

            var devConfigTransformed = CloneAndTransform(devConfig, "DEV", null);
            var certConfig = CloneAndTransform(devConfig, "CERT", certSparkConf);
            var prodConfig = CloneAndTransform(devConfig, "PROD", prodSparkConf);

            string devJson = JsonSerializer.Serialize(devConfigTransformed, options);
            string certJson = JsonSerializer.Serialize(certConfig, options);
            string prodJson = JsonSerializer.Serialize(prodConfig, options);

            return new Dictionary<string, string>
            {
                { "Desarrollo.json", ApplyGlobalCleanups(devJson) },
                { "Certificacion.json", ApplyGlobalCleanups(certJson) },
                { "Produccion.json", ApplyGlobalCleanups(prodJson) }
            };
        }

        private DatabricksJobConfig CloneAndTransform(
            DatabricksJobConfig original,
            string targetEnv,
            Dictionary<string, string>? overrideSparkConf)
        {
            var json = JsonSerializer.Serialize(original);
            var config = JsonSerializer.Deserialize<DatabricksJobConfig>(json)!;

            foreach (var task in config.Tasks)
            {
                if (task.NotebookTask != null)
                {
                    task.NotebookTask.NotebookPath = TransformPath(task.NotebookTask.NotebookPath, targetEnv);
                }
            }

            foreach (var param in config.Parameters)
            {
                param.Default = TransformValue(param.Default, targetEnv);
            }

            if (overrideSparkConf != null && config.JobClusters.Any())
            {
                foreach (var cluster in config.JobClusters)
                {
                    cluster.NewCluster.SparkConf = overrideSparkConf;
                }
            }
            else
            {
                foreach (var cluster in config.JobClusters)
                {
                    var newConf = new Dictionary<string, string>();
                    foreach (var item in cluster.NewCluster.SparkConf)
                    {
                        newConf[TransformValue(item.Key, targetEnv)] = TransformValue(item.Value, targetEnv);
                    }
                    cluster.NewCluster.SparkConf = newConf;
                }
            }

            return config;
        }

        private string TransformPath(string path, string env)
        {
            if (string.IsNullOrEmpty(path)) return path;
            string targetRepo = "/Repos/Develop/";
            if (env == "CERT") targetRepo = "/Repos/Certification/";
            if (env == "PROD") targetRepo = "/Repos/Production/";
            return Regex.Replace(path, @"^/Repos/[^/]+/", targetRepo);
        }

        // =========================================================================================
        // CORREGIDO: Reemplazo especial de rutas de Storage para Certificación vs Producción
        // =========================================================================================
        private string TransformValue(string value, string env)
        {
            if (string.IsNullOrEmpty(value)) return value;

            if (env == "CERT")
            {
                if (value.Contains("dsr_")) value = value.Replace("dsr_", "crt_");
                if (value.Contains("drs_")) value = value.Replace("drs_", "crt_");

                // NOTA BCI: En certificación el storage de norma es bcirg2dlsdev (no crt)
                value = value.Replace("bcirg2dlssbx", "bcirg2dlsdev");
                value = value.Replace("SECRETDEVSQLDBRICKS", "SECRETCERTSQLDBRICKS");
            }
            else if (env == "PROD")
            {
                if (value.Contains("dsr_")) value = value.Replace("dsr_", "prd_");
                if (value.Contains("drs_")) value = value.Replace("drs_", "prd_");
                value = value.Replace("bcirg2dlssbx", "bcirg2dlsprd");
                value = value.Replace("SECRETDEVSQLDBRICKS", "SECRETPRDSQLDBRICKS");
            }

            return value;
        }

        private string ApplyGlobalCleanups(string json)
        {
            try
            {
                if (JsonNode.Parse(json) is JsonObject obj)
                {
                    if (obj["job_clusters"] is JsonArray clusters)
                    {
                        foreach (var cluster in clusters)
                        {
                            if (cluster?["new_cluster"] is JsonObject newCluster)
                            {
                                newCluster.Remove("policy_id");
                            }
                        }
                    }

                    obj["run_as"] = new JsonObject
                    {
                        ["user_name"] = "dbricksdeploy@bci.cl"
                    };

                    return obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                }
            }
            catch { }
            return json;
        }

        // =========================================================================================
        // ARQUITECTURA BUNDLE MULTI-JOB: Desacoplamiento de nombres, variables base y carpetas relativas
        // =========================================================================================
        public Dictionary<string, string> GenerateBundleConfigs(
            List<string> yamlContents,
            List<string> devNames,
            List<string> certNames,
            List<string> prodNames,
            List<string> permissionLevels,
            List<string> permissionUsers,
            List<bool> devAutocerts,
            List<bool> certAutocerts,
            List<bool> prodAutocerts,
            List<string> sourceTables,
            List<string> targetTables,
            string bundleName)
        {
            var outputFiles = new Dictionary<string, string>();
            var databricksYml = new StringBuilder();

            databricksYml.AppendLine("bundle:");
            databricksYml.AppendLine($"  name: {bundleName}");
            databricksYml.AppendLine();

            // Declaración estricta de las variables base exigidas por el CI
            databricksYml.AppendLine("variables:");
            databricksYml.AppendLine("  databricks_folder:");
            databricksYml.AppendLine("    description: Carpeta donde se genera git folder");
            databricksYml.AppendLine("    default: \"Develop\"");
            databricksYml.AppendLine("  path: ");
            databricksYml.AppendLine("    description: \"\"");
            databricksYml.AppendLine("    default: \"Develop\"");

            databricksYml.AppendLine("  jobPermissions:");
            databricksYml.AppendLine("    description: Set de permisos de gobernanza asignados desde la celula");
            databricksYml.AppendLine("    default:");
            if (permissionLevels != null && permissionLevels.Count > 0)
            {
                for (int i = 0; i < permissionLevels.Count; i++)
                {
                    databricksYml.AppendLine($"      - level: {permissionLevels[i]}");
                    databricksYml.AppendLine($"        user_name: {permissionUsers[i]}");
                }
            }
            else
            {
                databricksYml.AppendLine("      - level: CAN_MANAGE");
                databricksYml.AppendLine("        user_name: dbricksdeploy@bci.cl");
            }

            databricksYml.AppendLine("  flagAutocertificacion:");
            databricksYml.AppendLine("    description: Flag para ejecutar certificacion");
            bool initialDefaultAutocert = devAutocerts.Count > 0 ? devAutocerts[0] : false;
            databricksYml.AppendLine($"    default: {initialDefaultAutocert.ToString().ToLower()}");

            var allParametersList = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < yamlContents.Count; i++)
            {
                var paramMatches = Regex.Matches(yamlContents[i], @"-\s*name:\s*([A-Za-z0-9_]+)[\s\r\n]*default:\s*[""']?([^""'\r\n]+)[""']?");
                foreach (Match m in paramMatches)
                {
                    string pName = m.Groups[1].Value;
                    string pVal = m.Groups[2].Value;

                    if (pName != "databricks_folder" && pName != "path" && !allParametersList.Any(x => x.Key == pName))
                    {
                        allParametersList.Add(new KeyValuePair<string, string>(pName, pVal));
                    }
                }
            }

            foreach (var param in allParametersList)
            {
                databricksYml.AppendLine($"  {param.Key}:");
                databricksYml.AppendLine("    description: \"\"");
                databricksYml.AppendLine($"    default: \"{TransformValue(param.Value, "DEV")}\"");
            }

            for (int i = 0; i < devNames.Count; i++)
            {
                string jCleanName = devNames[i].Replace(" ", "_");
                databricksYml.AppendLine($"  jobName_{jCleanName}:");
                databricksYml.AppendLine($"    description: nombre del job {jCleanName}");
                databricksYml.AppendLine($"    default: \"{devNames[i]}\"");
            }

            databricksYml.AppendLine();
            databricksYml.AppendLine("include:");
            databricksYml.AppendLine("  - resources/*.yml");
            databricksYml.AppendLine();
            databricksYml.AppendLine("targets:");

            // TARGET DEVELOP
            databricksYml.AppendLine("  develop:");
            databricksYml.AppendLine("    default: true");
            databricksYml.AppendLine("    workspace:");
            databricksYml.AppendLine("      root_path: /Users/${workspace.current_user.userName}/.bundles/${bundle.name}/${bundle.target}");
            databricksYml.AppendLine("    variables:");
            databricksYml.AppendLine("      path: \"Develop\"");
            foreach (var param in allParametersList)
            {
                databricksYml.AppendLine($"      {param.Key}: \"{TransformValue(param.Value, "DEV")}\"");
            }
            for (int i = 0; i < devNames.Count; i++)
            {
                string jCleanName = devNames[i].Replace(" ", "_");
                databricksYml.AppendLine($"      jobName_{jCleanName}: \"{devNames[i]}\"");
            }
            bool devAutoCombined = devAutocerts.Count > 0 ? devAutocerts[0] : false;
            databricksYml.AppendLine($"      flagAutocertificacion: {devAutoCombined.ToString().ToLower()}");
            databricksYml.AppendLine("      jobPermissions:");
            if (permissionLevels != null && permissionLevels.Count > 0)
            {
                for (int i = 0; i < permissionLevels.Count; i++)
                {
                    databricksYml.AppendLine($"        - level: {permissionLevels[i]}");
                    databricksYml.AppendLine($"          user_name: {permissionUsers[i]}");
                }
            }

            // TARGET CERTIFICATION
            databricksYml.AppendLine("  certification:");
            databricksYml.AppendLine("    workspace:");
            databricksYml.AppendLine("      root_path: /Shared/.bundles/certification/${bundle.name}");
            databricksYml.AppendLine("    variables:");
            databricksYml.AppendLine("      path: \"Certification\"");
            foreach (var param in allParametersList)
            {
                databricksYml.AppendLine($"      {param.Key}: \"{TransformValue(param.Value, "CERT")}\"");
            }
            for (int i = 0; i < certNames.Count; i++)
            {
                string jCleanName = devNames[i].Replace(" ", "_"); // Usamos el devName como Key pero el valor es Cert
                databricksYml.AppendLine($"      jobName_{jCleanName}: \"{certNames[i]}\"");
            }
            bool certAutoCombined = certAutocerts.Count > 0 ? certAutocerts[0] : false;
            databricksYml.AppendLine($"      flagAutocertificacion: {certAutoCombined.ToString().ToLower()}");
            databricksYml.AppendLine("      jobPermissions:");
            if (permissionLevels != null && permissionLevels.Count > 0)
            {
                for (int i = 0; i < permissionLevels.Count; i++)
                {
                    databricksYml.AppendLine($"        - level: {permissionLevels[i]}");
                    databricksYml.AppendLine($"          user_name: {permissionUsers[i]}");
                }
            }

            // TARGET PRODUCTION
            databricksYml.AppendLine("  production:");
            databricksYml.AppendLine("    workspace:");
            databricksYml.AppendLine("      root_path: /Shared/.bundles/production/${bundle.name}");
            databricksYml.AppendLine("    variables:");
            databricksYml.AppendLine("      path: \"Production\"");
            foreach (var param in allParametersList)
            {
                databricksYml.AppendLine($"      {param.Key}: \"{TransformValue(param.Value, "PROD")}\"");
            }
            for (int i = 0; i < prodNames.Count; i++)
            {
                string jCleanName = devNames[i].Replace(" ", "_");
                databricksYml.AppendLine($"      jobName_{jCleanName}: \"{prodNames[i]}\"");
            }
            bool prodAutoCombined = prodAutocerts.Count > 0 ? prodAutocerts[0] : false;
            databricksYml.AppendLine($"      flagAutocertificacion: {prodAutoCombined.ToString().ToLower()}");
            databricksYml.AppendLine("      jobPermissions:");
            databricksYml.AppendLine("        - level: CAN_MANAGE");
            databricksYml.AppendLine("          group_name: GRP_AZURE_DATABRICKS_PRODUCCION_BIGDATA_PRD");

            outputFiles.Add("databricks.yml", databricksYml.ToString());

            // Procesar cada archivo de recurso YAML y acoplar el linaje dinámico corporativo
            for (int i = 0; i < yamlContents.Count; i++)
            {
                string cleanYaml = yamlContents[i];
                string jCleanName = devNames[i].Replace(" ", "_");

                string sparkVersion = "16.4.x-scala2.12";
                string nodeTypeId = "Standard_D4a_v4";
                string dataSecurityMode = "SINGLE_USER";
                string runtimeEngine = "STANDARD";
                string clusterKey = "bci_v3_cluster";

                var mVersion = Regex.Match(cleanYaml, @"spark_version:\s*([^\r\n]+)");
                if (mVersion.Success) sparkVersion = mVersion.Groups[1].Value.Trim().Trim('"').Trim('\'');

                var mNode = Regex.Match(cleanYaml, @"node_type_id:\s*([^\r\n]+)");
                if (mNode.Success) nodeTypeId = mNode.Groups[1].Value.Trim().Trim('"').Trim('\'');

                var mSecurity = Regex.Match(cleanYaml, @"data_security_mode:\s*([^\r\n]+)");
                if (mSecurity.Success) dataSecurityMode = mSecurity.Groups[1].Value.Trim().Trim('"').Trim('\'');

                var mRuntime = Regex.Match(cleanYaml, @"runtime_engine:\s*([^\r\n]+)");
                if (mRuntime.Success) runtimeEngine = mRuntime.Groups[1].Value.Trim().Trim('"').Trim('\'');

                var mClusterKey = Regex.Match(cleanYaml, @"job_cluster_key:\s*([^\r\n]+)");
                if (mClusterKey.Success) clusterKey = mClusterKey.Groups[1].Value.Trim().Trim('"').Trim('\'');

                var currentParamMatches = Regex.Matches(cleanYaml, @"-\s*name:\s*([A-Za-z0-9_]+)[\s\r\n]*default:\s*[""']?([^""'\r\n]+)[""']?");
                var currentParamsList = new List<KeyValuePair<string, string>>();
                foreach (Match m in currentParamMatches)
                {
                    string pName = m.Groups[1].Value;
                    string pVal = m.Groups[2].Value;
                    if (!currentParamsList.Any(x => x.Key == pName))
                    {
                        currentParamsList.Add(new KeyValuePair<string, string>(pName, pVal));
                    }
                }

                cleanYaml = Regex.Replace(cleanYaml, @"notebook_path:\s*/Repos/[^/]+/[^/]+/", "notebook_path: /Repos/${var.path}/${bundle.name}/");
                foreach (var param in currentParamsList)
                {
                    string pattern = @"(name:\s*" + param.Key + @"[\s\r\n]*default:\s*[""']?)" + Regex.Escape(param.Value) + @"([""']?)";
                    cleanYaml = Regex.Replace(cleanYaml, pattern, "$1${var." + param.Key + "}$2");
                }

                var currentMTasks = Regex.Match(cleanYaml, @"(^[ \t]*tasks:[\s\S]*?)(?=(^[ \t]*job_clusters:))", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                string currentTasksBody = currentMTasks.Success ? currentMTasks.Groups[1].Value.TrimEnd() : "";

                bool requiresAutocertTask = (devAutocerts.Count > i && devAutocerts[i]) ||
                                            (certAutocerts.Count > i && certAutocerts[i]) ||
                                            (prodAutocerts.Count > i && prodAutocerts[i]);

                if (requiresAutocertTask && !string.IsNullOrEmpty(currentTasksBody))
                {
                    var taskKeyMatches = Regex.Matches(currentTasksBody, @"task_key:\s*[""']?([A-Za-z0-9_-]+)[""']?");
                    string lastTaskKey = "";
                    if (taskKeyMatches.Count > 0)
                    {
                        lastTaskKey = taskKeyMatches[taskKeyMatches.Count - 1].Groups[1].Value;
                    }

                    string rawSources = sourceTables.Count > i ? sourceTables[i] : "";
                    string rawTargets = targetTables.Count > i ? targetTables[i] : "";

                    var sourceList = rawSources.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    var targetList = rawTargets.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    var sourceYamlParams = new Dictionary<string, string>();
                    var targetYamlParams = new Dictionary<string, string>();

                    int sIdx = 1;
                    foreach (var s in sourceList)
                    {
                        var parts = s.Split('|');
                        string widget = parts[0].Trim();
                        string table = parts.Length > 1 ? parts[1].Trim() : "";
                        if (string.IsNullOrEmpty(table)) table = widget;

                        string resolvedValue = (string.IsNullOrEmpty(widget) || parts.Length == 1) ? table : $"${{var.{widget}}}.{table}";
                        sourceYamlParams.Add($"origen{sIdx}", resolvedValue);
                        sIdx++;
                    }

                    int tIdx = 1;
                    foreach (var t in targetList)
                    {
                        var parts = t.Split('|');
                        string widget = parts[0].Trim();
                        string table = parts.Length > 1 ? parts[1].Trim() : "";
                        if (string.IsNullOrEmpty(table)) table = widget;

                        string resolvedValue = (string.IsNullOrEmpty(widget) || parts.Length == 1) ? table : $"${{var.{widget}}}.{table}";
                        targetYamlParams.Add($"destino{tIdx}", resolvedValue);
                        tIdx++;
                    }

                    string scalaNotebookName = $"ntb_auto_certificacion_allpurpose_{jCleanName.ToLower()}";

                    var autocertTaskBlock = new StringBuilder();
                    autocertTaskBlock.AppendLine();
                    autocertTaskBlock.AppendLine("        - task_key: Auto_Certificacion_BigData");

                    if (!string.IsNullOrEmpty(lastTaskKey))
                    {
                        autocertTaskBlock.AppendLine("          depends_on:");
                        autocertTaskBlock.AppendLine($"            - task_key: {lastTaskKey}");
                    }

                    autocertTaskBlock.AppendLine("          notebook_task:");
                    autocertTaskBlock.AppendLine($"            notebook_path: /Repos/${{var.path}}/${{bundle.name}}/notebooks/Notebooks/validaciones/{scalaNotebookName}");
                    autocertTaskBlock.AppendLine("            base_parameters:");
                    autocertTaskBlock.AppendLine("              ejecutarAutocertificacion: \"${var.flagAutocertificacion}\"");
                    autocertTaskBlock.AppendLine($"              nombreJob: \"${{var.jobName_{jCleanName}}}\"");
                    autocertTaskBlock.AppendLine("              nombreTarea: \"Auto_Certificacion_BigData\"");
                    autocertTaskBlock.AppendLine("              tipo: \"A\"");
                    autocertTaskBlock.AppendLine("              schemaBitacora: \"catalog_bcidigital_prd_001.prd_slv_governance\"");

                    foreach (var kvp in sourceYamlParams)
                    {
                        autocertTaskBlock.AppendLine($"              {kvp.Key}: \"{kvp.Value}\"");
                    }
                    foreach (var kvp in targetYamlParams)
                    {
                        autocertTaskBlock.AppendLine($"              {kvp.Key}: \"{kvp.Value}\"");
                    }

                    autocertTaskBlock.AppendLine($"          job_cluster_key: {clusterKey}");

                    currentTasksBody += autocertTaskBlock.ToString();

                    var scalaContent = new StringBuilder();
                    scalaContent.AppendLine("// Databricks notebook source");
                    scalaContent.AppendLine("// MAGIC %md");
                    scalaContent.AppendLine("// MAGIC # Proceso que realiza la certificación automática para procesos all purpose.");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// COMMAND ----------");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// MAGIC %md");
                    scalaContent.AppendLine("// MAGIC ## Información importante");
                    scalaContent.AppendLine("// MAGIC");
                    scalaContent.AppendLine("// MAGIC 1. Tener cuidado al reemplazar los datos en `sourceJson` y `targetJson`, hay que mantener los json array.");
                    scalaContent.AppendLine("// MAGIC 2. Tipo de proceso, se debe identificar para el parámetro `tipo`.");
                    scalaContent.AppendLine("// MAGIC | **Tipo** | **Origen** | **Destino** |");
                    scalaContent.AppendLine("// MAGIC |----------|-----------------|----------------------------------------------------------|");
                    scalaContent.AppendLine("// MAGIC | A        | ADLS, Delta     | ADLS, Delta                                              |");
                    scalaContent.AppendLine("// MAGIC | B        | ADLS            | ADLS, HDI                                                |");
                    scalaContent.AppendLine("// MAGIC | C        | ADLS            | ADLS, Confluent                                          |");
                    scalaContent.AppendLine("// MAGIC | D        | Mongo           | ADLS                                                     |");
                    scalaContent.AppendLine("// MAGIC | E        | Confluent, ADLS | Confluent, ADLS, API, Mongo, Teradata, Marketing Cloud   |");
                    scalaContent.AppendLine("// MAGIC | F        | ADLS            | MSSQL, ORACLE                                            |");
                    scalaContent.AppendLine("// MAGIC | G        | Teradata        | ADLS                                                     |");
                    scalaContent.AppendLine("// MAGIC | H        | Splunk          | ADLS                                                     |");
                    scalaContent.AppendLine("// MAGIC | I        | ADLS            | ADLS FeatureStore                                        |");
                    scalaContent.AppendLine("// MAGIC | J        | Confluent, ADLS | Files                                                    |");
                    scalaContent.AppendLine("// MAGIC 3. _*(Opcional)*_ En caso de que tu proceso sea streaming, debes agregarle un timeout a su respectiva tarea en el job cluster,");
                    scalaContent.AppendLine("// MAGIC  para que pueda avanzar al notebook de autocertificación. Luego a la tarea de este notebook, debes colocarle que se ejecute");
                    scalaContent.AppendLine("// MAGIC   cuando todas sus dependencias fallen.");
                    scalaContent.AppendLine("// MAGIC 4. Rutas desarrollo y certificación");
                    scalaContent.AppendLine("// MAGIC     * `catalog_bcidigital_dsr_001.sch_ida.bitacora_allpurpose`");
                    scalaContent.AppendLine("// MAGIC     * `-`");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// COMMAND ----------");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// MAGIC %md");
                    scalaContent.AppendLine("// MAGIC ## Imports");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// COMMAND ----------");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("import org.apache.spark.sql.functions._");
                    scalaContent.AppendLine("import org.apache.spark.sql._");
                    scalaContent.AppendLine("import java.util.Date");
                    scalaContent.AppendLine("import java.text.SimpleDateFormat");
                    scalaContent.AppendLine("import org.json.JSONArray");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// COMMAND ----------");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// MAGIC %md");
                    scalaContent.AppendLine("// MAGIC ## Variables");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// COMMAND ----------");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("/* Parámetros del servicio de datos */");
                    scalaContent.AppendLine("//Una variable por cada input/insumo");
                    foreach (var kvp in sourceYamlParams)
                    {
                        string widgetName = $"widget{char.ToUpper(kvp.Key[0]) + kvp.Key.Substring(1)}";
                        scalaContent.AppendLine($"val {widgetName} = dbutils.widgets.get(\"{kvp.Key}\")");
                    }

                    scalaContent.AppendLine("//Una variable por cada output.");
                    foreach (var kvp in targetYamlParams)
                    {
                        string widgetName = $"widget{char.ToUpper(kvp.Key[0]) + kvp.Key.Substring(1)}";
                        scalaContent.AppendLine($"val {widgetName} = dbutils.widgets.get(\"{kvp.Key}\")");
                    }

                    scalaContent.AppendLine();
                    scalaContent.AppendLine("/* Parámetros del proceso de certificación*/");
                    scalaContent.AppendLine("val fecha = new Date");
                    scalaContent.AppendLine("val formato = new SimpleDateFormat(\"yyyyMMdd\")");
                    scalaContent.AppendLine("val fechaActual = formato.format(fecha)");
                    scalaContent.AppendLine("//Nombre del job (tiene que ser el de desarrollo o certi)");
                    scalaContent.AppendLine("val nombreJob = dbutils.widgets.get(\"nombreJob\")");
                    scalaContent.AppendLine("//Nombre task donde se escribe en la tabla de salida. En caso de ser más de uno solo se anhida a uno.");
                    scalaContent.AppendLine("val nombreTarea = dbutils.widgets.get(\"nombreTarea\")");
                    scalaContent.AppendLine("//revisar la celda 1 que tiene una tabla. ");
                    scalaContent.AppendLine("val tipo = dbutils.widgets.get(\"tipo\")");
                    scalaContent.AppendLine("//catalog_bcidigital_dsr_001.sch_ida");
                    scalaContent.AppendLine("val schemaBitacora = dbutils.widgets.get(\"schemaBitacora\")");
                    scalaContent.AppendLine("//NORMALMENTE va true en el ambiente que quieres certificar y false en los otros.");
                    scalaContent.AppendLine("val ejecutarAutocertificacion = dbutils.widgets.get(\"ejecutarAutocertificacion\")");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("if(!ejecutarAutocertificacion.toBoolean) {  ");
                    scalaContent.AppendLine("  dbutils.notebook.exit(\"Finalizó OK => Ejecutar proceso: NO\")");
                    scalaContent.AppendLine("}");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// COMMAND ----------");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("/* Armado de variables de schemas */");
                    scalaContent.AppendLine("val rutaBitacora = s\"${schemaBitacora}.bitacora_allpurpose\"");
                    scalaContent.AppendLine();

                    var scalaSourceNames = new List<string>();
                    var scalaTargetNames = new List<string>();

                    foreach (var kvp in sourceYamlParams)
                    {
                        string varName = $"base{char.ToUpper(kvp.Key[0]) + kvp.Key.Substring(1)}";
                        string widgetRef = $"widget{char.ToUpper(kvp.Key[0]) + kvp.Key.Substring(1)}";
                        scalaContent.AppendLine($"val {varName} = s\"${{{widgetRef}}}\"");
                        scalaSourceNames.Add($"\"${varName}\"");
                    }
                    foreach (var kvp in targetYamlParams)
                    {
                        string varName = $"base{char.ToUpper(kvp.Key[0]) + kvp.Key.Substring(1)}";
                        string widgetRef = $"widget{char.ToUpper(kvp.Key[0]) + kvp.Key.Substring(1)}";
                        scalaContent.AppendLine($"val {varName} = s\"${{{widgetRef}}}\"");
                        scalaTargetNames.Add($"\"${varName}\"");
                    }

                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// COMMAND ----------");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// MAGIC %md");
                    scalaContent.AppendLine("// MAGIC ## Ejecución del proceso");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// COMMAND ----------");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// MAGIC %md");
                    scalaContent.AppendLine("// MAGIC ### Mapeo de sourceJson y targetJson");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// COMMAND ----------");
                    scalaContent.AppendLine();

                    string sourcesStr = scalaSourceNames.Count > 0 ? string.Join(", ", scalaSourceNames) : "\"\"";
                    string targetsStr = scalaTargetNames.Count > 0 ? string.Join(", ", scalaTargetNames) : "\"\"";

                    scalaContent.AppendLine("val taskMap = Map(");
                    scalaContent.AppendLine("  nombreTarea -> Map(");
                    scalaContent.AppendLine("    \"sourceJson\" -> s\"\"\"[{");
                    scalaContent.AppendLine("      \"type\": \"Delta\",");
                    scalaContent.AppendLine($"      \"source\": [{sourcesStr}]");
                    scalaContent.AppendLine("    }]\"\"\",");
                    scalaContent.AppendLine("    \"targetJson\" -> s\"\"\"[{");
                    scalaContent.AppendLine("      \"type\": \"Delta\",");
                    scalaContent.AppendLine($"      \"target\": [{targetsStr}]");
                    scalaContent.AppendLine("    }]\"\"\"");
                    scalaContent.AppendLine("  )");
                    scalaContent.AppendLine(")");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// COMMAND ----------");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// MAGIC %md");
                    scalaContent.AppendLine("// MAGIC ### Escritura en tabla bitacora");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("// COMMAND ----------");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("for ((k, v) <- taskMap) {");
                    scalaContent.AppendLine("    var sourceJson = new JSONArray(v.apply(\"sourceJson\")).toString(4)");
                    scalaContent.AppendLine("    var targetJson = new JSONArray(v.apply(\"targetJson\")).toString(4)");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("    val df = Seq(");
                    scalaContent.AppendLine("      (nombreJob, k, sourceJson, targetJson, tipo, fechaActual)");
                    scalaContent.AppendLine("    ).toDF(\"NombreJob\", \"NombreTarea\", \"Source\", \"Target\", \"Tipo\", \"FechaEjecucion\")");
                    scalaContent.AppendLine("     .withColumn(\"HoraEjecucion\", expr(\"from_utc_timestamp(current_timestamp(), 'America/Santiago')\"))");
                    scalaContent.AppendLine();
                    scalaContent.AppendLine("    df.write.format(\"delta\").mode(\"append\").saveAsTable(rutaBitacora)");
                    scalaContent.AppendLine("}");

                    string finalScalaCode = scalaContent.ToString().Replace("\r\n", "\n");

                    outputFiles[$"notebooks/Notebooks/validaciones/{scalaNotebookName}.scala"] = finalScalaCode;
                }

                var resourceYml = new StringBuilder();
                resourceYml.AppendLine("resources:");
                resourceYml.AppendLine("  jobs:");
                resourceYml.AppendLine($"    {jCleanName}:");
                resourceYml.AppendLine($"      name: ${{var.jobName_{jCleanName}}}");
                resourceYml.AppendLine("      job_clusters:");
                resourceYml.AppendLine($"        - job_cluster_key: {clusterKey}");
                resourceYml.AppendLine("          new_cluster:");
                resourceYml.AppendLine("            cluster_name: \"\"");
                resourceYml.AppendLine($"            spark_version: {sparkVersion}");
                resourceYml.AppendLine($"            node_type_id: {nodeTypeId}");
                resourceYml.AppendLine("            enable_elastic_disk: true");
                resourceYml.AppendLine($"            data_security_mode: {dataSecurityMode}");
                resourceYml.AppendLine($"            runtime_engine: {runtimeEngine}");
                resourceYml.AppendLine("            custom_tags:");
                resourceYml.AppendLine("              clusterSource: dops-datosriesgo");
                resourceYml.AppendLine("            autoscale:");
                resourceYml.AppendLine("              min_workers: 1");
                resourceYml.AppendLine("              max_workers: 4");

                if (!string.IsNullOrEmpty(currentTasksBody))
                {
                    resourceYml.AppendLine(currentTasksBody);
                }

                resourceYml.AppendLine("      queue:");
                resourceYml.AppendLine("        enabled: true");

                if (currentParamsList.Count > 0)
                {
                    resourceYml.AppendLine("      parameters:");
                    foreach (var param in currentParamsList)
                    {
                        resourceYml.AppendLine($"        - name: {param.Key}");
                        resourceYml.AppendLine($"          default: \"${{var.{param.Key}}}\"");
                    }
                }
                resourceYml.AppendLine("      permissions: \"${var.jobPermissions}\"");

                outputFiles.Add($"resources/{jCleanName}.yml", resourceYml.ToString());
            }

            return outputFiles;
        }
    }
}
