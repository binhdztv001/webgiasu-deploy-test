namespace Webgiasu.Models.ViewModels
{
    public class Post
    {
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public User? Author { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int LikesCount { get; set; } = 0;
        public int CommentsCount { get; set; } = 0;
        public List<PostComment>? Comments { get; set; } = new();
        public List<PostLike>? Likes { get; set; } = new();
    }

    public class PostComment
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int AuthorId { get; set; }
        public User? Author { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class PostLike
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
