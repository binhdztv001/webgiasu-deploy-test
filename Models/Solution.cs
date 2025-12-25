namespace Webgiasu.Models
{
    public class Solution
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public int TutorId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public int? Rating { get; set; }
        public string? Feedback { get; set; }
    }
}
