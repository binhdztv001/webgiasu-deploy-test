using System.ComponentModel.DataAnnotations;

namespace Webgiasu.Models.ViewModels
{
    /// <summary>
    /// ViewModel để tạo lịch trao đổi mới
    /// </summary>
    public class CreateScheduleViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn lớp học")]
        [Display(Name = "Lớp học")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày")]
        [Display(Name = "Ngày trao đổi")]
        [DataType(DataType.Date)]
        public DateTime ScheduleDate { get; set; } = DateTime.Now.AddDays(1);

        [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu")]
        [Display(Name = "Giờ bắt đầu")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; } = new TimeSpan(14, 0, 0); // 14:00

        [Required(ErrorMessage = "Vui lòng chọn giờ kết thúc")]
        [Display(Name = "Giờ kết thúc")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; } = new TimeSpan(16, 0, 0); // 16:00

        [Required(ErrorMessage = "Vui lòng chọn hình thức")]
        [Display(Name = "Hình thức")]
        public string MeetingType { get; set; } = "Online";

        [Display(Name = "Địa điểm / Link meeting")]
        [StringLength(500, ErrorMessage = "Địa điểm không được vượt quá 500 ký tự")]
        public string? Location { get; set; }

        [Display(Name = "Nội dung trao đổi")]
        public string? Content { get; set; }

        [Display(Name = "Ghi chú")]
        [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// ViewModel để hiển thị chi tiết lịch trao đổi
    /// </summary>
    public class ScheduleDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime ScheduleDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string MeetingType { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Content { get; set; }
        public string? Notes { get; set; }
        public ScheduleStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatorName { get; set; } = string.Empty;

        // Thông tin lớp học
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? TutorName { get; set; }
        public string? TutorEmail { get; set; }
        public int TotalStudents { get; set; }

        // Format helpers
        public string ScheduleDateTimeDisplay => $"{ScheduleDate:dd/MM/yyyy} từ {StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        public string Duration
        {
            get
            {
                var duration = EndTime - StartTime;
                return $"{duration.TotalHours:F1} giờ";
            }
        }
    }

    /// <summary>
    /// ViewModel để chỉnh sửa lịch trao đổi
    /// </summary>
    public class EditScheduleViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày")]
        [Display(Name = "Ngày trao đổi")]
        [DataType(DataType.Date)]
        public DateTime ScheduleDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu")]
        [Display(Name = "Giờ bắt đầu")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ kết thúc")]
        [Display(Name = "Giờ kết thúc")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hình thức")]
        [Display(Name = "Hình thức")]
        public string MeetingType { get; set; } = "Online";

        [Display(Name = "Địa điểm / Link meeting")]
        [StringLength(500)]
        public string? Location { get; set; }

        [Display(Name = "Nội dung trao đổi")]
        public string? Content { get; set; }

        [Display(Name = "Ghi chú")]
        [StringLength(1000)]
        public string? Notes { get; set; }

        [Display(Name = "Trạng thái")]
        public ScheduleStatus Status { get; set; }

        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel để liệt kê các lịch trao đổi
    /// </summary>
    public class ScheduleListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime ScheduleDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string MeetingType { get; set; } = string.Empty;
        public ScheduleStatus Status { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int TotalStudents { get; set; }

        public string StatusBadgeClass
        {
            get
            {
                return Status switch
                {
                    ScheduleStatus.Upcoming => "bg-info",
                    ScheduleStatus.InProgress => "bg-warning",
                    ScheduleStatus.Completed => "bg-success",
                    ScheduleStatus.Cancelled => "bg-danger",
                    _ => "bg-secondary"
                };
            }
        }

        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    ScheduleStatus.Upcoming => "Sắp tới",
                    ScheduleStatus.InProgress => "Đang diễn ra",
                    ScheduleStatus.Completed => "Đã hoàn thành",
                    ScheduleStatus.Cancelled => "Đã hủy",
                    _ => "Không xác định"
                };
            }
        }

        public string ScheduleDateTimeDisplay => $"{ScheduleDate:dd/MM/yyyy} - {StartTime:hh\\:mm}";
    }
}

