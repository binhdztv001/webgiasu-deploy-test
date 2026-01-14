using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webgiasu.Models
{
    /// <summary>
    /// Lịch trao đổi/Lịch gặp mặt cho lớp học
    /// </summary>
    public class ClassSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public SchoolClass? Class { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public DateTime ScheduleDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        [MaxLength(50)]
        public string MeetingType { get; set; } = "Online"; // Online hoặc Offline

        [MaxLength(500)]
        public string? Location { get; set; } // Địa điểm cụ thể (cho Offline) hoặc Link meeting (cho Online)

        public string? Content { get; set; } // Nội dung trao đổi

        [MaxLength(1000)]
        public string? Notes { get; set; } // Ghi chú thêm

        [Required]
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Upcoming;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        public int CreatedBy { get; set; } // User ID của người tạo (School)

        [ForeignKey("CreatedBy")]
        public User? Creator { get; set; }
    }

    public enum ScheduleStatus
    {
        Upcoming,    // Sắp tới
        InProgress,  // Đang diễn ra
        Completed,   // Đã hoàn thành
        Cancelled    // Đã hủy
    }
}

