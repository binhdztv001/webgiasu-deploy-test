namespace Webgiasu.Models.ViewModels
{
    public class ClassSchoolViewModel
    {
        public int Id { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string TutorName { get; set; } = string.Empty;
        public string TutorEmail { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public ClassStatus Status { get; set; }
        public string CreatedDate { get; set; } = string.Empty;
    }
}
