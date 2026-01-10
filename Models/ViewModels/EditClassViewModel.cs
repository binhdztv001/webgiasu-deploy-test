namespace Webgiasu.Models.ViewModels
{
    public class EditClassViewModel
    {
        public int Id { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? TutorId { get; set; }
        public string StartDateValue { get; set; } = string.Empty;
        public string EndDateValue { get; set; } = string.Empty;
        public ClassStatus Status { get; set; }
        public string CreatedDate { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }
}
