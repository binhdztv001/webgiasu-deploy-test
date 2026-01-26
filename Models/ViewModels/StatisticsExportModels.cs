namespace Webgiasu.Models.ViewModels
{
    public class StudentStatisticsExportModel
    {
        public int TotalProblems { get; set; }
        public int SolvedProblems { get; set; }
        public int InProgressProblems { get; set; }
        public int WaitingProblems { get; set; }
        public decimal TotalSpent { get; set; }
        public int PendingPayments { get; set; }
        public List<TypeStatistic> ProblemsByType { get; set; } = new();
        public List<DifficultyStatistic> ProblemsByDifficulty { get; set; } = new();
        public List<MonthStatistic> ProblemsByMonth { get; set; } = new();
        public List<MonthStatistic> SpendingByMonth { get; set; } = new();
    }

    public class TutorStatisticsExportModel
    {
        public int TotalProblems { get; set; }
        public int SolvedProblems { get; set; }
        public int InProgressProblems { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal PendingEarnings { get; set; }
        public List<TypeStatistic> ProblemsByType { get; set; } = new();
        public List<DifficultyStatistic> ProblemsByDifficulty { get; set; } = new();
        public List<MonthStatistic> ProblemsByMonth { get; set; } = new();
        public List<MonthStatistic> EarningsByMonth { get; set; } = new();
    }

    public class SchoolStatisticsExportModel
    {
        public int TotalClasses { get; set; }
        public int ActiveClasses { get; set; }
        public int CompletedClasses { get; set; }
        public int OngoingClasses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTutors { get; set; }
        public int ActiveTutors { get; set; }
        public double CompletionRate { get; set; }
        public List<SubjectStatistic> ClassesBySubject { get; set; } = new();
        public List<StatusStatistic> ClassesByStatus { get; set; } = new();
        public List<MonthStatistic> ClassesByMonth { get; set; } = new();
        public List<MonthStatistic> StudentsByMonth { get; set; } = new();
        public List<ClassDetailStatistic> ClassDetails { get; set; } = new();
    }

    // ✅ THÊM MODEL MỚI CHO ENTERPRISE
    public class EnterpriseStatisticsExportModel
    {
        public int TotalSchools { get; set; }
        public int TotalMentors { get; set; }
        public int TotalClasses { get; set; }
        public int ActiveClasses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalSchedules { get; set; }
        public List<SchoolStatisticDetail> SchoolStats { get; set; } = new();
        public List<SubjectStatistic> MentorsBySubject { get; set; } = new();
        public List<StatusStatistic> SchedulesByStatus { get; set; } = new();
        public List<MonthStatistic> ClassesByMonth { get; set; } = new();
    }

    public class TypeStatistic
    {
        public string Type { get; set; } = "";
        public int Count { get; set; }
    }

    public class DifficultyStatistic
    {
        public string Difficulty { get; set; } = "";
        public int Count { get; set; }
    }

    public class MonthStatistic
    {
        public string Month { get; set; } = "";
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class SubjectStatistic
    {
        public string Subject { get; set; } = "";
        public int Count { get; set; }
    }

    public class StatusStatistic
    {
        public string Status { get; set; } = "";
        public int Count { get; set; }
    }

    public class ClassDetailStatistic
    {
        public string ClassName { get; set; } = "";
        public string Subject { get; set; } = "";
        public string TutorName { get; set; } = "";
        public int StudentCount { get; set; }
        public string Status { get; set; } = "";
        public string StartDate { get; set; } = "";
    }

    // ✅ THÊM CLASS MỚI CHO ENTERPRISE
    public class SchoolStatisticDetail
    {
        public string SchoolName { get; set; } = "";
        public int TotalClasses { get; set; }
        public int ActiveClasses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalSchedules { get; set; }
    }
}