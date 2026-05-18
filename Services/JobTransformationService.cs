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

        private string TransformValue(string value, string env)
        {
            if (string.IsNullOrEmpty(value)) return value;

            if (env == "CERT")
            {
                if (value.Contains("dsr_")) value = value.Replace("dsr_", "crt_");
                if (value.Contains("drs_")) value = value.Replace("drs_", "crt_");
                value = value.Replace("bcirg2dlssbx", "bcirg2dlscrt");
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

        // ====================================================================================
        // MEJORADO: COMPILACIÓN INYECTANDO CAMPO DESCRIPTION Y FLAGAUTOCERTIFICACION DINÁMICO
        // ====================================================================================
        public Dictionary<string, string> GenerateBundleConfigs(
            string yamlContent,
            string cleanedJobName,
            List<string> permissionLevels,
            List<string> permissionUsers,
            bool devAutocert,
            bool certAutocert,
            bool prodAutocert)
        {
            string cleanYaml = yamlContent;

            // 1. Extraer dinámicamente las propiedades de hardware del clúster original
            string sparkVersion = "16.4.x-scala2.12";
            string nodeTypeId = "Standard_D4a_v4";
            string dataSecurityMode = "SINGLE_USER";
            string runtimeEngine = "STANDARD";
            string clusterKey = "bci_v3_cluster";

            var mVersion = Regex.Match(yamlContent, @"spark_version:\s*([^\r\n]+)");
            if (mVersion.Success) sparkVersion = mVersion.Groups[1].Value.Trim().Trim('"').Trim('\'');

            var mNode = Regex.Match(yamlContent, @"node_type_id:\s*([^\r\n]+)");
            if (mNode.Success) nodeTypeId = mNode.Groups[1].Value.Trim().Trim('"').Trim('\'');

            var mSecurity = Regex.Match(yamlContent, @"data_security_mode:\s*([^\r\n]+)");
            if (mSecurity.Success) dataSecurityMode = mSecurity.Groups[1].Value.Trim().Trim('"').Trim('\'');

            var mRuntime = Regex.Match(yamlContent, @"runtime_engine:\s*([^\r\n]+)");
            if (mRuntime.Success) runtimeEngine = mRuntime.Groups[1].Value.Trim().Trim('"').Trim('\'');

            var mClusterKey = Regex.Match(yamlContent, @"job_cluster_key:\s*([^\r\n]+)");
            if (mClusterKey.Success) clusterKey = mClusterKey.Groups[1].Value.Trim().Trim('"').Trim('\'');

            // 2. Escanear y compilar el inventario de parámetros originales para el databricks.yml
            var paramMatches = Regex.Matches(cleanYaml, @"-\s*name:\s*([A-Za-z0-9_]+)[\s\r\n]*default:\s*[""']?([^""'\r\n]+)[""']?");
            var parametersList = new List<KeyValuePair<string, string>>();
            foreach (Match m in paramMatches)
            {
                string pName = m.Groups[1].Value;
                string pVal = m.Groups[2].Value;
                if (!parametersList.Any(x => x.Key == pName))
                {
                    parametersList.Add(new KeyValuePair<string, string>(pName, pVal));
                }
            }

            // 3. Sanitización masiva de rutas de repositorios a la máscara de bundles
            cleanYaml = Regex.Replace(cleanYaml, @"notebook_path:\s*/Repos/[^/]+/[^/]+/", "notebook_path: /Repos/${var.databricks_folder}/${bundle.name}/");
            foreach (var param in parametersList)
            {
                string pattern = @"(name:\s*" + param.Key + @"[\s\r\n]*default:\s*[""']?)" + Regex.Escape(param.Value) + @"([""']?)";
                cleanYaml = Regex.Replace(cleanYaml, pattern, "$1${var." + param.Key + "}$2");
            }

            // 4. Aislamiento estable de la grilla de tareas (tasks)
            var mTasks = Regex.Match(cleanYaml, @"(^[ \t]*tasks:[\s\S]*?)(?=(^[ \t]*job_clusters:))", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            string tasksBody = mTasks.Success ? mTasks.Groups[1].Value.TrimEnd() : "";

            // 5. Ensamblado secuencial del recurso yml (Estructura de cluster arriba)
            var resourceYml = new StringBuilder();
            resourceYml.AppendLine("new_cluster: &new_cluster");
            resourceYml.AppendLine("  new_cluster:");
            resourceYml.AppendLine("    cluster_name: \"\"");
            resourceYml.AppendLine($"    spark_version: {sparkVersion}");
            resourceYml.AppendLine($"    node_type_id: {nodeTypeId}");
            resourceYml.AppendLine("    enable_elastic_disk: true");
            resourceYml.AppendLine($"    data_security_mode: {dataSecurityMode}");
            resourceYml.AppendLine($"    runtime_engine: {runtimeEngine}");
            resourceYml.AppendLine("    custom_tags:");
            resourceYml.AppendLine("      clusterSource: dops-datosriesgo");
            resourceYml.AppendLine("    autoscale:");
            resourceYml.AppendLine("      min_workers: 1");
            resourceYml.AppendLine("      max_workers: 4");
            resourceYml.AppendLine();
            resourceYml.AppendLine("resources:");
            resourceYml.AppendLine("  jobs:");
            resourceYml.AppendLine($"    {cleanedJobName.Replace(" ", "_")}:");
            resourceYml.AppendLine("      name: ${var.jobName}");
            resourceYml.AppendLine("      job_clusters:");
            resourceYml.AppendLine($"        - job_cluster_key: {clusterKey}");
            resourceYml.AppendLine("          <<: *new_cluster");

            if (!string.IsNullOrEmpty(tasksBody))
            {
                resourceYml.AppendLine(tasksBody);
            }

            resourceYml.AppendLine("      queue:");
            resourceYml.AppendLine("        enabled: true");

            if (parametersList.Count > 0)
            {
                resourceYml.AppendLine("      parameters:");
                foreach (var param in parametersList)
                {
                    resourceYml.AppendLine($"        - name: {param.Key}");
                    resourceYml.AppendLine($"          default: \"${{var.{param.Key}}}\"");
                }
            }
            resourceYml.AppendLine("      permissions: \"${var.jobPermissions}\"");

            // 6. Construcción del Manifiesto Global databricks.yml inyectando description y flagAutocertificacion
            var databricksYml = new StringBuilder();
            databricksYml.AppendLine("bundle:");
            databricksYml.AppendLine($"  name: Data-dbs-datosriesgo-gld-{cleanedJobName.Replace(" ", "_")}");
            databricksYml.AppendLine();
            databricksYml.AppendLine("variables:");
            databricksYml.AppendLine("  databricks_folder:");
            databricksYml.AppendLine("    description: Carpeta donde se genera git folder");
            databricksYml.AppendLine("    default: Develop");
            databricksYml.AppendLine("  jobName:");
            databricksYml.AppendLine("    description: Nombre del job orquestado");
            databricksYml.AppendLine("    default: ${bundle.target}-datosriesgo-plt-" + cleanedJobName.Replace(" ", "-"));

            // MEJORA 1: Inyección del campo description: "" vacío de respaldo para evitar fallos del linter de BCI
            foreach (var param in parametersList)
            {
                databricksYml.AppendLine($"  {param.Key}:");
                databricksYml.AppendLine("    description: \"\"");
                databricksYml.AppendLine($"    default: \"{TransformValue(param.Value, "DEV")}\"");
            }

            // MEJORA 2: Inyección de flagAutocertificacion global por defecto (DEV)
            databricksYml.AppendLine("  flagAutocertificacion:");
            databricksYml.AppendLine("    description: Flag para ejecutar certificacion");
            databricksYml.AppendLine($"    default: {devAutocert.ToString().ToLower()}");

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
            databricksYml.AppendLine("      databricks_folder: Develop");
            databricksYml.AppendLine("      jobName: develop-datosriesgo-plt-" + cleanedJobName.Replace(" ", "-"));
            foreach (var param in parametersList)
            {
                databricksYml.AppendLine($"      {param.Key}: \"{TransformValue(param.Value, "DEV")}\"");
            }
            databricksYml.AppendLine($"      flagAutocertificacion: {devAutocert.ToString().ToLower()}");
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
            databricksYml.AppendLine("      databricks_folder: Certification");
            databricksYml.AppendLine("      jobName: certification-datosriesgo-plt-" + cleanedJobName.Replace(" ", "-"));
            foreach (var param in parametersList)
            {
                databricksYml.AppendLine($"      {param.Key}: \"{TransformValue(param.Value, "CERT")}\"");
            }
            databricksYml.AppendLine($"      flagAutocertificacion: {certAutocert.ToString().ToLower()}");
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
            databricksYml.AppendLine("      databricks_folder: Production");
            databricksYml.AppendLine("      jobName: production-datosriesgo-plt-" + cleanedJobName.Replace(" ", "-"));
            foreach (var param in parametersList)
            {
                databricksYml.AppendLine($"      {param.Key}: \"{TransformValue(param.Value, "PROD")}\"");
            }
            databricksYml.AppendLine($"      flagAutocertificacion: {prodAutocert.ToString().ToLower()}");
            databricksYml.AppendLine("      jobPermissions:");
            databricksYml.AppendLine("        - level: CAN_MANAGE");
            databricksYml.AppendLine("          group_name: GRP_AZURE_DATABRICKS_PRODUCCION_BIGDATA_PRD");

            return new Dictionary<string, string>
            {
                { "databricks.yml", databricksYml.ToString() },
                { $"resources/{cleanedJobName.Replace(" ", "_")}.yml", resourceYml.ToString() }
            };
        }
    }
}
