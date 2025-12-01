using System.Text.Json.Serialization;

namespace NotebookValidator.Web.Models
{
    // Clase raíz
    public class DatabricksJobConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email_notifications")]
        public object? EmailNotifications { get; set; } // Puede ser tipado si es necesario

        [JsonPropertyName("webhook_notifications")]
        public object? WebhookNotifications { get; set; }

        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; }

        [JsonPropertyName("max_concurrent_runs")]
        public int MaxConcurrentRuns { get; set; }

        [JsonPropertyName("tasks")]
        public List<JobTask> Tasks { get; set; } = new();

        [JsonPropertyName("job_clusters")]
        public List<JobClusterConfig> JobClusters { get; set; } = new();

        [JsonPropertyName("parameters")]
        public List<JobParameter> Parameters { get; set; } = new();

        // Propiedad auxiliar para capturar el resto del JSON no mapeado y no perderlo
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; } = new();
    }

    public class JobTask
    {
        [JsonPropertyName("task_key")]
        public string TaskKey { get; set; } = string.Empty;

        [JsonPropertyName("run_if")]
        public string RunIf { get; set; } = string.Empty;

        [JsonPropertyName("notebook_task")]
        public NotebookTask? NotebookTask { get; set; }

        [JsonPropertyName("job_cluster_key")]
        public string JobClusterKey { get; set; } = string.Empty;

        [JsonPropertyName("depends_on")]
        public List<TaskDependency>? DependsOn { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; } = new();
    }

    public class NotebookTask
    {
        [JsonPropertyName("notebook_path")]
        public string NotebookPath { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("base_parameters")]
        public Dictionary<string, string>? BaseParameters { get; set; }
    }

    public class TaskDependency
    {
        [JsonPropertyName("task_key")]
        public string TaskKey { get; set; } = string.Empty;
    }

    public class JobClusterConfig
    {
        [JsonPropertyName("job_cluster_key")]
        public string JobClusterKey { get; set; } = string.Empty;

        [JsonPropertyName("new_cluster")]
        public NewClusterDefinition NewCluster { get; set; } = new();
    }

    public class NewClusterDefinition
    {
        [JsonPropertyName("spark_version")]
        public string SparkVersion { get; set; } = string.Empty;

        [JsonPropertyName("node_type_id")]
        public string NodeTypeId { get; set; } = string.Empty;

        // Usamos Dictionary para spark_conf porque las claves tienen puntos (ej. spark.hadoop...)
        [JsonPropertyName("spark_conf")]
        public Dictionary<string, string> SparkConf { get; set; } = new();

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; } = new();
    }

    public class JobParameter
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("default")]
        public string Default { get; set; } = string.Empty;
    }
}