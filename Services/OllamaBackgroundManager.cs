using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent; // Requerido para manejo seguro multihilo
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Services
{
    public class OllamaJob
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string UsuarioId { get; set; } = string.Empty; // Identifica al dueño del Job
        public string Estado { get; set; } = "Procesando en RAM/GPU...";
        public string? Resultado { get; set; }
        public bool Terminado { get; set; } = false;
        public bool HuboError { get; set; } = false;
        public bool Notificado { get; set; } = false; // Control de lectura local

        // Properties de rendimiento y telemetría
        public string? InicioFormateado { get; set; }
        public string? FinFormateado { get; set; }
        public string? TiempoTranscurrido { get; set; }
    }

    public class OllamaBackgroundManager
    {
        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;

        // Diccionario estático y seguro para hilos que guarda el historial global en la sesión de RAM
        private static readonly ConcurrentDictionary<string, OllamaJob> _todosLosJobs = new ConcurrentDictionary<string, OllamaJob>();

        public OllamaBackgroundManager(IMemoryCache cache, IServiceScopeFactory scopeFactory)
        {
            _cache = cache;
            _scopeFactory = scopeFactory;
        }

        // Nota: Fíjate que ahora recibe el Dictionary<string, string> archivos
        public string IniciarProceso(string instruccionUsuario, string sistemaPrompt, string usuarioId, Dictionary<string, string> archivos, string modeloIA)
        {
            var job = new OllamaJob
            {
                UsuarioId = usuarioId,
                InicioFormateado = DateTime.Now.ToString("HH:mm:ss")
            };

            _todosLosJobs[job.Id] = job;
            _cache.Set(job.Id, job, TimeSpan.FromHours(2));

            _ = Task.Run(async () =>
            {
                var cronometro = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var ollamaService = scope.ServiceProvider.GetRequiredService<OllamaTestService>();
                        var notificacionesService = scope.ServiceProvider.GetRequiredService<NotificacionesService>();
                        var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();

                        string resultadoFinal = "";
                        int cantidadArchivos = archivos?.Count ?? 0;

                        if (archivos != null && archivos.Count > 0)
                        {
                            string cuerpoDocumentacion = "";
                            int contador = 1;

                            foreach (var archivo in archivos)
                            {
                                job.Estado = $"Analizando archivo {contador}/{archivos.Count} ({archivo.Key}) con {modeloIA}...";

                                string sysPromptEstricto = (sistemaPrompt ?? "") +
                                    "\n\nREGLAS DE FORMATO ESTRICTAS:\n" +
                                    "1. Usa títulos H3 (###) para las secciones solicitadas.\n" +
                                    "2. Toda query SQL debe ir envuelta OBLIGATORIAMENTE en bloques Markdown (```sql ... ```).\n" +
                                    "3. No inventes código.\n" +
                                    "4. RESPONDE ÚNICA Y EXCLUSIVAMENTE EN ESPAÑOL.";

                                string promptArchivo = $"--- SCRIPT: {archivo.Key} ---\n{archivo.Value}\n\nINSTRUCCIÓN: {instruccionUsuario}";

                                string docScript = await ollamaService.GenerarTextoAsync(promptArchivo, sysPromptEstricto, modeloIA);

                                cuerpoDocumentacion += $"## Documento de Proceso: {archivo.Key}\n\n{docScript}\n\n---\n\n";
                                contador++;
                            }

                            job.Estado = "Generando Introducción y Conclusión Global...";

                            string promptIntro = $"Basado en los siguientes scripts, redacta una 'Introducción Global del Pipeline' (2 párrafos).\nREGLA: ESCRIBE TODO EL TEXTO ESTRICTAMENTE EN ESPAÑOL.\n\nRESUMEN:\n{cuerpoDocumentacion}";
                            string introGlobal = await ollamaService.GenerarTextoAsync(promptIntro, "Eres Arquitecto de Datos. Redacta profesionalmente. RESPONDE SIEMPRE EN ESPAÑOL.", modeloIA);

                            string promptConclusion = $"Basado en los siguientes scripts, redacta una 'Conclusión General' (2 párrafos).\nREGLA: ESCRIBE TODO EL TEXTO ESTRICTAMENTE EN ESPAÑOL.\n\nRESUMEN:\n{cuerpoDocumentacion}";
                            string conclusionGlobal = await ollamaService.GenerarTextoAsync(promptConclusion, "Eres Arquitecto de Datos. Redacta profesionalmente. RESPONDE SIEMPRE EN ESPAÑOL.", modeloIA);

                            resultadoFinal = $"# Documentación Oficial\n\n### Introducción Global\n{introGlobal}\n\n---\n\n{cuerpoDocumentacion}### Conclusión General\n{conclusionGlobal}";
                        }
                        else
                        {
                            job.Estado = $"Procesando consulta general con {modeloIA}...";
                            string sysPromptGeneral = string.IsNullOrWhiteSpace(sistemaPrompt) ? "Responde siempre en español." : sistemaPrompt + " Responde siempre en español.";
                            resultadoFinal = await ollamaService.GenerarTextoAsync(instruccionUsuario, sysPromptGeneral, modeloIA);
                        }

                        cronometro.Stop();
                        job.FinFormateado = DateTime.Now.ToString("HH:mm:ss");
                        var ts = cronometro.Elapsed;
                        job.TiempoTranscurrido = ts.TotalMinutes >= 1 ? $"{(int)ts.TotalMinutes} min {ts.Seconds} seg" : $"{ts.Seconds} seg";
                        job.Resultado = resultadoFinal;
                        job.Estado = "Completado";
                        job.Terminado = true;

                        // ---------------------------------------------------------
                        // NUEVO: CREACIÓN DE JSON ESTRUCTURADO PARA AUDITORÍA
                        // ---------------------------------------------------------
                        var detalleAuditoria = new
                        {
                            ID_Ticket = job.Id,
                            Modelo_Usado = modeloIA,
                            Archivos_Procesados = cantidadArchivos,
                            Tiempo_Transcurrido = job.TiempoTranscurrido,
                            Estado_Final = "Completado Exitosamente"
                        };

                        string jsonDetalle = System.Text.Json.JsonSerializer.Serialize(detalleAuditoria);

                        await auditService.LogActionAsync(usuarioId, "ANALISIS_IA_COMPLETADO", jsonDetalle, null, job.Id);

                        await notificacionesService.EnviarAsync(usuarioId, "IaCompletada", "🧠 Análisis Finalizado", $"Procesado en {job.TiempoTranscurrido}.", $"/OllamaTest/Index?jobId={job.Id}", null);
                    }
                }
                catch (Exception ex)
                {
                    cronometro.Stop();
                    job.FinFormateado = DateTime.Now.ToString("HH:mm:ss");
                    var ts = cronometro.Elapsed;
                    job.TiempoTranscurrido = ts.TotalMinutes >= 1 ? $"{(int)ts.TotalMinutes} min {ts.Seconds} seg" : $"{ts.Seconds} seg";
                    job.Terminado = true;
                    job.HuboError = true;
                    job.Estado = "Error";
                    job.Resultado = $"Error: {ex.Message}";

                    try
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var notificacionesService = scope.ServiceProvider.GetRequiredService<NotificacionesService>();
                            var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();

                            // ---------------------------------------------------------
                            // NUEVO: CREACIÓN DE JSON ESTRUCTURADO PARA ERRORES
                            // ---------------------------------------------------------
                            var detalleError = new
                            {
                                ID_Ticket = job.Id,
                                Modelo_Usado = modeloIA,
                                Tiempo_Transcurrido = job.TiempoTranscurrido,
                                Estado_Final = "Error Crítico",
                                Mensaje_Sistema = ex.Message
                            };

                            string jsonError = System.Text.Json.JsonSerializer.Serialize(detalleError);

                            await auditService.LogActionAsync(usuarioId, "ANALISIS_IA_ERROR", jsonError, null, job.Id);
                            await notificacionesService.EnviarAsync(usuarioId, "ValidacionRechazada", "❌ Error en Análisis de IA", $"Fallo a los {job.TiempoTranscurrido}.", null, null);
                        }
                    }
                    catch { }
                }
            });

            return job.Id;
        }

        public OllamaJob? ObtenerEstado(string id)
        {
            _cache.TryGetValue(id, out OllamaJob? job);
            return job;
        }

        public OllamaJob? ObtenerEstadoSeguro(string id, string usuarioId)
        {
            var job = ObtenerEstado(id);
            if (job != null && job.UsuarioId != usuarioId)
            {
                return null;
            }
            return job;
        }

        public List<OllamaJob> ObtenerHistorialUsuario(string usuarioId)
        {
            return _todosLosJobs.Values
                .Where(j => j.UsuarioId == usuarioId)
                .OrderByDescending(j => j.Id)
                .ToList();
        }

        public int ContarTareasCompletadasNoNotificadas(string usuarioId)
        {
            return _todosLosJobs.Values
                .Count(j => j.UsuarioId == usuarioId && j.Terminado && !j.Notificado);
        }

        public void MarcarNotificacionesLeidas(string usuarioId)
        {
            var tareasNoLeidas = _todosLosJobs.Values
                .Where(j => j.UsuarioId == usuarioId && j.Terminado && !j.Notificado);

            foreach (var t in tareasNoLeidas)
            {
                t.Notificado = true;
            }
        }
    }
}
