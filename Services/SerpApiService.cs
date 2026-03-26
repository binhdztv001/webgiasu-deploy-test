using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Webgiasu.Services
{
    public interface ISerpApiService
    {
        Task<List<SearchResult>> SearchDocumentsAsync(string query, string fileType = "pdf");
    }

    public class SerpApiService : ISerpApiService
    {
        private readonly string _apiKey;
        private readonly ILogger<SerpApiService> _logger;
        private readonly HttpClient _http;

        private static readonly string[] HighTrustDomains =
        {
            ".edu", ".edu.vn", ".ac.vn", ".ac.uk",
            ".gov", ".gov.vn",
            "mit.edu", "stanford.edu",
            "springer.com", "elsevier.com",
            "sciencedirect.com", "arxiv.org",
            "ieee.org", "nih.gov", "nasa.gov"
        };

        private static readonly string[] BlacklistDomains =
        {
            "facebook.com", "youtube.com", "tiktok.com",
            "scribd.com", "slideshare.net",
            "blogspot", "wordpress.com",
            "medium.com", "docplayer"
        };

        public SerpApiService(IConfiguration config, ILogger<SerpApiService> logger, HttpClient http)
        {
            _apiKey = config["SerpApi:ApiKey"] ?? throw new Exception("SerpApi:ApiKey not found");
            _logger = logger;
            _http = http;
        }

        public async Task<List<SearchResult>> SearchDocumentsAsync(string query, string fileType = "pdf")
        {
            try
            {
                _logger.LogInformation("🔍 SerpApi search: {Query}", query);

                var finalQuery = $"{query} filetype:{fileType} (site:edu OR site:edu.vn OR site:ac.vn OR site:gov)";
                var q = Uri.EscapeDataString(finalQuery);

                var url = $"/search?engine=google_scholar&q={q}&api_key={_apiKey}&num=20&hl=vi";

                _logger.LogDebug("Request URL: {Url}", url);

                using var resp = await _http.GetAsync(url);
                var content = await resp.Content.ReadAsStringAsync();

                _logger.LogInformation("SerpApi status {Status}, length {Len}", resp.StatusCode, content?.Length ?? 0);

                if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning("SerpApi returned unsuccessful response, fallback");
                    return await SearchWithoutSiteRestriction(query, fileType);
                }

                using var jsonDoc = JsonDocument.Parse(content);

                if (!jsonDoc.RootElement.TryGetProperty("organic_results", out var organic))
                {
                    _logger.LogWarning("No organic_results in SerpApi response, fallback");
                    _logger.LogDebug("Root properties: {Props}", string.Join(", ",
                        jsonDoc.RootElement.EnumerateObject().Select(p => p.Name)));
                    return await SearchWithoutSiteRestriction(query, fileType);
                }

                var results = new List<SearchResult>();
                foreach (var item in organic.EnumerateArray())
                {
                    if (!item.TryGetProperty("link", out var linkProp)) continue;
                    var link = linkProp.GetString();
                    if (string.IsNullOrWhiteSpace(link)) continue;
                    if (!link.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) continue;
                    if (BlacklistDomains.Any(b => link.Contains(b, StringComparison.OrdinalIgnoreCase))) continue;

                    var score = CalculateQualityScore(link);
                    if (score < 40) continue;

                    var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                    var snippet = item.TryGetProperty("snippet", out var s) ? s.GetString() ?? "" : "";
                    var domain = "";
                    try { domain = new Uri(link).Host; } catch { }

                    results.Add(new SearchResult
                    {
                        Title = title,
                        Url = link,
                        Snippet = snippet,
                        FileType = "pdf",
                        QualityScore = score,
                        SourceDomain = domain
                    });
                }

                var finalResults = results
                    .GroupBy(r => r.Url)
                    .Select(g => g.First())
                    .OrderByDescending(r => r.QualityScore)
                    .Take(5)
                    .ToList();

                _logger.LogInformation("Search finished: returned {Count}", finalResults.Count);
                return finalResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SerpApi error");
                return new List<SearchResult>();
            }
        }

        private async Task<List<SearchResult>> SearchWithoutSiteRestriction(string query, string fileType)
        {
            try
            {
                var fallbackQuery = $"{query} filetype:{fileType} tài liệu học tập";
                var q = Uri.EscapeDataString(fallbackQuery);
                var url = $"/search?engine=google&q={q}&api_key={_apiKey}&num=10&hl=vi";

                using var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Fallback search failed");
                    return new List<SearchResult>();
                }

                var content = await resp.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(content);

                if (!jsonDoc.RootElement.TryGetProperty("organic_results", out var organic))
                    return new List<SearchResult>();

                var results = new List<SearchResult>();
                foreach (var item in organic.EnumerateArray())
                {
                    if (!item.TryGetProperty("link", out var linkProp)) continue;
                    var link = linkProp.GetString();
                    if (string.IsNullOrWhiteSpace(link)) continue;
                    if (!link.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) continue;
                    if (BlacklistDomains.Any(b => link.Contains(b, StringComparison.OrdinalIgnoreCase))) continue;
                    var score = CalculateQualityScore(link);
                    if (score < 40) continue;

                    var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                    var snippet = item.TryGetProperty("snippet", out var s) ? s.GetString() ?? "" : "";
                    var domain = "";
                    try { domain = new Uri(link).Host; } catch { }

                    results.Add(new SearchResult
                    {
                        Title = title,
                        Url = link,
                        Snippet = snippet,
                        FileType = "pdf",
                        QualityScore = score,
                        SourceDomain = domain
                    });
                }

                return results.Take(5).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallback error");
                return new List<SearchResult>();
            }
        }

        private int CalculateQualityScore(string url)
        {
            var lower = url.ToLowerInvariant();
            var score = 0;
            if (lower.EndsWith(".pdf")) score += 40;
            if (lower.StartsWith("https://")) score += 5;
            foreach (var d in HighTrustDomains) if (lower.Contains(d)) score += 30;
            foreach (var d in BlacklistDomains) if (lower.Contains(d)) score -= 80;
            if (url.Length < 160) score += 5;
            return score;
        }
    }

    public class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public string FileType { get; set; } = "pdf";
        public int QualityScore { get; set; }
        public string SourceDomain { get; set; } = "";
    }
}