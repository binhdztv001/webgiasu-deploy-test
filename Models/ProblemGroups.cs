using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webgiasu.Models
{
    public class ProblemGroup
    {
        [Key]
        public int Id { get; set; }

        // Problem chung
        [Required]
        public int ProblemId { get; set; }

        // Student tạo nhóm
        [Required]
        public int CreatedByUserId { get; set; }

        [MaxLength(255)]
        public string? GroupName { get; set; }

        // Tổng tiền thuê Tutor
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Giá chia cho mỗi thành viên
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerMember { get; set; }

        [Required]
        public ProblemGroupStatus Status { get; set; } = ProblemGroupStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /* ================= Navigation Properties ================= */

        [ForeignKey("ProblemId")]
        public Problem Problem { get; set; } = null!;

        [ForeignKey("CreatedByUserId")]
        public User CreatedByUser { get; set; } = null!;

        public ICollection<ProblemGroupMember> Members { get; set; } = new List<ProblemGroupMember>();

        public ICollection<ProblemGroupInvite> Invites { get; set; } = new List<ProblemGroupInvite>();
    }
}