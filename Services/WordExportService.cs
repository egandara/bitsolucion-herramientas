using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.IO;
using System.Linq;

namespace NotebookValidator.Web.Services
{
    public class WordExportService
    {
        // Método principal que será llamado por el controlador
        public byte[] CreateDocumentFromMarkdown(string markdownContent, string notebookName, string templatePath)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Usamos la plantilla solo para copiar su estructura base y estilos
                byte[] templateBytes = System.IO.File.ReadAllBytes(templatePath);
                memoryStream.Write(templateBytes, 0, templateBytes.Length);

                using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(memoryStream, true))
                {
                    MainDocumentPart mainPart = wordDocument.MainDocumentPart;

                    // Limpiamos el cuerpo del documento para empezar de cero
                    mainPart.Document.Body = new Body();
                    Body body = mainPart.Document.Body;

                    // 1. Añadir el Título Principal
                    AppendParagraph(body, notebookName, "Heading1");

                    // 2. Parsear y añadir el contenido de Markdown
                    var lines = markdownContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    bool inCodeBlock = false;

                    foreach (var line in lines)
                    {
                        if (line.Trim().StartsWith("```"))
                        {
                            inCodeBlock = !inCodeBlock;
                            continue; // Saltamos las líneas que delimitan el bloque de código
                        }

                        if (inCodeBlock)
                        {
                            AppendCodeLine(body, line);
                        }
                        else
                        {
                            AppendMarkdownLine(body, line);
                        }
                    }
                }
                return memoryStream.ToArray();
                // Guardamos los cambios en el stream
                // Esta línea no es necesaria aquí ya que WordprocessingDocument guarda al ser dispuesto
            }
        }

        // --- MÉTODOS AYUDANTES PARA APLICAR ESTILOS ---

        private void AppendParagraph(Body body, string text, string styleId)
        {
            Paragraph para = body.AppendChild(new Paragraph());
            Run run = para.AppendChild(new Run());
            run.AppendChild(new Text(text));
            para.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = styleId });
        }
        
        private void AppendCodeLine(Body body, string text)
        {
            Paragraph para = body.AppendChild(new Paragraph());
            para.ParagraphProperties = new ParagraphProperties(
                new ParagraphStyleId() { Val = "CodeBlock" } // Asumimos un estilo llamado "CodeBlock"
            );
            Run run = para.AppendChild(new Run());
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        }

        private void AppendMarkdownLine(Body body, string line)
        {
            Paragraph para = new Paragraph();

            // Títulos de Markdown
            if (line.StartsWith("### "))
            {
                para.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading2" });
                line = line.Substring(4);
            }
            else if (line.StartsWith("#### "))
            {
                para.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading3" });
                line = line.Substring(5);
            }
            // Viñetas de Markdown
            else if (line.Trim().StartsWith("* ") || line.Trim().StartsWith("- "))
            {
                para.ParagraphProperties = new ParagraphProperties(
                    new ParagraphStyleId() { Val = "ListParagraph" },
                    new NumberingProperties(
                        new NumberingLevelReference() { Val = 0 },
                        new NumberingId() { Val = 1 }
                    ));
                line = line.Trim().Substring(2);
            }
            else
            {
                para.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Normal" });
            }

            // Negritas con **
            var parts = line.Split(new[] { "**" }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                var run = new Run();
                if (i % 2 == 1) // Las partes impares están en negrita
                {
                    run.RunProperties = new RunProperties(new Bold());
                }
                run.Append(new Text(parts[i].Replace("`", "")));
                para.Append(run);
            }

            body.Append(para);
        }
    }
}