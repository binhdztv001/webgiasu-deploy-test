using Microsoft.AspNetCore.Mvc.Rendering;

namespace Webgiasu.Models.ViewModels
{
    public class CreateProblemViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ProblemType Type { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public IFormFile? ImageFile { get; set; }
        public DateTime Deadline { get; set; }
    }

    public class ProblemDetailsViewModel
    {
        public Problem Problem { get; set; } = new Problem();
        public User? Student { get; set; }
        public User? AssignedTutor { get; set; }
        public Solution? Solution { get; set; }
        public Payment? Payment { get; set; }
    }

    public class StudentDashboardViewModel
    {
        public List<Problem> MyProblems { get; set; } = new List<Problem>();
        public List<Solution> Solutions { get; set; } = new List<Solution>();
        public List<Payment> Payments { get; set; } = new List<Payment>();
    }
}
