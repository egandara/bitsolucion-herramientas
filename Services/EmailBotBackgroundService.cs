using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Services
{
    public class EmailBotBackgroundService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailBotBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // Prefijo requerido en el asunto
        private const string SubjectRequiredPrefix = "Validación de Notebooks BCI";

        public EmailBotBackgroundService(
            IConfiguration configuration,
            ILogger<EmailBotBackgroundService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int interval = _configuration.GetValue<int>("EmailBotSettings:CheckIntervalMinutes", 2);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessInboxAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el ciclo del Email Bot.");
                }
                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
        }

        private async Task ProcessInboxAsync(CancellationToken stoppingToken)
        {
            var settings = _configuration.GetSection("EmailBotSettings");
            using var client = new ImapClient();

            await client.ConnectAsync(settings["ImapServer"], int.Parse(settings["ImapPort"]), true, stoppingToken);
            await client.AuthenticateAsync(settings["EmailAddress"], settings["Password"], stoppingToken);

            var inbox = client.Inbox;
            await inbox.OpenAsync(MailKit.FolderAccess.ReadWrite, stoppingToken);

            var query = SearchQuery.NotSeen.And(SearchQuery.SubjectContains(SubjectRequiredPrefix));
            var uids = await inbox.SearchAsync(query, stoppingToken);

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid, stoppingToken);

                if (!message.Subject.TrimStart().StartsWith(SubjectRequiredPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation($"Correo ignorado (Asunto no coincide): {message.Subject}");
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, stoppingToken);
                    continue;
                }

                // Extraemos el remitente y lo limpiamos
                var senderEmailRaw = message.From.Mailboxes.FirstOrDefault()?.Address;
                var normalizedSender = senderEmailRaw?.Trim().ToLower();

                using var scope = _scopeFactory.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var validatorService = scope.ServiceProvider.GetRequiredService<NotebookValidatorService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // 1. Validar Usuario
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedSender);

                if (user == null)
                {
                    await SendResponseAsync(senderEmailRaw, "Acceso Denegado", "<h3>Acceso Denegado</h3><p>Este correo no está registrado en el sistema.</p>", null, settings);
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, stoppingToken);
                    continue;
                }

                // 2. Extraer Adjuntos
                var validAttachments = message.Attachments
                    .OfType<MimePart>()
                    .Where(a => a.FileName.EndsWith(".py") || a.FileName.EndsWith(".ipynb") || a.FileName.EndsWith(".zip"))
                    .Select(a => (a.Content.Open(), a.FileName))
                    .ToList();

                if (!validAttachments.Any())
                {
                    await SendResponseAsync(senderEmailRaw, "Sin archivos", "<h3>Sin archivos válidos</h3><p>No se encontraron archivos .py, .ipynb o .zip válidos adjuntos.</p>", null, settings);
                }
                else
                {
                    // 3. Procesar con el motor centralizado
                    // CORRECCIÓN LÍNEA 121: Ajustamos nombres de variables y parámetros para coincidir con el resto del archivo
                    var (findings, count, _) = await validatorService.ProcessFilesAsync(validAttachments);

                    if (user.AnalysisQuota < count)
                    {
                        await SendResponseAsync(senderEmailRaw, "Cuota Insuficiente", $"<h3>Cuota Insuficiente</h3><p>El análisis requiere {count} créditos y solo tienes {user.AnalysisQuota}.</p>", null, settings);
                    }
                    else
                    {
                        // 4. Descontar Cuota y Registrar Análisis
                        user.AnalysisQuota -= count;
                        var run = new AnalysisRun
                        {
                            UserId = user.Id,
                            AnalysisTimestamp = DateTime.Now,
                            TotalFilesAnalyzed = count,
                            TotalProblemsFound = findings.Count,
                            ResultsJson = JsonSerializer.Serialize(findings)
                        };
                        context.AnalysisRuns.Add(run);
                        await context.SaveChangesAsync();
                        await userManager.UpdateAsync(user);

                        // 5. Generar Excel, construir HTML y responder
                        byte[] report = validatorService.GenerateExcelReportBytes(findings, run);

                        // Extraer solo los nombres de los archivos analizados
                        var fileNames = validAttachments.Select(a => a.Item2).ToList();
                        string summaryHtml = BuildSummaryHtml(fileNames, findings);

                        await SendResponseAsync(senderEmailRaw, "Análisis Completado", summaryHtml, report, settings);
                    }
                }
                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, stoppingToken);
            }
            await client.DisconnectAsync(true, stoppingToken);
        }

        // --- MÉTODO PARA GENERAR EL RESUMEN HTML ---
        private string BuildSummaryHtml(List<string> analyzedFiles, List<Finding> findings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style='font-family: Arial, sans-serif; color: #333;'>");
            sb.AppendLine("<h2 style='color: #0A192F;'>Resumen del Análisis de Notebooks</h2>");

            // Sección de Problemas Detectados
            sb.AppendLine("<div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; border: 1px solid #dee2e6; margin-bottom: 20px;'>");
            if (findings.Any())
            {
                sb.AppendLine("<table width='100%' cellpadding='8' cellspacing='0' style='border-collapse: collapse;'>");

                var groupedFindings = findings.GroupBy(f => f.FindingType).OrderByDescending(g => g.Count());
                foreach (var group in groupedFindings)
                {
                    sb.AppendLine($@"
                        <tr style='border-bottom: 1px solid #dee2e6;'>
                            <td style='font-weight: bold; color: #495057;'>{group.Key}</td>
                            <td align='right'>
                                <span style='background-color: #dc3545; color: white; padding: 3px 8px; border-radius: 12px; font-weight: bold; font-size: 12px;'>
                                    {group.Count()}
                                </span>
                            </td>
                        </tr>");
                }

                // Total
                sb.AppendLine($@"
                    <tr style='background-color: #e9ecef;'>
                        <td style='font-weight: bold; color: #0A192F; font-size: 16px;'>Total de Problemas</td>
                        <td align='right' style='font-weight: bold; color: #0A192F; font-size: 16px;'>{findings.Count}</td>
                    </tr>");
                sb.AppendLine("</table>");
            }
            else
            {
                sb.AppendLine("<p style='color: #198754; font-weight: bold; margin: 0;'>¡Excelente! No se encontraron problemas en los notebooks analizados.</p>");
            }
            sb.AppendLine("</div>");

            // Sección de Archivos Analizados
            sb.AppendLine("<h3 style='color: #0A192F; border-bottom: 2px solid #ffc107; padding-bottom: 5px;'>Archivos Analizados (" + analyzedFiles.Count + ")</h3>");
            sb.AppendLine("<ul style='background-color: #f8f9fa; padding: 15px 15px 15px 35px; border-radius: 5px; border: 1px solid #dee2e6;'>");
            foreach (var file in analyzedFiles)
            {
                sb.AppendLine($"<li style='margin-bottom: 5px;'><code>{file}</code></li>");
            }
            sb.AppendLine("</ul>");

            sb.AppendLine("<br>");
            sb.AppendLine("<p><em>Adjunto a este correo encontrarás el reporte en formato Excel con el detalle de la celda y línea exacta de cada hallazgo.</em></p>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        private async Task SendResponseAsync(string to, string subject, string bodyHtml, byte[] excel, IConfigurationSection settings)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Bot Validador", settings["EmailAddress"]));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = $"RE: {subject}";

            var builder = new BodyBuilder { HtmlBody = bodyHtml };

            if (excel != null)
            {
                builder.Attachments.Add("Reporte_Notebooks.xlsx", excel, new ContentType("application", "vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
            }
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(settings["SmtpServer"], int.Parse(settings["SmtpPort"]), SecureSocketOptions.Auto);
            await smtp.AuthenticateAsync(settings["EmailAddress"], settings["Password"]);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
