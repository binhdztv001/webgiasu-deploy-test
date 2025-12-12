namespace Webgiasu.Models.ViewModels
{
    public class RatingViewModel
    {
        public int ProblemId { get; set; }
        public string? ProblemTitle { get; set; }
        public int TutorId { get; set; }
        public string? TutorName { get; set; }
        public int Stars { get; set; }
        public string? Comment { get; set; }
    }

    public class TutorRatingsSummaryViewModel
    {
        public int TutorId { get; set; }
        public string? TutorName { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public Dictionary<int, int>? StarDistribution { get; set; } // Star (1-5) -> Count
        public List<RatingDetailViewModel>? RecentRatings { get; set; }
    }

    public class RatingDetailViewModel
    {
        public int RatingId { get; set; }
        public string? StudentName { get; set; }
        public string? ProblemTitle { get; set; }
        public int Stars { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
