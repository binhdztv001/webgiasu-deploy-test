namespace Webgiasu.Models
{
    public class GroupPayment
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int MemberId { get; set; } // ProblemGroupMember.Id
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? TransactionId { get; set; }

        // Navigation properties
        public ProblemGroup? Group { get; set; }
        public User? User { get; set; }
    }
}