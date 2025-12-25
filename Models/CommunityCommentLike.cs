namespace Webgiasu.Models
{
    public class CommunityCommentLike
    {
        public int Id { get; set; }

        public int CommentId { get; set; }
        public CommunityComment Comment { get; set; }
        public string ReactionType { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
