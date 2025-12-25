namespace Webgiasu.Models.ViewModels
{
    public class CommentCreateModel
    {
        public int PostId { get; set; }
        public string Content { get; set; } = "";
        public bool IsAnonymous { get; set; } = false;
    }
}
