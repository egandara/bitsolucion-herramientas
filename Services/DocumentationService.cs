using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NotebookValidator.Web.Models;
using static QuestPDF.Helpers.Colors;

namespace NotebookValidator.Web.Services
{
    public class DocumentationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _promptTemplate;
        private readonly string _azureEndpoint;
        private readonly string _apiKey;

        public DocumentationService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["AzureOpenAI:ApiKey"];
            var deploymentName = configuration["AzureOpenAI:DeploymentName"];
            var endpoint = configuration["AzureOpenAI:Endpoint"];
            _azureEndpoint = $"{endpoint.TrimEnd('/')}/openai/deployments/{deploymentName}/chat/completions?api-version=2023-05-15";

                        _promptTemplate = @"# CONTEXTO Y PERSONA
Eres un arquitecto de datos senior y un experto documentador técnico con amplia experiencia en Databricks, PySpark y SQL. Tu audiencia es un nuevo desarrollador que necesita entender este notebook rápidamente para poder mantenerlo. Tu tono debe ser profesional, claro y técnico.

# TAREA PRINCIPAL
Tu tarea es generar una documentación técnica completa y precisa en formato Markdown a partir del código de un notebook de Databricks que te proporcionaré. El código vendrá separado por celdas, indicadas con `# Celda de Código [n]:`.

# ESTRUCTURA DE LA DOCUMENTACIÓN (OBLIGATORIA)
La documentación debe seguir estrictamente las siguientes secciones y formato:

### {Nombre del Notebook}
*Extrae el nombre del archivo .ipynb de la primera celda del código y úsalo como título principal.*

#### 1. Resumen General
*Un párrafo conciso (2-4 frases) que describa el propósito de negocio y el objetivo técnico del notebook.*

#### 2. Librerías y Dependencias
*Una lista de las librerías importantes utilizadas (ej. PySpark, pandas) y una breve descripción de su rol en el notebook.*

#### 3. Lógica Paso a Paso
*Narra el flujo del proceso de principio a fin, describiendo el propósito de cada celda o grupo lógico de celdas. Enfócate en el QUÉ hace el código y el PORQUÉ de cada etapa (ej. """"Primero, se cargan los datos de clientes desde la capa bronze. Luego, se enriquecen con la información de ventas..."""").*

#### 4. Descripción Detallada de Lógicas SQL
*Esta es la sección más importante. Realiza un análisis técnico profundo de CADA consulta SQL o transformación PySpark significativa. NO OMITAS NI RESUMAS NINGUNA CONSULTA. Para cada una:*
*- **Cita la consulta completa** dentro de un bloque de código SQL (```sql ... ```). La consulta debe ser exacta y sin abreviaciones.
*- **Explica en detalle** la lógica: qué tablas se unen (JOINs), qué filtros se aplican (WHERE), qué campos se calculan y cuál es el propósito de las funciones de agregación (GROUP BY, SUM, etc.).
*- **Describe el resultado esperado**: qué datos produce la consulta y cómo se utilizan esos datos en los pasos posteriores del notebook.
*- **Identifica cualquier suposición o dependencia** que la consulta tenga sobre los datos (ej. """"Se asume que la tabla de ventas ya está limpia y sin duplicados..."""").
*- **Si la consulta es compleja**, divídela en partes lógicas (como CTEs o subconsultas) y explica cada parte por separado para mayor claridad.

#### 5. Entradas (Inputs)
*Genera una lista de las **tablas o archivos de origen** que el notebook lee. Basa tu análisis en las cláusulas `FROM` y `JOIN` de las consultas. **No incluyas parámetros o variables de widget en esta sección.**
*- `esquema.tabla_1`: Breve descripción del propósito de la tabla.
*- `esquema.tabla_2`: Breve descripción del propósito de la tabla.*

#### 6. Salidas (Outputs)
*Genera una lista de las **tablas o archivos finales** que el notebook crea o modifica. Basa tu análisis en las cláusulas `CREATE TABLE`, `INSERT INTO` o `MERGE INTO`.
*- `esquema.tabla_final_1`: Breve descripción de la tabla generada.
*- `esquema.tabla_final_2`: Breve descripción de la tabla generada.*

# REGLAS ADICIONALES
- Basa tu análisis **únicamente** en el código proporcionado. No inventes información.
- Utiliza formato Markdown profesional, incluyendo listas y bloques de código.
- Sé conciso y ve al grano. Evita frases introductorias como """"Claro, aquí tienes la documentación"""".
- Evita también frases como """"indica, se infiere, se supone, previsiblemente o se asume"""". Tu análisis debe ser directo y basado en el código.
-- -
CÓDIGO DEL NOTEBOOK:
{0}"; // Tu prompt completo
        }

        // --- ESTE ES EL MÉTODO PÚBLICO QUE EL CONTROLADOR NECESITA ---
        public async Task<string> GetDocumentationMarkdownAsync(IFormFile notebookFile)
        {
            string notebookCode = await ExtractCodeFromNotebook(notebookFile);
            if (string.IsNullOrWhiteSpace(notebookCode))
            {
                throw new InvalidOperationException("No se pudo extraer código del notebook subido o el notebook no contiene celdas de código.");
            }
            return await GetDocumentationFromAI(notebookCode);
        }

        // Este método crea el archivo .docx y es público para que el controlador lo use.
        public byte[] CreateWordDocument(string markdownContent, string templatePath, string notebookName)
        {
            byte[] templateBytes = System.IO.File.ReadAllBytes(templatePath);
            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(templateBytes, 0, templateBytes.Length);

                using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(memoryStream, true))
                {
                    var body = wordDocument.MainDocumentPart.Document.Body;
                    
                    // Lógica para reemplazar el título y el contenido
                    ReplacePlaceholder(body, "{Nombre del Notebook}", notebookName, isTitle: true);

                    var placeholder = body.Descendants<Text>().FirstOrDefault(t => t.Text.Contains("[CONTENIDO_GENERADO]"));
                    if (placeholder != null)
                    {
                        var parentParagraph = placeholder.Parent.Parent as Paragraph;
                        if (parentParagraph != null)
                        {
                            var lines = markdownContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                            bool inCodeBlock = false;
                            Paragraph codeParagraph = null;

                            foreach (var line in lines)
                            {
                                // Lógica para el formato del documento (sin cambios)
                                if (line.Trim() == "```sql")
                                {
                                    inCodeBlock = true;
                                    codeParagraph = new Paragraph(new ParagraphProperties(
                                        new Shading() { Val = ShadingPatternValues.Clear, Fill = "F1F1F1" },
                                        new ParagraphBorders(
                                            new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                            new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                            new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                            new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
                                        )
                                    ));
                                    continue;
                                }
                                if (line.Trim() == "```")
                                {
                                    inCodeBlock = false;
                                    if (codeParagraph != null)
                                    {
                                        parentParagraph.Parent.InsertBefore(codeParagraph, parentParagraph);
                                        codeParagraph = null;
                                    }
                                    continue;
                                }

                                if (inCodeBlock && codeParagraph != null)
                                {
                                    var codeRun = new Run(new Text(line) { Space = SpaceProcessingModeValues.Preserve });
                                    codeRun.RunProperties = new RunProperties(new RunFonts() { Ascii = "Courier New" });
                                    codeParagraph.Append(codeRun, new Break());
                                }
                                else
                                {
                                    var cleanedLine = line.Replace("`", "");
                                    var newPara = new Paragraph();
                                    var newRun = new Run();
                                    var newText = new Text(cleanedLine.TrimStart('#', ' ', '-', '*'));
                                    
                                    var runProperties = new RunProperties();
                                    if (line.StartsWith("### ")) { runProperties.Append(new Bold(), new FontSize() { Val = "32" }); }
                                    else if (line.StartsWith("#### ")) { runProperties.Append(new Bold(), new FontSize() { Val = "28" }); }
                                    else if (line.Contains("**")) { runProperties.Append(new Bold()); }

                                    if (line.StartsWith("* ") || line.StartsWith("- "))
                                    {
                                        newPara.ParagraphProperties = new ParagraphProperties(
                                            new NumberingProperties(
                                                new NumberingLevelReference() { Val = 0 },
                                                new NumberingId() { Val = 1 }
                                            ));
                                    }

                                    newRun.PrependChild(runProperties);
                                    newRun.Append(newText);
                                    newPara.Append(newRun);
                                    parentParagraph.Parent.InsertBefore(newPara, parentParagraph);
                                }
                            }
                            placeholder.Text = string.Empty;
                        }
                    }
                }
                return memoryStream.ToArray();
            }
        }

        private async Task<string> ExtractCodeFromNotebook(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());
            var content = await streamReader.ReadToEndAsync();
            var notebook = JsonSerializer.Deserialize<Notebook>(content, new JsonSerializerOptions { AllowTrailingCommas = true });
            if (notebook == null || notebook.Cells == null) return string.Empty;

            return string.Join("\n\n---\n\n", notebook.Cells
                .Where(c => c.CellType == "code" && c.Source.Any())
                .Select((c, index) => $"# Celda de Código [{index + 1}]:\n" + string.Join("", c.Source)));
        }

        private async Task<string> GetDocumentationFromAI(string code)
        {
            var fullPrompt = _promptTemplate.Replace("{0}", code);
            var client = _httpClientFactory.CreateClient();
            var requestBody = new OpenAIChatRequest
            {
                Messages = new List<OpenAIMessage> { new OpenAIMessage { Role = "user", Content = fullPrompt } },
                MaxTokens = 4000
            };
            var jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _azureEndpoint);
            requestMessage.Headers.Add("api-key", _apiKey);
            requestMessage.Content = content;
            var httpResponse = await client.SendAsync(requestMessage);
            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception($"Error de la API de OpenAI: {httpResponse.StatusCode} - {errorContent}");
            }
            var responseJson = await httpResponse.Content.ReadAsStringAsync();
            var openAiResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseJson);
            return openAiResponse?.Choices.FirstOrDefault()?.Message.Content ?? "No se recibió contenido en la respuesta.";
        }
        
        private void ReplacePlaceholder(Body body, string placeholder, string value, bool isTitle = false)
        {
            var element = body.Descendants<Text>().FirstOrDefault(t => t.Text.Contains(placeholder));
            if (element != null)
            {
                element.Text = element.Text.Replace(placeholder, value);
                if (isTitle && element.Parent is Run run)
                {
                    var runProps = run.GetFirstChild<RunProperties>() ?? run.PrependChild(new RunProperties());
                    runProps.Append(new Bold(), new FontSize() { Val = "36" });
                }
            }
        }
    }
}