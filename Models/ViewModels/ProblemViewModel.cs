using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Webgiasu.Models.ViewModels
{
    public class CreateProblemViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mô tả")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn môn học")]
        public ProblemType Type { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn độ khó")]
        public DifficultyLevel Difficulty { get; set; }

        public IFormFile? ImageFile { get; set; }
        public IFormFile? AttachmentFile { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn deadline")]
        public DateTime Deadline { get; set; }

        // ✅ NEW: Custom price field
        public decimal? CustomPrice { get; set; }

        // ✅ NEW: Group mode fields
        public bool IsGroupMode { get; set; } = false;

        [MaxLength(255)]
        public string? GroupName { get; set; }
    }

    public class ProblemDetailsViewModel
    {
        public Problem Problem { get; set; } = new Problem();
        public User? Student { get; set; }
        public User? AssignedTutor { get; set; }
        public Solution? Solution { get; set; }
        public Payment? Payment { get; set; }
        
        // ✅ NEW: Group info if problem belongs to a group
        public ProblemGroup? Group { get; set; }
        public List<ProblemGroupMember>? GroupMembers { get; set; }
    }

    public class StudentDashboardViewModel
    {
        public List<Problem> MyProblems { get; set; } = new List<Problem>();
        public List<Solution> Solutions { get; set; } = new List<Solution>();
        public List<Payment> Payments { get; set; } = new List<Payment>();
        
        // ✅ NEW: Group-related data
        public List<ProblemGroup> MyGroups { get; set; } = new List<ProblemGroup>();
        public List<ProblemGroupInvite> PendingInvites { get; set; } = new List<ProblemGroupInvite>();
    }

    // ✅ NEW: ViewModel for Group Details page
    public class GroupDetailsViewModel
    {
        public ProblemGroup Group { get; set; } = new ProblemGroup();
        public Problem Problem { get; set; } = new Problem();
        public List<ProblemGroupMember> Members { get; set; } = new List<ProblemGroupMember>();
        public List<ProblemGroupInvite> PendingInvites { get; set; } = new List<ProblemGroupInvite>();
        public List<User> AvailableFriendsToInvite { get; set; } = new List<User>();
        public bool IsOwner { get; set; }
        public bool IsMember { get; set; }
        public decimal PricePerMember { get; set; }
    }

    // ✅ NEW: ViewModel for inviting friends
    public class InviteFriendsViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public List<User> AvailableFriends { get; set; } = new List<User>();
        public List<int> SelectedFriendIds { get; set; } = new List<int>();
    }
}
