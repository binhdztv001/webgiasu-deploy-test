namespace Webgiasu.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalTutors { get; set; }
        public int TotalProblems { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingTutorApprovals { get; set; }
        public int ActiveProblems { get; set; }
    }

    public class AdminStatisticsViewModel
    {
        public Dictionary<ProblemType, int> ProblemsByType { get; set; } = new Dictionary<ProblemType, int>();
        public Dictionary<string, decimal> RevenueByMonth { get; set; } = new Dictionary<string, decimal>();
        public List<User> TopTutors { get; set; } = new List<User>();
    }
}
