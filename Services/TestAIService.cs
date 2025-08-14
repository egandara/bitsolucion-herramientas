using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NotebookValidator.Web.Models;

namespace NotebookValidator.Web.Services
{
    public class TestAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private readonly string _apiEndpoint;

        public TestAIService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["AzureOpenAI:ApiKey"];
            var endpoint = configuration["AzureOpenAI:Endpoint"];
            var deploymentName = configuration["AzureOpenAI:DeploymentName"];
            
            // Construimos la URL completa para la API
            _apiEndpoint = $"{endpoint.TrimEnd('/')}/openai/deployments/{deploymentName}/chat/completions?api-version=2023-05-15";
        }

        public async Task<string> SendPromptAsync(string textPrompt)
        {
            // 1. Crear el cliente HTTP
            var client = _httpClientFactory.CreateClient();

            // 2. Preparar el cuerpo de la petición usando nuestros modelos
            var requestBody = new OpenAIChatRequest
            {
                Messages = new List<OpenAIMessage>
                {
                    new OpenAIMessage { Role = "user", Content = textPrompt }
                }
            };
            
            var jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // 3. Crear el mensaje de la petición, añadiendo la clave de API en la cabecera
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _apiEndpoint);
            requestMessage.Headers.Add("api-key", _apiKey);
            requestMessage.Content = content;
            
            // 4. Enviar la petición a la API de Azure
            var httpResponse = await client.SendAsync(requestMessage);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception($"Error de la API de OpenAI: {httpResponse.StatusCode} - {errorContent}");
            }
            
            // 5. Leer y procesar la respuesta
            var responseJson = await httpResponse.Content.ReadAsStringAsync();
            var openAiResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseJson);

            return openAiResponse?.Choices.FirstOrDefault()?.Message.Content ?? "No se recibió contenido en la respuesta.";
        }
    }
}