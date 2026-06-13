using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions; // 💡 LIBRERÍA AGREGADA PARA EL SCRAPING
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    // 💡 MODELOS DE ESTADIOS
    public class StadiumModel
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("country_en")] public string CountryEn { get; set; }
        [JsonPropertyName("region")] public string Region { get; set; }
    }

    public class RootStadiumsContainer
    {
        [JsonPropertyName("stadiums")] public List<StadiumModel> Stadiums { get; set; }
    }

    public class PartidoLocal
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("group")] public string Group { get; set; }
        [JsonPropertyName("local_date")] public string LocalDate { get; set; }
        [JsonPropertyName("stadium_id")] public string StadiumId { get; set; }
        [JsonPropertyName("finished")] public string Finished { get; set; }
        [JsonPropertyName("time_elapsed")] public string TimeElapsed { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("home_team_name_en")] public string HomeTeamNameEn { get; set; }
        [JsonPropertyName("away_team_name_en")] public string AwayTeamNameEn { get; set; }
        [JsonPropertyName("home_team_label")] public string HomeTeamLabel { get; set; }
        [JsonPropertyName("away_team_label")] public string AwayTeamLabel { get; set; }
        [JsonPropertyName("home_score")] public string HomeScore { get; set; }
        [JsonPropertyName("away_score")] public string AwayScore { get; set; }
        [JsonPropertyName("home_scorers")] public string HomeScorers { get; set; }
        [JsonPropertyName("away_scorers")] public string AwayScorers { get; set; }
    }

    public class RootGamesContainer
    {
        [JsonPropertyName("games")] public List<PartidoLocal> Games { get; set; }
    }

    public class WidgetMatchModel
    {
        public string Id { get; set; }
        public string HomeCode { get; set; }
        public string HomeFlag { get; set; }
        public string AwayCode { get; set; }
        public string AwayFlag { get; set; }
        public string ScoreVisual { get; set; }
        public string StatusBadgeHtml { get; set; }
        public List<string> HomeScorersList { get; set; }
        public List<string> AwayScorersList { get; set; }
        public bool HasDetails { get; set; }
    }

    public class WidgetTimeGroup
    {
        public string TimeLabel { get; set; }
        public List<WidgetMatchModel> Matches { get; set; }
    }

    public class WidgetDateGroup
    {
        public string DateLabel { get; set; }
        public List<WidgetTimeGroup> Times { get; set; }
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
            { "Mexico", ("MEX", "🇲🇽") }, { "South Africa", ("RSA", "🇿🇦") },
            { "South Korea", ("KOR", "🇰🇷") }, { "Czech Republic", ("CZE", "🇨🇿") },
            { "Canada", ("CAN", "🇨🇦") }, { "Bosnia and Herzegovina", ("BIH", "🇧🇦") },
            { "United States", ("USA", "🇺🇸") }, { "Paraguay", ("PAR", "🇵🇾") },
            { "Qatar", ("QAT", "🇶🇦") }, { "Switzerland", ("SUI", "🇨🇭") },
            { "Brazil", ("BRA", "🇧🇷") }, { "Morocco", ("MAR", "🇲🇦") },
            { "Haiti", ("HAI", "🇭🇹") }, { "Scotland", ("SCO", "🏴󠁧󠁢󠁳󠁣󠁴󠁿") },
            { "Australia", ("AUS", "🇦🇺") }, { "Turkey", ("TUR", "🇹🇷") },
            { "Germany", ("GER", "🇩🇪") }, { "Curaçao", ("CUW", "🇨🇼") },
            { "Ivory Coast", ("CIV", "🇨🇮") }, { "Ecuador", ("ECU", "🇪🇨") },
            { "Netherlands", ("NED", "🇳🇱") }, { "Japan", ("JPN", "🇯🇵") },
            { "Sweden", ("SWE", "🇸🇪") }, { "Tunisia", ("TUN", "🇹🇳") },
            { "Belgium", ("BEL", "🇧🇪") }, { "Egypt", ("EGY", "🇪🇬") },
            { "Iran", ("IRN", "🇮🇷") }, { "New Zealand", ("NZL", "🇳🇿") },
            { "Spain", ("ESP", "🇪🇸") }, { "Cape Verde", ("CPV", "🇨🇻") },
            { "Saudi Arabia", ("KSA", "🇸🇦") }, { "Uruguay", ("URU", "🇺🇾") },
            { "France", ("FRA", "🇫🇷") }, { "Senegal", ("SEN", "🇸🇳") },
            { "Iraq", ("IRQ", "🇮🇶") }, { "Norway", ("NOR", "🇳🇴") },
            { "Argentina", ("ARG", "🇦🇷") }, { "Algeria", ("ALG", "🇩🇿") },
            { "Austria", ("AUT", "🇦🇹") }, { "Jordan", ("JOR", "🇯🇴") },
            { "Portugal", ("POR", "🇵🇹") }, { "Democratic Republic of the Congo", ("COD", "🇨🇩") },
            { "Uzbekistan", ("UZB", "🇺🇿") }, { "Colombia", ("COL", "🇨🇴") },
            { "England", ("ENG", "🏴󠁧󠁢󠁥󠁮󠁧󠁿") }, { "Croatia", ("CRO", "🇭🇷") },
            { "Ghana", ("GHA", "🇬🇭") }, { "Panama", ("PAN", "🇵🇦") }
        };

        public WidgetController(IMemoryCache cache, IWebHostEnvironment env, IConfiguration config)
        {
            _cache = cache;
            _env = env;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> GetWorldCupWidget()
        {
            string remoteJsonCacheKey = "RemoteFixtureJsonString_V12";
            string remoteStadiumsCacheKey = "RemoteStadiumsJsonString_V1";
            string localJsonPath = Path.Combine(_env.WebRootPath, "data", "partidos.json");
            string jsonContent = string.Empty;
            string stadiumsJson = string.Empty;

            // 1. OBTENER ESTADIOS (Caché de 24 horas)
            if (!_cache.TryGetValue(remoteStadiumsCacheKey, out stadiumsJson))
            {
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    stadiumsJson = await client.GetStringAsync("https://worldcup26.ir/get/stadiums");
                    _cache.Set(remoteStadiumsCacheKey, stadiumsJson, TimeSpan.FromHours(24));
                }
                catch { }
            }

            var dictEstadios = new Dictionary<string, StadiumModel>();
            if (!string.IsNullOrEmpty(stadiumsJson))
            {
                try
                {
                    var rootStadiums = JsonSerializer.Deserialize<RootStadiumsContainer>(stadiumsJson);
                    if (rootStadiums?.Stadiums != null)
                    {
                        dictEstadios = rootStadiums.Stadiums.ToDictionary(s => s.Id, s => s);
                    }
                }
                catch { }
            }

            // 2. OBTENER PARTIDOS (Caché de 2 minutos)
            if (!_cache.TryGetValue(remoteJsonCacheKey, out jsonContent))
            {
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    jsonContent = await client.GetStringAsync("https://worldcup26.ir/get/games");

                    _cache.Set(remoteJsonCacheKey, jsonContent, TimeSpan.FromMinutes(2));

                    lock (FileLock)
                    {
                        string dir = Path.GetDirectoryName(localJsonPath);
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        System.IO.File.WriteAllText(localJsonPath, jsonContent);
                    }
                }
                catch
                {
                    if (System.IO.File.Exists(localJsonPath))
                        jsonContent = await System.IO.File.ReadAllTextAsync(localJsonPath);
                }
            }

            if (string.IsNullOrEmpty(jsonContent))
                return Json(new { error = "No se pudo obtener el fixture." });

            var root = JsonSerializer.Deserialize<RootGamesContainer>(jsonContent);
            if (root?.Games == null || !root.Games.Any())
                return Json(new { error = "Formato inválido" });

            DateTime now = ObtenerHoraActualChile();

            // 💡 3. DETECTAR SI HAY PARTIDO EN VIVO PARA AHORRAR RECURSOS DE SCRAPING
            bool hayPartidoEnVivo = root.Games.Any(p => {
                dictEstadios.TryGetValue(p.StadiumId ?? "", out var estadioLocal);
                DateTime pTime = CalcularHoraChile(p.LocalDate, estadioLocal);
                double m = (now - pTime).TotalMinutes;
                return m >= -5 && m <= 150 && (p.Finished != "TRUE" && p.TimeElapsed != "finished");
            });

            // 💡 4. SCRAPING PEREZOSO: Leer la página de Zafronix (Caché de 1 Minuto)
            string minutoScrapeado = "";
            if (hayPartidoEnVivo)
            {
                string cacheKeyLivePage = "ZafronixLivePage_V1";
                if (!_cache.TryGetValue(cacheKeyLivePage, out string htmlZafronix))
                {
                    try
                    {
                        using var client = new HttpClient();
                        client.Timeout = TimeSpan.FromSeconds(4); // Rápido para no trabar la app
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
                        htmlZafronix = await client.GetStringAsync("https://api.zafronix.com/live");

                        _cache.Set(cacheKeyLivePage, htmlZafronix, TimeSpan.FromMinutes(1));
                    }
                    catch { } // Falla silenciosa si Zafronix se cae
                }

                if (!string.IsNullOrEmpty(htmlZafronix))
                {
                    // Regex nivel "Hacker": Busca en textos visibles y en variables de JavaScript ocultas
                    var match = Regex.Match(htmlZafronix, @"(?:liveMinute|""liveMinute"")\s*[:=]\s*(\d+)|LIVE\s*(\d+)|""status""\s*:\s*""live"".*?(?:minute|liveMinute)""?\s*:\s*(\d+)", RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        if (match.Groups[1].Success) minutoScrapeado = match.Groups[1].Value;
                        else if (match.Groups[2].Success) minutoScrapeado = match.Groups[2].Value;
                        else if (match.Groups[3].Success) minutoScrapeado = match.Groups[3].Value;
                    }
                }
            }


            // 5. PROCESAR DATOS Y CRUZAR ZONAS HORARIAS
            var todosLosPartidosMapeados = root.Games.Select(p => {

                dictEstadios.TryGetValue(p.StadiumId ?? "", out var estadioLocal);
                DateTime pTime = CalcularHoraChile(p.LocalDate, estadioLocal);
                double m = (now - pTime).TotalMinutes;

                var elLocal = NormalizarEquipo(p.HomeTeamNameEn, p.HomeTeamLabel);
                var elVisitante = NormalizarEquipo(p.AwayTeamNameEn, p.AwayTeamLabel);

                string score = $"{p.HomeScore} - {p.AwayScore}";
                string badge = "";

                bool isFinished = p.Finished == "TRUE" || p.TimeElapsed == "finished";

                // Respaldo: Si pasaron más de 150 min y no dice "live", lo cerramos
                if (!isFinished && m > 150 && p.TimeElapsed != "live")
                {
                    isFinished = true;
                }

                // El JSON manda: si dice live, es live.
                bool isLive = p.TimeElapsed == "live" || (m >= 0 && !isFinished);

                if (isFinished)
                {
                    badge = "<span class='wc-fin-badge'>Fin</span>";
                }
                else if (isLive)
                {
                    string badgeText = "";
                    string badgeIcon = "🔴";

                    // 💡 Prioridad 1: El minuto real Scrapeado de Zafronix
                    if (!string.IsNullOrEmpty(minutoScrapeado) && m >= -2 && m <= 150)
                    {
                        badgeText = minutoScrapeado + "'";

                        // Si se queda pegado en 45 o 46 y el partido real ya va por la mitad, asumimos HT
                        if (minutoScrapeado == "45" || minutoScrapeado == "46" || minutoScrapeado == "47")
                        {
                            int minReal = (int)m;
                            if (minReal > 50 && minReal <= 70)
                            {
                                badgeText = "HT";
                                badgeIcon = "⏸️";
                            }
                        }
                    }
                    // 💡 Prioridad 2: Si la API manda el minuto exacto, lo respetamos
                    else if (!string.IsNullOrEmpty(p.TimeElapsed) && p.TimeElapsed != "live" && p.TimeElapsed != "notstarted")
                    {
                        badgeText = p.TimeElapsed.Contains("'") ? p.TimeElapsed : p.TimeElapsed + "'";
                        if (p.TimeElapsed.Equals("ht", StringComparison.OrdinalIgnoreCase) || p.TimeElapsed.Equals("halftime", StringComparison.OrdinalIgnoreCase))
                        {
                            badgeText = "HT";
                            badgeIcon = "⏸️";
                        }
                    }
                    // 💡 Prioridad 3: El cálculo avanzado de fútbol matemático (Nuestro salvavidas)
                    else
                    {
                        int minReal = (int)m;
                        if (minReal <= 0) minReal = 1;

                        if (minReal <= 50)
                        {
                            badgeText = minReal + "'"; // Primer tiempo (hasta 50 mins por descuentos)
                        }
                        else if (minReal > 50 && minReal <= 70)
                        {
                            badgeText = "HT"; // Entretiempo de 20 mins
                            badgeIcon = "⏸️";
                        }
                        else if (minReal > 70 && minReal <= 115)
                        {
                            badgeText = (minReal - 25) + "'"; // Segundo tiempo (Ej: min 71 real - 25 = 46' del partido)
                        }
                        else
                        {
                            badgeText = "90+'"; // Tiempo cumplido
                        }
                    }

                    // Inyectar un color ámbar/naranja si están en el entretiempo
                    if (badgeText == "HT")
                    {
                        badge = $"<span class='wc-live-pulse-badge' style='background: rgba(255, 193, 7, 0.15); color: #ffc107; border-color: rgba(255, 193, 7, 0.4);'>{badgeIcon} {badgeText}</span>";
                    }
                    else
                    {
                        badge = $"<span class='wc-live-pulse-badge'>{badgeIcon} {badgeText}</span>";
                    }
                }
                else
                {
                    badge = "<span class='wc-future-badge'>Próx</span>";
                }

                return new
                {
                    ParsedDate = pTime.Date,
                    TimeStr = pTime.ToString("HH:mm"),
                    MatchData = new WidgetMatchModel
                    {
                        Id = p.Id,
                        HomeCode = elLocal.Code,
                        HomeFlag = elLocal.Flag,
                        AwayCode = elVisitante.Code,
                        AwayFlag = elVisitante.Flag,
                        ScoreVisual = (m < 0 && !isFinished && !isLive) ? "vs" : score,
                        StatusBadgeHtml = badge,
                        HomeScorersList = LimpiarGoleadores(p.HomeScorers),
                        AwayScorersList = LimpiarGoleadores(p.AwayScorers),
                        HasDetails = isFinished || isLive
                    }
                };
            }).ToList();

            // 6. AGRUPACIÓN Y ORDENAMIENTO
            var estructuraAgrupada = todosLosPartidosMapeados
                .GroupBy(p => p.ParsedDate)
                .OrderBy(g => g.Key)
                .Select(gDate => new WidgetDateGroup
                {
                    DateLabel = gDate.Key.ToString("dddd dd/MM", new System.Globalization.CultureInfo("es-ES")).ToUpper(),
                    Times = gDate
                        .GroupBy(t => t.TimeStr)
                        .OrderBy(tGrp => tGrp.Key)
                        .Select(gTime => new WidgetTimeGroup
                        {
                            TimeLabel = gTime.Key,
                            Matches = gTime.Select(m => m.MatchData).ToList()
                        }).ToList()
                }).ToList();

            int indexFechaActiva = Math.Max(0, estructuraAgrupada.FindIndex(d => d.DateLabel.Contains(now.ToString("dd/MM"))));
            if (indexFechaActiva == 0) indexFechaActiva = Math.Max(0, estructuraAgrupada.FindLastIndex(d => now.Date >= DateTime.ParseExact(d.DateLabel.Split(' ')[1], "dd/MM", null).Date));

            return Json(new
            {
                dates = estructuraAgrupada,
                currentDateIndex = indexFechaActiva
            });
        }

        // 💡 MOTOR DINÁMICO DE ZONAS HORARIAS (Convierte a Chile)
        private DateTime CalcularHoraChile(string localDateStr, StadiumModel estadio)
        {
            DateTime parsedDate = DateTime.ParseExact(localDateStr, "MM/dd/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            int offsetHoras = -4; // Por defecto Eastern

            if (estadio != null)
            {
                if (estadio.Region == "Western") offsetHoras = -7;
                else if (estadio.Region == "Eastern") offsetHoras = -4;
                else if (estadio.Region == "Central")
                {
                    if (estadio.CountryEn == "Mexico") offsetHoras = -6; // México no tiene horario de verano
                    else offsetHoras = -5; // Central USA
                }
            }

            DateTimeOffset dto = new DateTimeOffset(parsedDate, TimeSpan.FromHours(offsetHoras));

            try
            {
                TimeZoneInfo chileZone = TimeZoneInfo.FindSystemTimeZoneById(
                    System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                    ? "Pacific SA Standard Time"
                    : "America/Santiago");
                return TimeZoneInfo.ConvertTime(dto, chileZone).DateTime;
            }
            catch { return dto.ToLocalTime().DateTime; }
        }

        private DateTime ObtenerHoraActualChile()
        {
            try
            {
                TimeZoneInfo chileZone = TimeZoneInfo.FindSystemTimeZoneById(
                    System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                    ? "Pacific SA Standard Time" : "America/Santiago");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chileZone);
            }
            catch { return DateTime.Now; }
        }

        private List<string> LimpiarGoleadores(string raw)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(raw) || raw.Equals("null", StringComparison.OrdinalIgnoreCase))
                return result;

            string clean = raw.Replace("{", "").Replace("}", "").Replace("\"", "").Replace("“", "").Replace("”", "");
            var items = clean.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                string trimmed = item.Trim();
                if (!string.IsNullOrEmpty(trimmed)) result.Add(trimmed);
            }
            return result;
        }

        private (string Code, string Flag) NormalizarEquipo(string nombre, string label)
        {
            string target = !string.IsNullOrEmpty(nombre) ? nombre : label;
            if (string.IsNullOrEmpty(target)) return ("--", "🏳️");

            if (DiccionarioFIFA.TryGetValue(target, out var match)) return (match.Code, match.Flag);
            if (target.Contains("Winner") || target.Contains("Runner-up") || target.Contains("Match") || target.Contains("3rd")) return (target, "🏆");

            string clean = target.Replace(".", "").Replace(" ", "");
            return (clean.Length > 3 ? clean.Substring(0, 3).ToUpper() : clean.ToUpper(), "🏳️");
        }
    }
}
