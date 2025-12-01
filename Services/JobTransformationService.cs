using System.Text.Json;
using NotebookValidator.Web.Models;

namespace NotebookValidator.Web.Services
{
    public class JobTransformationService
    {
        // Actualizamos la firma para aceptar los configs de Spark como string
        public Dictionary<string, string> GenerateEnvironmentConfigs(
            string jsonContent,
            string? certSparkConfJson,
            string? prodSparkConfJson)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            var devConfig = JsonSerializer.Deserialize<DatabricksJobConfig>(jsonContent);
            if (devConfig == null) throw new ArgumentException("El JSON del Job no es válido.");

            // Parsear los configs de Spark ingresados por el usuario (si existen)
            Dictionary<string, string>? certSparkConf = null;
            Dictionary<string, string>? prodSparkConf = null;

            if (!string.IsNullOrWhiteSpace(certSparkConfJson))
            {
                try { certSparkConf = JsonSerializer.Deserialize<Dictionary<string, string>>(certSparkConfJson); }
                catch { /* Manejar error de JSON inválido si es necesario o dejarlo null */ }
            }

            if (!string.IsNullOrWhiteSpace(prodSparkConfJson))
            {
                try { prodSparkConf = JsonSerializer.Deserialize<Dictionary<string, string>>(prodSparkConfJson); }
                catch { /* Manejar error */ }
            }

            // Generar versiones
            var certConfig = CloneAndTransform(devConfig, "CERT", certSparkConf);
            var prodConfig = CloneAndTransform(devConfig, "PROD", prodSparkConf);

            return new Dictionary<string, string>
            {
                { "Desarrollo.json", jsonContent },
                { "Certificacion.json", JsonSerializer.Serialize(certConfig, options) },
                { "Produccion.json", JsonSerializer.Serialize(prodConfig, options) }
            };
        }

        private DatabricksJobConfig CloneAndTransform(
            DatabricksJobConfig original,
            string targetEnv,
            Dictionary<string, string>? overrideSparkConf)
        {
            var json = JsonSerializer.Serialize(original);
            var config = JsonSerializer.Deserialize<DatabricksJobConfig>(json)!;

            // 1. Transformar Rutas de Notebooks
            foreach (var task in config.Tasks)
            {
                if (task.NotebookTask != null)
                {
                    task.NotebookTask.NotebookPath = TransformPath(task.NotebookTask.NotebookPath, targetEnv);
                }
            }

            // 2. Transformar Parámetros Globales (NUEVAS REGLAS)
            foreach (var param in config.Parameters)
            {
                param.Default = TransformValue(param.Default, targetEnv);
            }

            // 3. Aplicar Spark Config Manual (Parametrizado por el usuario)
            if (overrideSparkConf != null && config.JobClusters.Any())
            {
                // Asumimos que aplicamos el config al primer cluster o a todos
                // Si tienes múltiples clusters, podríamos necesitar lógica extra.
                foreach (var cluster in config.JobClusters)
                {
                    cluster.NewCluster.SparkConf = overrideSparkConf;
                }
            }
            else
            {
                // Si el usuario no puso nada, aplicamos transformaciones básicas por si acaso
                // Opcional: podrías dejar el de dev si prefieres.
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
            // Corrección: Rutas en Inglés (Certification / Production)
            if (env == "CERT")
            {
                return path.Replace("/Repos/Develop/", "/Repos/Certification/");
            }
            if (env == "PROD")
            {
                return path.Replace("/Repos/Develop/", "/Repos/Production/");
            }
            return path;
        }

        private string TransformValue(string value, string env)
        {
            if (string.IsNullOrEmpty(value)) return value;

            if (env == "CERT")
            {
                // Reglas CERTIFICACIÓN
                if (value.StartsWith("dsr_")) return value.Replace("dsr_", "crt_");

                // Regla específica Storage: sbx -> dev (según tu instrucción)
                value = value.Replace("bcirg2dlssbx", "bcirg2dlsdev");

                // Otras reglas genéricas si aplican
                value = value.Replace("SECRETDEVSQLDBRICKS", "SECRETCERTSQLDBRICKS");
            }
            else if (env == "PROD")
            {
                // Reglas PRODUCCIÓN
                if (value.StartsWith("dsr_")) return value.Replace("dsr_", "prd_");

                // Regla específica Storage: sbx -> prd
                value = value.Replace("bcirg2dlssbx", "bcirg2dlsprd");

                value = value.Replace("SECRETDEVSQLDBRICKS", "SECRETPRDSQLDBRICKS");
            }

            return value;
        }
    }
}