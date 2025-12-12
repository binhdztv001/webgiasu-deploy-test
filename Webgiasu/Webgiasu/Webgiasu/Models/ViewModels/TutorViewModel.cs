namespace Webgiasu.Models.ViewModels
{
    public class TutorDashboardViewModel
    {
        public List<Problem> AvailableProblems { get; set; } = new List<Problem>();
        public List<Problem> MyAssignedProblems { get; set; } = new List<Problem>();
        public List<Solution> MySolutions { get; set; } = new List<Solution>();
        public int TotalEarnings { get; set; }
    }

    public class SubmitSolutionViewModel
    {
        public int ProblemId { get; set; }
        public string Content { get; set; } = string.Empty;
        public IFormFile? SolutionFile { get; set; }
    }

    public class TutorEarningsViewModel
    {
        public List<Solution> Solutions { get; set; } = new List<Solution>();
        public int TotalEarnings { get; set; }
    }
}
