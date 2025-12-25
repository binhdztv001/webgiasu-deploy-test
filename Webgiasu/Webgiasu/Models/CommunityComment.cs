using System.ComponentModel.DataAnnotations;

namespace Webgiasu.Models
{
    public class CommunityComment
    {
        [Key]
        public int Id { get; set; }

        // 🔑 FK → CommunityPost
        [Required]
        public int PostId { get; set; }

        // 🔑 FK → Users
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Content { get; set; }

        // ✅ NEW: Đánh dấu bình luận ẩn danh
        public bool IsAnonymous { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        // 🔗 Navigation
        public CommunityPost Post { get; set; }
        public User User { get; set; }
    }
}
