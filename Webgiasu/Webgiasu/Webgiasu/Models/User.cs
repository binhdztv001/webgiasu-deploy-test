namespace Webgiasu.Models
{
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

        // Tutor Profile Fields
        public string? Bio { get; set; } // Gi?i thi?u b?n thân
        public string? Subjects { get; set; } // Môn h?c chuyên d?y
        public string? Education { get; set; } // Trình ?? h?c v?n
        public int? ExperienceYears { get; set; } // S? n?m kinh nghi?m
        public string? Certificates { get; set; } // Ch?ng ch?

        // Premium
        public bool IsPremium { get; set; } = false;
        public DateTime? PremiumExpiredAt { get; set; }

    }
}
