namespace Webgiasu.Models
{
    public enum NotificationType
    {
        ProblemCreated,      // Đăng bài mới thành công
        TutorAccepted,       // Mentor nhận bài
        SolutionSubmitted,   // Mentor gửi lời giải
        PaymentCompleted,    // Thanh toán thành công
        GroupInvite,         // Lời mời vào nhóm
        FriendRequest,
        FriendRequestAccepted, // Lời mời kết bạn
        RatingReceived,          // Khi nhận được đánh giá
        PaymentReceived,          // Khi nhận được tiền thanh toán
        TutorApplied,           // ✅ THÊM MỚI
        ApplicationApproved,    // ✅ THÊM MỚI
        ApplicationRejected,    // ✅ THÊM MỚI (optional)
        EnterpriseMentorCreated, // ✅ THÊM MỚI
        EnterpriseSystemAlert   // ✅ THÊM MỚI
    }

    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? Link { get; set; }  // Link đến trang chi tiết
        public bool IsRead { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation
        public User? User { get; set; }

        // Optional: Reference IDs
        public int? ProblemId { get; set; }
        public int? PaymentId { get; set; }
        public int? GroupId { get; set; }
    }
}