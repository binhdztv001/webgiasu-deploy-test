namespace Webgiasu.Models.ViewModels
{
    public class ProblemScheduleItemViewModel
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Time { get; set; }
    }

    public class TutorScheduleDayViewModel
    {
        public DateTime Date { get; set; }
        public List<string> ReceivedProblemTitles { get; set; } = new();
        public List<string> DeadlineProblemTitles { get; set; } = new();
        public List<ProblemScheduleItemViewModel> ReceivedProblems { get; set; } = new();
        public List<ProblemScheduleItemViewModel> DeadlineProblems { get; set; } = new();
    }
}
