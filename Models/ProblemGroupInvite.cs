using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webgiasu.Models
{
    public class ProblemGroupInvite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }

        [Required]
        public int InvitedUserId { get; set; }

        [Required]
        public int InvitedByUserId { get; set; }

        [Required]
        public GroupInviteStatus Status { get; set; } = GroupInviteStatus.Pending;

        public DateTime InvitedAt { get; set; } = DateTime.Now;

        public DateTime? RespondedAt { get; set; }

        /* ================= Navigation Properties ================= */

        [ForeignKey("GroupId")]
        public ProblemGroup Group { get; set; } = null!;

        [ForeignKey("InvitedUserId")]
        public User InvitedUser { get; set; } = null!;

        [ForeignKey("InvitedByUserId")]
        public User InvitedByUser { get; set; } = null!;
    }
}