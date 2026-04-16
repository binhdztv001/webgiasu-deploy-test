namespace Webgiasu.Models.ViewModels
{
    public class TutorScheduleDayViewModel
    {
        public DateTime Date { get; set; }
        public List<string> ReceivedProblemTitles { get; set; } = new();
        public List<string> DeadlineProblemTitles { get; set; } = new();
    }
}
