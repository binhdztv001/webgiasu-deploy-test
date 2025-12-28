using System.Net.Http;
using System.Text;
using System.Text.Json;

public class GeminiService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiService> _logger;
    private readonly IWebHostEnvironment _env; // ✅ Thêm để đọc file local

    public GeminiService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<GeminiService> logger,
        IWebHostEnvironment env) // ✅ Inject IWebHostEnvironment
    {
        _http = httpClient;
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey chưa được cấu hình");
        _model = config["Gemini:Model"] ?? "gemini-1.5-flash";
        _logger = logger;
        _env = env;
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<string> AskAsync(string prompt)
    {
        return await AskWithImageAsync(prompt, null);
    }

    public async Task<string> AskWithImageAsync(string prompt, string? imageUrl)
    {
        try
        {
            var apiVersion = "v1";
            var url = $"https://generativelanguage.googleapis.com/{apiVersion}/models/{_model}:generateContent?key={_apiKey}";

            _logger.LogInformation("🔗 API URL: {Url}", url.Replace(_apiKey, "***"));

            var parts = new List<object> { new { text = prompt } };

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                try
                {
                    _logger.LogInformation("📸 Đang tải ảnh từ: {Url}", imageUrl);

                    var imageBytes = await DownloadImageAsync(imageUrl);
                    var base64Image = Convert.ToBase64String(imageBytes);

                    // ✅ Tự động phát hiện mime type
                    var mimeType = GetMimeType(imageUrl);

                    parts.Add(new
                    {
                        inline_data = new
                        {
                            mime_type = mimeType,
                            data = base64Image
                        }
                    });

                    _logger.LogInformation("✅ Đã thêm ảnh vào request (size: {Size} KB, type: {Type})",
                        imageBytes.Length / 1024, mimeType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Không thể tải ảnh từ: {Url}", imageUrl);
                    // ✅ Thêm thông báo vào prompt
                    parts.Add(new { text = "\n[Lưu ý: Không thể tải được ảnh của bài toán]" });
                }
            }

            var body = new
            {
                contents = new[]
                {
                    new { parts = parts.ToArray() }
                }
            };

            var json = JsonSerializer.Serialize(body);
            _logger.LogInformation("📤 Request Body Length: {Length} bytes", json.Length);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("📥 Response Status: {Status}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ API Error: {Result}", result);
                return $"Lỗi API: {response.StatusCode}. Vui lòng thử lại sau.";
            }

            using var doc = JsonDocument.Parse(result);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates))
            {
                _logger.LogWarning("⚠️ Không tìm thấy candidates trong response");
                return "AI không thể trả lời câu hỏi này. Vui lòng thử lại.";
            }

            if (candidates.GetArrayLength() == 0)
            {
                return "AI không có câu trả lời. Vui lòng thử câu hỏi khác.";
            }

            var text = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "AI không trả về kết quả.";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "🔌 Lỗi kết nối HTTP");
            return "Không thể kết nối đến AI. Kiểm tra kết nối mạng.";
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "⏱️ Timeout");
            return "Yêu cầu hết thời gian chờ. Vui lòng thử lại.";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "📄 Lỗi parse JSON");
            return "Lỗi xử lý dữ liệu từ AI.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Lỗi không xác định");
            return "Đã xảy ra lỗi. Vui lòng thử lại.";
        }
    }

    // ✅ Helper: Tải ảnh từ cả local và URL
    private async Task<byte[]> DownloadImageAsync(string imageUrl)
    {
        // ✅ Xử lý đường dẫn local (bắt đầu bằng / hoặc ~/)
        if (imageUrl.StartsWith("/") || imageUrl.StartsWith("~/"))
        {
            var relativePath = imageUrl.TrimStart('~', '/');
            var filePath = Path.Combine(_env.WebRootPath, relativePath);

            _logger.LogInformation("📂 Đọc file local: {Path}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("⚠️ File không tồn tại: {Path}", filePath);
                throw new FileNotFoundException($"Không tìm thấy file ảnh: {filePath}");
            }

            return await File.ReadAllBytesAsync(filePath);
        }

        // ✅ Xử lý URL đầy đủ (http/https)
        _logger.LogInformation("🌐 Tải ảnh từ URL: {Url}", imageUrl);
        return await _http.GetByteArrayAsync(imageUrl);
    }

    // ✅ Helper: Phát hiện MIME type từ extension
    private string GetMimeType(string imageUrl)
    {
        var extension = Path.GetExtension(imageUrl).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/jpeg" // Default
        };
    }
}