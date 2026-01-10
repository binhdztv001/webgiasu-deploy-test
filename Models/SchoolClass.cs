using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webgiasu.Models
{
    public class SchoolClass
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SchoolId { get; set; }

        [ForeignKey("SchoolId")]
        public User? School { get; set; }

        [Required]
        [MaxLength(200)]
        public string ClassName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? TutorId { get; set; }

        [ForeignKey("TutorId")]
        public User? Tutor { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public ClassStatus Status { get; set; } = ClassStatus.Active;

        // Navigation property
        public ICollection<ClassStudent>? Students { get; set; }
    }

    public class ClassStudent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public SchoolClass? Class { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public User? Student { get; set; }

        public DateTime JoinedDate { get; set; } = DateTime.Now;
    }

    public enum ClassStatus
    {
        Active,      // Đang diễn ra
        Completed,   // Đã hoàn thành
        Upcoming,    // Sắp bắt đầu
        Cancelled    // Đã hủy
    }
}
