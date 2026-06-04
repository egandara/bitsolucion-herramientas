using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NotebookValidator.Web.Services
{
    public class LegacyJobMigrationService
    {
        // Añadimos 'string JobName' a la tupla de retorno
        public (string YamlContent, List<string> Warnings, string JobName) TransformJsonToYaml(string jsonContent, string targetUser = "exegaam@bci.cl", string targetRepo = "")
        {
            var warnings = new List<string>();
            JsonNode? node;

            try
            {
                node = JsonNode.Parse(jsonContent.Trim());
                if (node == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new InvalidOperationException("El archivo no es un JSON válido o está corrupto. Ábrelo en un bloc de notas y verifica que comience con '{'.");
            }

            if (node.AsObject().ContainsKey("settings") && node["settings"] != null)
            {
                node = node["settings"]!;
            }

            var jobObj = node.AsObject();

            // Aquí calculamos el nombre oficial del Job
            string originalName = jobObj["name"]?.ToString() ?? "Untitled_Job";
            string newName = originalName.StartsWith("[dev]") ? originalName : $"[dev]{originalName}";
            string jobKey = "dev_" + Regex.Replace(originalName, @"[^a-zA-Z0-9_]", "_");

            if (jobObj.ContainsKey("tasks") && jobObj["tasks"] is JsonArray tasks)
            {
                var newTasksArray = new JsonArray();

                foreach (var taskNode in tasks)
                {
                    if (taskNode == null) continue;
                    var taskObj = taskNode.AsObject();

                    if (taskObj.ContainsKey("run_if") && taskObj["run_if"]?.ToString() == "ALL_SUCCESS") taskObj.Remove("run_if");
                    taskObj.Remove("timeout_seconds");

                    if (taskObj.ContainsKey("email_notifications") && !taskObj["email_notifications"]!.AsObject().Any()) taskObj.Remove("email_notifications");
                    if (taskObj.ContainsKey("webhook_notifications") && !taskObj["webhook_notifications"]!.AsObject().Any()) taskObj.Remove("webhook_notifications");

                    if (taskObj.ContainsKey("notebook_task") && taskObj["notebook_task"] != null)
                    {
                        var notebookTask = taskObj["notebook_task"]!.AsObject();
                        if (notebookTask.ContainsKey("notebook_path") && notebookTask["notebook_path"] != null)
                        {
                            string path = notebookTask["notebook_path"]!.ToString();
                            string user = string.IsNullOrWhiteSpace(targetUser) ? "exegaam@bci.cl" : targetUser.Trim();
                            string repo = targetRepo?.Trim() ?? "";

                            path = Regex.Replace(path, @"^/Repos/([^/]+)/([^/]+)/(.*)$", match => {
                                string originalRepo = match.Groups[2].Value;
                                string restOfPath = match.Groups[3].Value;
                                string finalRepo = string.IsNullOrWhiteSpace(repo) ? originalRepo : repo;
                                return $"/Repos/{user}/{finalRepo}/{restOfPath}";
                            });
                            notebookTask["notebook_path"] = path;
                        }

                        if (notebookTask.ContainsKey("base_parameters") && notebookTask["base_parameters"] != null)
                        {
                            UpdateUnityCatalogParameters(notebookTask["base_parameters"]!.AsObject());
                        }
                    }

                    var orderedTask = new JsonObject();
                    if (taskObj.ContainsKey("task_key")) orderedTask["task_key"] = taskObj["task_key"]?.DeepClone();
                    if (taskObj.ContainsKey("depends_on")) orderedTask["depends_on"] = taskObj["depends_on"]?.DeepClone();
                    if (taskObj.ContainsKey("notebook_task")) orderedTask["notebook_task"] = taskObj["notebook_task"]?.DeepClone();
                    if (taskObj.ContainsKey("job_cluster_key")) orderedTask["job_cluster_key"] = taskObj["job_cluster_key"]?.DeepClone();

                    foreach (var kvp in taskObj.ToList())
                    {
                        if (!orderedTask.ContainsKey(kvp.Key)) orderedTask[kvp.Key] = kvp.Value?.DeepClone();
                    }

                    newTasksArray.Add(orderedTask);
                }
                jobObj["tasks"] = newTasksArray;
            }

            if (jobObj.ContainsKey("parameters") && jobObj["parameters"] is JsonArray globalParams)
            {
                foreach (var paramNode in globalParams)
                {
                    if (paramNode == null) continue;
                    var pObj = paramNode.AsObject();
                    if (pObj.ContainsKey("default") && pObj["default"] != null)
                    {
                        pObj["default"] = ConvertToUnityCatalog(pObj["default"]!.ToString());
                    }
                }
            }

            if (jobObj.ContainsKey("job_clusters") && jobObj["job_clusters"] is JsonArray clusters)
            {
                var newClustersArray = new JsonArray();

                foreach (var clusterNode in clusters)
                {
                    if (clusterNode == null) continue;
                    var cluster = clusterNode.AsObject();

                    if (cluster.ContainsKey("new_cluster") && cluster["new_cluster"] != null)
                    {
                        var oldNewCluster = cluster["new_cluster"]!.AsObject();
                        var orderedCluster = new JsonObject();

                        if (oldNewCluster.ContainsKey("cluster_name")) orderedCluster["cluster_name"] = oldNewCluster["cluster_name"]?.DeepClone() ?? "";
                        if (oldNewCluster.ContainsKey("spark_version")) orderedCluster["spark_version"] = oldNewCluster["spark_version"]?.DeepClone() ?? "16.4.x-scala2.12";

                        orderedCluster["spark_conf"] = GetDevSparkConf();

                        orderedCluster["azure_attributes"] = new JsonObject
                        {
                            ["first_on_demand"] = 1,
                            ["spot_bid_max_price"] = 100
                        };

                        if (oldNewCluster.ContainsKey("node_type_id")) orderedCluster["node_type_id"] = oldNewCluster["node_type_id"]?.DeepClone();
                        if (oldNewCluster.ContainsKey("custom_tags")) orderedCluster["custom_tags"] = oldNewCluster["custom_tags"]?.DeepClone();

                        orderedCluster["policy_id"] = "001EDF295A4ED6D3";
                        orderedCluster["data_security_mode"] = "SINGLE_USER";
                        orderedCluster["runtime_engine"] = "STANDARD";
                        orderedCluster["kind"] = "CLASSIC_PREVIEW";
                        orderedCluster["use_ml_runtime"] = false;
                        orderedCluster["is_single_node"] = true;
                        orderedCluster["num_workers"] = 0;

                        cluster["new_cluster"] = orderedCluster;
                    }
                    newClustersArray.Add(cluster.DeepClone());
                }
                jobObj["job_clusters"] = newClustersArray;
            }

            var orderedJobObj = new JsonObject();
            orderedJobObj["name"] = newName;

            if (jobObj.ContainsKey("tasks")) orderedJobObj["tasks"] = jobObj["tasks"]?.DeepClone();
            if (jobObj.ContainsKey("job_clusters")) orderedJobObj["job_clusters"] = jobObj["job_clusters"]?.DeepClone();
            if (jobObj.ContainsKey("queue")) orderedJobObj["queue"] = jobObj["queue"]?.DeepClone();
            if (jobObj.ContainsKey("parameters")) orderedJobObj["parameters"] = jobObj["parameters"]?.DeepClone();

            var keysToIgnore = new HashSet<string> {
                "name", "tasks", "job_clusters", "queue", "parameters",
                "job_id", "creator_user_name", "created_time", "run_as",
                "email_notifications", "webhook_notifications", "timeout_seconds",
                "max_concurrent_runs", "format"
            };

            foreach (var kvp in jobObj.ToList())
            {
                if (!keysToIgnore.Contains(kvp.Key)) orderedJobObj[kvp.Key] = kvp.Value?.DeepClone();
            }

            var cleanJobDictionary = ConvertJsonNodeToObject(orderedJobObj) as Dictionary<string, object>;
            if (cleanJobDictionary == null) throw new InvalidOperationException("Error interno al reestructurar el JSON.");

            var dabsStructure = new Dictionary<string, object>
            {
                { "resources", new Dictionary<string, object>
                    {
                        { "jobs", new Dictionary<string, object>
                            {
                                { jobKey, cleanJobDictionary }
                            }
                        }
                    }
                }
            };

            var serializer = new SerializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .WithIndentedSequences()
                .Build();

            string finalYaml = serializer.Serialize(dabsStructure);

            if (finalYaml.Contains("abfss://") && (!finalYaml.Contains("sbx") && !finalYaml.Contains("dsr")))
            {
                warnings.Add("Se detectaron rutas absolutas (abfss://) en los parámetros que podrían apuntar a Producción. Revisa los contenedores.");
            }
            if (finalYaml.Contains("{{secrets/") && (!finalYaml.Contains("dsr") && !finalYaml.Contains("sbx")))
            {
                warnings.Add("Se detectaron inyecciones de KeyVault ({{secrets/...) que no parecen ser de desarrollo. Verifica tus credenciales.");
            }

            // Retornamos también el nombre procesado del Job
            return (finalYaml, warnings, newName);
        }

        private object? ConvertJsonNodeToObject(JsonNode? node)
        {
            if (node == null) return null;

            if (node is JsonObject jsonObj)
            {
                var dict = new Dictionary<string, object>();
                foreach (var kvp in jsonObj)
                {
                    dict[kvp.Key] = ConvertJsonNodeToObject(kvp.Value)!;
                }
                return dict;
            }

            if (node is JsonArray jsonArray)
            {
                var list = new List<object>();
                foreach (var item in jsonArray)
                {
                    list.Add(ConvertJsonNodeToObject(item)!);
                }
                return list;
            }

            if (node is JsonValue jsonValue)
            {
                if (jsonValue.TryGetValue<bool>(out var boolVal)) return boolVal;
                if (jsonValue.TryGetValue<int>(out var intVal)) return intVal;
                if (jsonValue.TryGetValue<long>(out var longVal)) return longVal;
                if (jsonValue.TryGetValue<double>(out var doubleVal)) return doubleVal;
                if (jsonValue.TryGetValue<string>(out var stringVal)) return stringVal;

                return jsonValue.ToString();
            }

            return null;
        }

        private void UpdateUnityCatalogParameters(JsonObject parameters)
        {
            foreach (var prop in parameters.ToList())
            {
                if (prop.Value != null)
                {
                    string val = prop.Value!.ToString();
                    parameters[prop.Key] = ConvertToUnityCatalog(val);
                }
            }
        }

        private string ConvertToUnityCatalog(string val)
        {
            if (val.EndsWith("_db") && !val.Contains("catalog_bcidigital"))
            {
                return val.Contains("prd_") ? $"catalog_bcidigital_prd_001.{val}" : $"catalog_bcidigital_dsr_001.{val}";
            }
            return val;
        }

        private JsonObject GetDevSparkConf()
        {
            return new JsonObject
            {
                ["spark.executor.extraJavaOptions"] = "-Dlog4j2.formatMsgNoLookups=true",
                ["spark.hadoop.fs.azure.account.oauth2.client.endpoint"] = "https://login.microsoftonline.com/2fb79105-8354-42f6-a589-c38e0ec918ab/oauth2/token",
                ["spark.hadoop.fs.azure.account.oauth2.client.id.bcirg2dlssbx.dfs.core.windows.net"] = "64d325e9-9659-4a66-ab0c-837d167ee146",
                ["spark.hadoop.fs.azure.account.auth.type"] = "OAuth",
                ["spark.hadoop.fs.azure.account.oauth.provider.type"] = "org.apache.hadoop.fs.azurebfs.oauth2.ClientCredsTokenProvider",
                ["spark.databricks.delta.preview.enabled"] = "true",
                ["spark.hadoop.fs.azure.account.oauth2.client.id"] = "21d6459f-57bb-45e1-b6c5-dcab3f4b6486",
                ["spark.hadoop.javax.jdo.option.ConnectionDriverName"] = "com.microsoft.sqlserver.jdbc.SQLServerDriver",
                ["spark.hadoop.fs.azure.account.oauth.provider.type.bcirg2dlssbx.dfs.core.windows.net"] = "org.apache.hadoop.fs.azurebfs.oauth2.ClientCredsTokenProvider",
                ["spark.hadoop.fs.azure.account.oauth2.client.secret"] = "{{secrets/bci_ss_kv_bcirg3dsr_keyvault001/SECRETDEVBIGDATA}}",
                ["spark.hadoop.fs.azure.account.auth.type.bcirg2dlssbx.dfs.core.windows.net"] = "OAuth",
                ["spark.hadoop.javax.jdo.option.ConnectionPassword"] = "{{secrets/bci_ss_kv_bcirg3dsr_keyvault001/SECRETDEVSQLDBRICKS}}",
                ["spark.driver.extraJavaOptions"] = "-Djava.security.auth.login.config=/dbfs/CONF/jaas.conf -Djavax.security.auth.useSubjectCredsOnly=false",
                ["spark.hadoop.fs.azure.account.oauth2.client.endpoint.bcirg2dlssbx.dfs.core.windows.net"] = "https://login.microsoftonline.com/2fb79105-8354-42f6-a589-c38e0ec918ab/oauth2/token",
                ["spark.master"] = "local[*, 4]",
                ["spark.hadoop.javax.jdo.option.ConnectionURL"] = "jdbc:sqlserver://bcirg3dlk-asq-mstrct001.database.windows.net:1433;encrypt=true;trustServerCertificate=true;database=bcirg3dlkasqmstrct001",
                ["spark.hadoop.fs.azure.account.oauth2.client.secret.bcirg2dlssbx.dfs.core.windows.net"] = "{{secrets/bci_ss_kv_bcirg3dsr_keyvault001/SECRETTEYSEGINFOSTD}}",
                ["spark.sql.legacy.parquet.datetimeRebaseModeInWrite"] = "CORRECTED",
                ["spark.hadoop.javax.jdo.option.ConnectionUserName"] = "hms_admin",
                ["spark.databricks.cluster.profile"] = "singleNode",
                ["spark.sql.legacy.parquet.datetimeRebaseModeInRead"] = "LEGACY"
            };
        }
    }
}
