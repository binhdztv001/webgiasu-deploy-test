using Microsoft.AspNetCore.Mvc;
namespace Webgiasu.Controllers
{
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly GeminiService _gemini;
        private readonly ILogger<AiController> _logger;

        public AiController(GeminiService gemini, ILogger<AiController> logger)
        {
            _gemini = gemini;
            _logger = logger;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AiRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Message))
                return BadRequest(new { error = "Câu hỏi không được để trống" });

            try
            {
                // ✅ Xây dựng prompt với context bài toán
                var contextInfo = "";
                if (req.ProblemContext != null)
                {
                    contextInfo = $@"
📚 THÔNG TIN BÀI TOÁN:
- Tiêu đề: {req.ProblemContext.Title}
- Môn học: {req.ProblemContext.Type}
- Độ khó: {req.ProblemContext.Difficulty}
- Nội dung: {req.ProblemContext.Description}

";
                }

                var prompt = $@"{contextInfo}Bạn là trợ lý học tập thông minh cho học sinh.
Nhiệm vụ: Chỉ gợi ý HƯỚNG GIẢI, KHÔNG giải chi tiết hoàn chỉnh.

Quy tắc:
- Trả lời ngắn gọn, dễ hiểu bằng tiếng Việt
- Nếu có hình ảnh, hãy phân tích hình học trong ảnh
- Gợi ý phương pháp, công thức cần dùng
- Khuyến khích học sinh tự suy nghĩ và thực hành
- Nếu học sinh hỏi công thức, chỉ nêu tên và dạng tổng quát
- Output ra màn hình theo cấu trúc như sau:
1. Phân tích đề bài ngắn gọn
2. Gợi ý phương pháp giải
- Không giải chi tiết từng bước.

Câu hỏi của học sinh: {req.Message}";

                // ✅ Gọi API với ảnh (nếu có)
                var imageUrl = req.ProblemContext?.ImageUrl;
                var answer = await _gemini.AskWithImageAsync(prompt, imageUrl);

                return Ok(new { reply = answer });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Lỗi xử lý câu hỏi");
                return StatusCode(500, new { error = "Lỗi server. Vui lòng thử lại." });
            }
        }
    }

    public class AiRequest
    {
        public string Message { get; set; } = string.Empty;
        public ProblemContextDto? ProblemContext { get; set; }
    }

    public class ProblemContextDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string? ImageUrl { get; set; } // ✅ Thêm trường này
    }
}