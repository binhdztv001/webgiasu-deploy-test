namespace Webgiasu.Models
{
    public enum ApplicationStatus
    {
        Pending,      // Chờ Student duyệt
        Approved,     // Student đã chọn
        Rejected,     // Student từ chối
        Withdrawn     // Tutor rút lại
    }

    public class TutorApplication
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public int TutorId { get; set; }

        public string Proposal { get; set; } = string.Empty; // Đề xuất của Tutor
        public decimal ProposedPrice { get; set; }            // Giá đề xuất (có thể thương lượng)
        public int EstimatedDays { get; set; }                // Thời gian dự kiến hoàn thành

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        public DateTime AppliedDate { get; set; } = DateTime.Now;
        public DateTime? ResponsedDate { get; set; }          // Ngày Student phản hồi

        // Navigation properties
        public Problem? Problem { get; set; }
        public User? Tutor { get; set; }
    }
}