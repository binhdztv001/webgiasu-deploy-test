using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webgiasu.Models
{
    public class ProblemGroupMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;

        [Required]
        public GroupPaymentStatus PaymentStatus { get; set; } = GroupPaymentStatus.Unpaid;

        public DateTime JoinedAt { get; set; } = DateTime.Now;

        /* ================= Navigation Properties ================= */

        [ForeignKey("GroupId")]
        public ProblemGroup Group { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}