using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NotebookValidator.Web.Models
{
    public class OpenAIChatRequest
    {
        [JsonPropertyName("messages")]
        public List<OpenAIMessage> Messages { get; set; } = new();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 3000;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
    }

    public class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class OpenAIChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChoice> Choices { get; set; } = new();
    }

    public class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIMessage Message { get; set; } = new();
    }
}