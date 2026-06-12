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
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    // MODELOS DE ENTRADA (MAPEO DEL JSON DE GITHUB)
    public class PartidoLocal
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("group")] public string Group { get; set; }
        [JsonPropertyName("local_date")] public string LocalDate { get; set; }
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

    // MODELOS DE SALIDA OPTIMIZADOS PARA EL CARRUSEL DEL FRONTEND
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
            string apiKey = _config["WidgetSettings:ApiKey"];
            string apiCacheKey = "LiveWorldCupApiCache_V12";
            string remoteJsonCacheKey = "RemoteFixtureJsonString_V12";
            DateTime now = DateTime.Now;

            string localJsonPath = Path.Combine(_env.WebRootPath, "data", "partidos.json");
            string jsonContent = string.Empty;

            if (!_cache.TryGetValue(remoteJsonCacheKey, out jsonContent))
            {
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    jsonContent = await client.GetStringAsync("https://worldcup26.ir/get/games");

                    _cache.Set(remoteJsonCacheKey, jsonContent, TimeSpan.FromMinutes(10));

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

            // 1. PROCESAR, LIMPIAR Y FORMATEAR CADA PARTIDO INDIVIDUAL
            var todosLosPartidosMapeados = root.Games.Select(p => {
                DateTime pTime = DateTime.ParseExact(p.LocalDate, "MM/dd/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                double m = (now - pTime).TotalMinutes;

                var elLocal = NormalizarEquipo(p.HomeTeamNameEn, p.HomeTeamLabel);
                var elVisitante = NormalizarEquipo(p.AwayTeamNameEn, p.AwayTeamLabel);

                string score = $"{p.HomeScore} - {p.AwayScore}";
                string badge = "";
                bool isFinished = p.Finished == "TRUE" || p.TimeElapsed == "finished" || m > 135;

                if (isFinished)
                {
                    badge = "<span class='wc-fin-badge'>Fin</span>";
                }
                else if (m >= 0 && m <= 135)
                {
                    int minReal = m > 90 ? 90 : (int)m == 0 ? 1 : (int)m;
                    badge = $"<span class='wc-live-pulse-badge'>🔴 {minReal}'</span>";
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
                        ScoreVisual = (m < 0 && !isFinished) ? "vs" : score,
                        StatusBadgeHtml = badge,
                        HomeScorersList = LimpiarGoleadores(p.HomeScorers),
                        AwayScorersList = LimpiarGoleadores(p.AwayScorers),
                        HasDetails = isFinished || (m >= 0)
                    }
                };
            }).ToList();

            // 2. AGRUPACIÓN COMPLEJA E INTELIGENTE: FECHAS ➔ HORAS ➔ PARTIDOS SIMULTÁNEOS
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

            // 3. CALCULAR QUÉ ÍNDICE DE FECHA DEBE QUEDAR ACTIVO POR DEFECTO (HOY)
            int indexFechaActiva = estructuraAgrupada.FindIndex(d => d.DateLabel.Contains(now.ToString("dd/MM")));
            if (indexFechaActiva == -1) indexFechaActiva = estructuraAgrupada.FindLastIndex(d => now.Date >= DateTime.ParseExact(d.DateLabel.Split(' ')[1], "dd/MM", null).Date);
            if (indexFechaActiva == -1) indexFechaActiva = 0;

            return Json(new
            {
                dates = estructuraAgrupada,
                currentDateIndex = indexFechaActiva
            });
        }

        // LIMPIADOR AVANZADO DE STRINGS DE GOLEADORES DEL REPOSITORIO
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
