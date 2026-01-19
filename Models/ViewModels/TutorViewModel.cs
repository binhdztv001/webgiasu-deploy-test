using System.ComponentModel.DataAnnotations;

namespace Webgiasu.Models.ViewModels
{
    public class TutorDashboardViewModel
    {
        public List<Problem> AvailableProblems { get; set; } = new List<Problem>();
        public List<Problem> MyAssignedProblems { get; set; } = new List<Problem>();
        public List<Solution> MySolutions { get; set; } = new List<Solution>();
        public int TotalEarnings { get; set; }
    }

    public class SubmitSolutionViewModel
    {
        public int ProblemId { get; set; }
        public string Content { get; set; } = string.Empty;
        public IFormFile? SolutionFile { get; set; }
    }

    public class TutorEarningsViewModel
    {
        public List<Solution> Solutions { get; set; } = new List<Solution>();
        public int TotalEarnings { get; set; }
    }

    // ✅ THÊM MỚI
    public class ApplyProblemViewModel
    {
        [Required(ErrorMessage = "ProblemId là bắt buộc")]
        public int ProblemId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đề xuất của bạn")]
        [StringLength(1000, MinimumLength = 50, ErrorMessage = "Đề xuất phải từ 50 đến 1000 ký tự")]
        [Display(Name = "Đề xuất")]
        public string Proposal { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá đề xuất")]
        [Range(10000, 10000000, ErrorMessage = "Giá đề xuất phải từ 10,000đ đến 10,000,000đ")]
        [Display(Name = "Giá đề xuất (VNĐ)")]
        public decimal ProposedPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời gian dự kiến")]
        [Range(1, 30, ErrorMessage = "Thời gian dự kiến từ 1 đến 30 ngày")]
        [Display(Name = "Thời gian dự kiến (ngày)")]
        public int EstimatedDays { get; set; }

        // Optional display properties
        public string? ProblemTitle { get; set; }
        public decimal? OriginalPrice { get; set; }
        public DateTime? Deadline { get; set; }
    }
}
