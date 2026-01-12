namespace Webgiasu.Models
{
    public enum EducationLevel
    {
        TieuHoc = 1,           // Tiểu học (lớp 1-5)
        THCS = 2,              // Trung học cơ sở (lớp 6-9)
        THPT = 3,              // Trung học phổ thông (lớp 10-12)
        DaiHoc = 4             // Đại học
    }
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsApproved { get; set; } // Dành cho Tutor
        public DateTime RegisteredDate { get; set; }
        public bool IsPremium { get; set; } = false;
        public DateTime? PremiumExpiredAt { get; set; }
        public EducationLevel? Level { get; set; }


        // Tutor Profile Fields
        public string? Bio { get; set; } // Gi?i thi?u b?n thân
        public string? Subjects { get; set; } // Môn h?c chuyên d?y
        public string? Education { get; set; } // Trình ?? h?c v?n
        public int? ExperienceYears { get; set; } // S? n?m kinh nghi?m
        public string? Certificates { get; set; } // Ch?ng ch?
        public ICollection<ProblemGroup>? CreatedGroups { get; set; }
        public ICollection<ProblemGroupMember>? GroupMemberships { get; set; }
        public ICollection<ProblemGroupInvite>? ReceivedInvites { get; set; }
        public ICollection<ProblemGroupInvite>? SentInvites { get; set; }
    }




}
