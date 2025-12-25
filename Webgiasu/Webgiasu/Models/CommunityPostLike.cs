namespace Webgiasu.Models
{
    public class CommunityPostLike
    {
        public int Id { get; set; }

        public int PostId { get; set; }
        public CommunityPost Post { get; set; }
        public string ReactionType { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
