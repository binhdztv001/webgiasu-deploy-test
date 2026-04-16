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

    public class AdminCreateUserViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Student;
        public bool IsApproved { get; set; } = true;
        public EducationLevel? Level { get; set; }
    }

    public class AdminEditUserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsApproved { get; set; }
        public EducationLevel? Level { get; set; }
    }
}
