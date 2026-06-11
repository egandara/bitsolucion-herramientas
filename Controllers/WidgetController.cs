using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration; // Aseguramos el espacio de nombres para IConfiguration
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    public class PartidoLocal
    {
        public string Home { get; set; }
        public string Away { get; set; }
        public string StartTime { get; set; }
        public int? GolesHome { get; set; }
        public int? GolesAway { get; set; }
    }

    public class PartidoDisplayModel
    {
        public string Home { get; set; }
        public string Away { get; set; }
        public DateTimeOffset Time { get; set; }
        public string Key { get; set; }
        public int? GolesH { get; set; }
        public int? GolesA { get; set; }
        public PartidoLocal RawRef { get; set; }
    }

    [AllowAnonymous]
    public class WidgetController : Controller
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private static readonly object FileLock = new object();

        private static readonly Dictionary<string, (string Code, string Flag)> DiccionarioFIFA = new Dictionary<string, (string Code, string Flag)>(StringComparer.OrdinalIgnoreCase)
        {
            { "Mexico", ("MEX", "🇲🇽") },
            { "South Africa", ("RSA", "🇿🇦") },
            { "Rep. of Korea", ("KOR", "🇰🇷") },
            { "Czech Rep.", ("CZE", "🇨🇿") },
            { "Canada", ("CAN", "🇨🇦") },
            { "Bosnia/Herzeg.", ("BIH", "🇧🇦") },
            { "USA", ("USA", "🇺🇸") },
            { "Paraguay", ("PAR", "🇵🇾") },
            { "Qatar", ("QAT", "🇶🇦") },
            { "Switzerland", ("SUI", "🇨🇭") },
            { "Brazil", ("BRA", "🇧🇷") },
            { "Morocco", ("MAR", "🇲🇦") },
            { "Haiti", ("HAI", "🇭🇹") },
            { "Scotland", ("SCO", "🏴󠁧󠁢󠁳󠁣󠁴󠁿") },
            { "Australia", ("AUS", "🇦🇺") },
            { "Turkey", ("TUR", "🇹🇷") },
            { "Germany", ("GER", "🇩🇪") },
            { "Curaçao", ("CUW", "🇨🇼") },
            { "Ivory Coast", ("CIV", "🇨🇮") },
            { "Ecuador", ("ECU", "🇪🇨") },
            { "Netherlands", ("NED", "🇳🇱") },
            { "Japan", ("JPN", "🇯🇵") },
            { "Sweden", ("SWE", "🇸🇪") },
            { "Tunisia", ("TUN", "🇹🇳") },
            { "Belgium", ("BEL", "🇧🇪") },
            { "Egypt", ("EGY", "🇪🇬") },
            { "IR Iran", ("IRN", "🇮🇷") },
            { "New Zealand", ("NZL", "🇳🇿") },
            { "Spain", ("ESP", "🇪🇸") },
            { "Cape Verde", ("CPV", "🇨🇻") },
            { "Saudi Arabia", ("KSA", "🇸🇦") },
            { "Uruguay", ("URU", "🇺🇾") },
            { "France", ("FRA", "🇫🇷") },
            { "Senegal", ("SEN", "🇸🇳") },
            { "Iraq", ("IRQ", "🇮🇶") },
            { "Norway", ("NOR", "🇳🇴") },
            { "Argentina", ("ARG", "🇦🇷") },
            { "Algeria", ("ALG", "🇩🇿") },
            { "Austria", ("AUT", "🇦🇹") },
            { "Jordan", ("JOR", "🇯🇴") },
            { "Portugal", ("POR", "🇵🇹") },
            { "DR Congo", ("COD", "🇨🇩") },
            { "Uzbekistan", ("UZB", "🇺🇿") },
            { "Colombia", ("COL", "🇨🇴") },
            { "England", ("ENG", "🏴󠁧󠁢󠁥󠁮󠁧󠁿") },
            { "Croatia", ("CRO", "🇭🇷") },
            { "Ghana", ("GHA", "🇬🇭") },
            { "Panama", ("PAN", "🇵🇦") }
        };

        // CORREGIDO: Ahora sí incluimos IConfiguration config en la firma
        public WidgetController(IMemoryCache cache, IWebHostEnvironment env, IConfiguration config)
        {
            _cache = cache;
            _env = env;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> GetWorldCupWidget()
        {
            string apiKey = _config["WidgetSettings:ApiKey"];
            string apiCacheKey = "LiveWorldCupApiCache_V9";
            DateTimeOffset now = DateTimeOffset.Now;

            string jsonPath = Path.Combine(_env.WebRootPath, "data", "partidos.json");
            if (!System.IO.File.Exists(jsonPath))
                return Json(new { error = "Archivo partidos.json no encontrado" });

            string jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
            var calendarioRaw = JsonSerializer.Deserialize<List<PartidoLocal>>(jsonContent, options);

            var todosLosPartidos = calendarioRaw
                .Select(p => new PartidoDisplayModel
                {
                    Home = p.Home,
                    Away = p.Away,
                    Time = DateTimeOffset.Parse(p.StartTime),
                    Key = $"{p.Home}_{p.Away}".ToLower().Replace(" ", ""),
                    GolesH = p.GolesHome,
                    GolesA = p.GolesAway,
                    RawRef = p
                })
                .OrderBy(p => p.Time)
                .ToList();

            if (!todosLosPartidos.Any())
                return Json(new { error = "Fixture vacío" });

            int actualIdx = todosLosPartidos.FindLastIndex(p => now >= p.Time);
            if (actualIdx == -1) actualIdx = 0;

            if (actualIdx < todosLosPartidos.Count - 1)
            {
                double minsDesdeInicio = (now - todosLosPartidos[actualIdx].Time).TotalMinutes;
                if (minsDesdeInicio > 135) actualIdx++;
            }

            int antIdx = actualIdx - 1;
            int sigIdx = actualIdx + 1;

            var pAnterior = antIdx >= 0 ? todosLosPartidos[antIdx] : null;
            var pActual = todosLosPartidos[actualIdx];
            var pSiguiente = sigIdx < todosLosPartidos.Count ? todosLosPartidos[sigIdx] : null;

            double minsActual = (now - pActual.Time).TotalMinutes;
            bool necesitaLlamarApi = minsActual >= 5 && minsActual <= 135;
            JsonElement apiResponseRows = default;
            bool huboCambiosEnGoles = false;

            if (necesitaLlamarApi)
            {
                if (!_cache.TryGetValue(apiCacheKey, out string rawJson))
                {
                    try
                    {
                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("x-rapidapi-host", "v3.football.api-sports.io");
                        client.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);

                        string apiDateStr = pActual.Time.ToString("yyyy-MM-dd");
                        var response = await client.GetAsync($"https://v3.football.api-sports.io/fixtures?league=1&season=2026&date={apiDateStr}");
                        rawJson = await response.Content.ReadAsStringAsync();

                        _cache.Set(apiCacheKey, rawJson, TimeSpan.FromMinutes(5));
                    }
                    catch { rawJson = null; }
                }

                if (!string.IsNullOrEmpty(rawJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(rawJson);
                        if (doc.RootElement.TryGetProperty("response", out var respProp)) apiResponseRows = respProp.Clone();
                    }
                    catch { }
                }
            }

            var responseObj = new
            {
                anterior = ConstruirHtmlTarjeta(pAnterior, "ANTERIOR", now, apiResponseRows, ref huboCambiosEnGoles),
                actual = ConstruirHtmlTarjeta(pActual, "ACTUAL", now, apiResponseRows, ref huboCambiosEnGoles),
                siguiente = ConstruirHtmlTarjeta(pSiguiente, "SIGUIENTE", now, apiResponseRows, ref huboCambiosEnGoles)
            };

            if (huboCambiosEnGoles)
            {
                lock (FileLock)
                {
                    try
                    {
                        string updatedJson = JsonSerializer.Serialize(calendarioRaw, options);
                        System.IO.File.WriteAllText(jsonPath, updatedJson);
                    }
                    catch { }
                }
            }

            return Json(responseObj);
        }

        private string ConstruirHtmlTarjeta(PartidoDisplayModel p, string bloque, DateTimeOffset now, JsonElement apiResponseRows, ref bool huboCambios)
        {
            if (p == null) return "<span class='text-white-50 opacity-25'>--</span>";

            double m = (now - p.Time).TotalMinutes;

            var elLocal = NormalizarEquipo(p.Home);
            var elVisitante = NormalizarEquipo(p.Away);

            string scoreVisual = (p.GolesH != null && p.GolesA != null) ? $"{p.GolesH} - {p.GolesA}" : "0 - 0";

            if (m < 0)
            {
                string horaStr = p.Time.ToLocalTime().ToString("HH:mm");
                string fechaStr = p.Time.ToLocalTime().ToString("dd/MM");
                return $"<span class='wc-time-badge-future'>{fechaStr} {horaStr}</span> {elLocal.Flag} <span class='fw-bold text-white-50'>{elLocal.Name}</span> <span class='mx-1 opacity-25 text-white-50'>vs</span> <span class='fw-bold text-white-50'>{elVisitante.Name}</span> {elVisitante.Flag}";
            }

            if (m >= 0 && m < 5 && bloque == "ACTUAL")
            {
                int elapsedSimulado = (int)m == 0 ? 1 : (int)m;
                return $"<span class='wc-live-pulse-badge'>🔴 {elapsedSimulado}'</span> {elLocal.Flag} <span class='fw-bold'>{elLocal.Name}</span> <span class='wc-score-live-highlight'>0 - 0</span> <span class='fw-bold'>{elVisitante.Name}</span> {elVisitante.Flag}";
            }

            if (apiResponseRows.ValueKind == JsonValueKind.Array)
            {
                foreach (var row in apiResponseRows.EnumerateArray())
                {
                    try
                    {
                        string apiHome = row.GetProperty("teams").GetProperty("home").GetProperty("name").GetString().ToLower();
                        if (apiHome.Contains(p.Home.ToLower()) || p.Home.ToLower().Contains(apiHome))
                        {
                            string shortStatus = row.GetProperty("fixture").GetProperty("status").GetProperty("short").GetString();
                            var goalsHome = row.GetProperty("goals").GetProperty("home");
                            var goalsAway = row.GetProperty("goals").GetProperty("away");

                            if (goalsHome.ValueKind != JsonValueKind.Null && goalsAway.ValueKind != JsonValueKind.Null)
                            {
                                int apiGolesH = goalsHome.GetInt32();
                                int apiGolesA = goalsAway.GetInt32();

                                if (p.GolesH != apiGolesH || p.GolesA != apiGolesA)
                                {
                                    p.RawRef.GolesHome = apiGolesH;
                                    p.RawRef.GolesAway = apiGolesA;
                                    huboCambios = true;
                                    scoreVisual = $"{apiGolesH} - {apiGolesA}";
                                }
                            }

                            if (shortStatus == "FT" || shortStatus == "PEN" || shortStatus == "AET")
                            {
                                return $"<span class='wc-fin-badge'>Fin</span> {elLocal.Flag} <span class='fw-bold text-white-50'>{elLocal.Name}</span> <span class='wc-score-past-badge'>{scoreVisual}</span> <span class='fw-bold text-white-50'>{elVisitante.Name}</span> {elVisitante.Flag}";
                            }
                            else
                            {
                                var elapsedProp = row.GetProperty("fixture").GetProperty("status").GetProperty("elapsed");
                                int minReal = elapsedProp.ValueKind != JsonValueKind.Null ? elapsedProp.GetInt32() : (int)m;
                                return $"<span class='wc-live-pulse-badge'>🔴 {minReal}'</span> {elLocal.Flag} <span class='fw-bold'>{elLocal.Name}</span> <span class='wc-score-live-highlight'>{scoreVisual}</span> <span class='fw-bold'>{elVisitante.Name}</span> {elVisitante.Flag}";
                            }
                        }
                    }
                    catch { }
                }
            }

            if (m > 135)
            {
                return $"<span class='wc-fin-badge'>Fin</span> {elLocal.Flag} <span class='fw-bold text-white-50'>{elLocal.Name}</span> <span class='wc-score-past-badge'>{scoreVisual}</span> <span class='fw-bold text-white-50'>{elVisitante.Name}</span> {elVisitante.Flag}";
            }
            else
            {
                int minFallback = m > 90 ? 90 : (int)m;
                return $"<span class='wc-live-pulse-badge'>🔴 {minFallback}'</span> {elLocal.Flag} <span class='fw-bold'>{elLocal.Name}</span> <span class='wc-score-live-highlight'>{scoreVisual}</span> <span class='fw-bold'>{elVisitante.Name}</span> {elVisitante.Flag}";
            }
        }

        private (string Name, string Flag) NormalizarEquipo(string nombreOriginal)
        {
            if (DiccionarioFIFA.TryGetValue(nombreOriginal, out var match))
            {
                return (match.Code, match.Flag);
            }
            string clean = nombreOriginal.Replace(".", "").Replace(" ", "");
            string codeFallback = clean.Length > 3 ? clean.Substring(0, 3).ToUpper() : clean.ToUpper();
            return (codeFallback, "🏳️");
        }

        [HttpGet]
        public async Task<IActionResult> TestApiSportsConnection()
        {
            // Esto leerá la API Key directamente desde tu appsettings.json para verificar que el flujo esté bien configurado
            string apiKey = _config["WidgetSettings:ApiKey"];

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-rapidapi-host", "v3.football.api-sports.io");
                client.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);

                // Consultamos el endpoint oficial de estado de cuenta (siempre responde si la key es válida)
                var response = await client.GetAsync("https://v3.football.api-sports.io/status");
                string rawJson = await response.Content.ReadAsStringAsync();

                return Content(rawJson, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { error = "Fallo de conexión física", detalle = ex.Message });
            }
        }
    }
}
