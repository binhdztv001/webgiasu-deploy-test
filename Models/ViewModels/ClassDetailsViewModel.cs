namespace Webgiasu.Models.ViewModels
{
    public class ClassDetailsViewModel
    {
        public int Id { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TutorName { get; set; } = string.Empty;
        public string TutorEmail { get; set; } = string.Empty;
        public string TutorPhone { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public ClassStatus Status { get; set; }
        public List<StudentInClassViewModel> Students { get; set; } = new();
        public string Duration { get; set; } = string.Empty;
    }

    public class StudentInClassViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string JoinedDate { get; set; } = string.Empty;
    }
}
