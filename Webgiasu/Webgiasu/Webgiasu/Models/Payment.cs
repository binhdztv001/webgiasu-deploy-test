namespace Webgiasu.Models
{
    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public class Payment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int ProblemId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? TransactionId { get; set; }
    }
}
