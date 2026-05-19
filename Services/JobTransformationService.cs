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

        // =========================================================================================
        // ARQUITECTURA BUNDLE MULTI-JOB: Centraliza configuraciones y ordena la gobernanza (DABs)
        // =========================================================================================
        public Dictionary<string, string> GenerateBundleConfigs(
            List<string> yamlContents,
            List<string> cleanedJobNames,
            List<string> permissionLevels,
            List<string> permissionUsers,
            List<bool> devAutocerts,
            List<bool> certAutocerts,
            List<bool> prodAutocerts)
        {
            var outputFiles = new Dictionary<string, string>();
            var databricksYml = new StringBuilder();

            string baseBundleName = cleanedJobNames.Count > 0 ? cleanedJobNames[0].Replace(" ", "_") : "bundle";
            databricksYml.AppendLine("bundle:");
            databricksYml.AppendLine($"  name: Data-dbs-datosriesgo-gld-{baseBundleName}");
            databricksYml.AppendLine();
            databricksYml.AppendLine("variables:");
            databricksYml.AppendLine("  path:");
            databricksYml.AppendLine("    description: Indica path del ambiente");
            databricksYml.AppendLine("    default: \"Develop\"");

            // ORDENAMIENTO REQUERIDO 1: jobPermissions al comienzo de la lista de gobernanza
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

            // ORDENAMIENTO REQUERIDO 2: flagAutocertificacion ordenado en segundo lugar
            databricksYml.AppendLine("  flagAutocertificacion:");
            databricksYml.AppendLine("    description: Flag para ejecutar certificacion");
            bool initialDefaultAutocert = devAutocerts.Count > 0 ? devAutocerts[0] : false;
            databricksYml.AppendLine($"    default: {initialDefaultAutocert.ToString().ToLower()}");

            // Recopilar y unificar todos los parámetros de notebooks encontrados en el lote de archivos
            var allParametersList = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < yamlContents.Count; i++)
            {
                var paramMatches = Regex.Matches(yamlContents[i], @"-\s*name:\s*([A-Za-z0-9_]+)[\s\r\n]*default:\s*[""']?([^""'\r\n]+)[""']?");
                foreach (Match m in paramMatches)
                {
                    string pName = m.Groups[1].Value;
                    string pVal = m.Groups[2].Value;
                    if (!allParametersList.Any(x => x.Key == pName))
                    {
                        allParametersList.Add(new KeyValuePair<string, string>(pName, pVal));
                    }
                }
            }

            // Registrar parámetros unificados con su descripción vacía reglamentaria
            foreach (var param in allParametersList)
            {
                databricksYml.AppendLine($"  {param.Key}:");
                databricksYml.AppendLine("    description: \"\"");
                databricksYml.AppendLine($"    default: \"{TransformValue(param.Value, "DEV")}\"");
            }

            // ORDENAMIENTO REQUERIDO 3: Variables específicas de nombres de jobs con etiqueta [dev] al final del todo
            for (int i = 0; i < cleanedJobNames.Count; i++)
            {
                string suffix = cleanedJobNames.Count > 1 ? "_" + (i + 1) : "";
                databricksYml.AppendLine($"  jobName_{cleanedJobNames[i].Replace(" ", "_")}:");
                databricksYml.AppendLine($"    description: nombre del job {cleanedJobNames[i]}");
                databricksYml.AppendLine($"    default: \"[dev] {cleanedJobNames[i].Replace(" ", "_")}\"");
            }

            databricksYml.AppendLine();
            databricksYml.AppendLine("include:");
            databricksYml.AppendLine("  - resources/*.yml");
            databricksYml.AppendLine();
            databricksYml.AppendLine("targets:");

            // TARGET DEVELOP (Sin etiquetas sucias [dev] o [crt] en variables internas)
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
            for (int i = 0; i < cleanedJobNames.Count; i++)
            {
                string jName = cleanedJobNames[i].Replace(" ", "_");
                databricksYml.AppendLine($"      jobName_{jName}: \"develop-datosriesgo-plt-{jName}\"");
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
            for (int i = 0; i < cleanedJobNames.Count; i++)
            {
                string jName = cleanedJobNames[i].Replace(" ", "_");
                databricksYml.AppendLine($"      jobName_{jName}: \"certification-datosriesgo-plt-{jName}\"");
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
            for (int i = 0; i < cleanedJobNames.Count; i++)
            {
                string jName = cleanedJobNames[i].Replace(" ", "_");
                databricksYml.AppendLine($"      jobName_{jName}: \"production-datosriesgo-plt-{jName}\"");
            }
            bool prodAutoCombined = prodAutocerts.Count > 0 ? prodAutocerts[0] : false;
            databricksYml.AppendLine($"      flagAutocertificacion: {prodAutoCombined.ToString().ToLower()}");
            databricksYml.AppendLine("      jobPermissions:");
            databricksYml.AppendLine("        - level: CAN_MANAGE");
            databricksYml.AppendLine("          group_name: GRP_AZURE_DATABRICKS_PRODUCCION_BIGDATA_PRD");

            outputFiles.Add("databricks.yml", databricksYml.ToString());

            // Procesar y empaquetar de forma nativa cada recurso individual bajo la carpeta resources/
            for (int i = 0; i < yamlContents.Count; i++)
            {
                string cleanYaml = yamlContents[i];
                string jCleanName = cleanedJobNames[i].Replace(" ", "_");

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

                cleanYaml = Regex.Replace(cleanYaml, @"notebook_path:\s*/Repos/[^/]+/[^/]+/", "notebook_path: /Repos/${var.databricks_folder}/${bundle.name}/");
                foreach (var param in currentParamsList)
                {
                    string pattern = @"(name:\s*" + param.Key + @"[\s\r\n]*default:\s*[""']?)" + Regex.Escape(param.Value) + @"([""']?)";
                    cleanYaml = Regex.Replace(cleanYaml, pattern, "$1${var." + param.Key + "}$2");
                }

                var currentMTasks = Regex.Match(cleanYaml, @"(^[ \t]*tasks:[\s\S]*?)(?=(^[ \t]*job_clusters:))", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                string currentTasksBody = currentMTasks.Success ? currentMTasks.Groups[1].Value.TrimEnd() : "";

                var resourceYml = new StringBuilder();
                resourceYml.AppendLine("resources:");
                resourceYml.AppendLine("  jobs:");
                resourceYml.AppendLine($"    {jCleanName}:");
                resourceYml.AppendLine($"      name: ${{var.jobName_{jCleanName}}}"); // Enlace dinámico a la variable del bundle específico
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
