namespace Webgiasu.Models
{
    public class Rating
    {
        public int RatingId { get; set; }
        public int ProblemId { get; set; }
        public int StudentId { get; set; }
        public int TutorId { get; set; }
        public int Stars { get; set; } // 1-5
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Problem? Problem { get; set; }
        public User? Student { get; set; }
        public User? Tutor { get; set; }
    }
}
