using System.ComponentModel.DataAnnotations;

namespace Webgiasu.Models
{
    public class CommunityPost
    {
        [Key]
        public int Id { get; set; }

        // 🔑 FK → Users.Id
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        public bool IsLocked { get; set; } = false;

        [MaxLength(100)]
        public string? Category { get; set; }

        // 🔗 Navigation
        public User User { get; set; }

        public ICollection<CommunityComment> Comments { get; set; }
    }
}
