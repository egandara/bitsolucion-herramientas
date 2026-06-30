using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NotebookValidator.Web.Services
{
    public class OllamaTestService
    {
        private readonly HttpClient _httpClient;
        private readonly string _ollamaUrl;

        public OllamaTestService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _ollamaUrl = configuration["AI:OllamaUrl"] ?? "http://localhost:11434";
        }

        public async Task<string> GenerarTextoAsync(string prompt, string sistemaPromptUsuario)
        {
            var url = $"{_ollamaUrl.TrimEnd('/')}/v1/chat/completions";

            // =========================================================================
            // GLOSARIO CORPORATIVO FIJO Y REGLAS ESTRICTAS DE BIT SOLUCIONES
            // =========================================================================
            string baseSistemaPrompt =
                "Eres un Ingeniero de Datos Senior y Arquitecto de Software experto en Chile trabajando para la consultora BIT Soluciones.\n" +
                "Tu único propósito es explicar código, documentar procesos técnicos y analizar scripts o cuadratura de datos de forma ejecutiva.\n\n" +
                "REGLAS CORPORATIVAS ESTRICTAS QUE DEBES OBEDECER SIN EXCEPCIÓN:\n" +
                "- BCI significa ÚNICA Y EXCLUSIVAMENTE 'Banco de Crédito e Inversiones'. Prohibido decir 'Bank of Chile' o inventar traducciones.\n" +
                "- Si detectas tablas con el prefijo 'Tmp_' o variables asociadas a 'tablasProyecto', refiérete a ellas formalmente como 'Tablas Temporales de Cuadratura'.\n" +
                "- Responde SIEMPRE en un perfecto ESPAÑOL DE CHILE, utilizando un lenguaje formal, claro, limpio y corporativo.\n" +
                "- Estructura OBLIGATORIAMENTE tus respuestas usando sintaxis Markdown estructurada (Títulos con #, subtítulos, listas con viñetas '-' y palabras clave en negrita).\n" +
                "- Prohibido responder en inglés o generar bloques de pseudocódigo redundantes a menos que se te solicite explícitamente.\n\n" +
                "Enfoque contextual provisto por la sesión actual:\n";

            string sistemaPromptFinal = baseSistemaPrompt + (string.IsNullOrWhiteSpace(sistemaPromptUsuario) ? "Documentación y análisis general." : sistemaPromptUsuario);

            var requestBody = new
            {
                model = "qwen2.5:7b", // Tu nuevo modelo configurado
                messages = new[]
                {
                    new { role = "system", content = sistemaPromptFinal },
                    new { role = "user", content = prompt }
                },
                stream = false
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error de Ollama ({response.StatusCode}): {errorContent}";
                }

                var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();

                if (jsonResult.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
                    {
                        return content.GetString() ?? "La IA devolvió un texto vacío.";
                    }
                }

                return "No se pudo procesar la respuesta en el formato esperado.";
            }
            catch (Exception ex)
            {
                return $"Error de conexión con el servidor de IA local: {ex.Message}";
            }
        }
    }
}
