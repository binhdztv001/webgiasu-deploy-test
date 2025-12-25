namespace Webgiasu.Models
{
    public enum GroupMemberRole
    {
        Owner,
        Member
    }

    public enum GroupInviteStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public enum GroupPaymentStatus
    {
        Unpaid,
        Paid,
        Refunded
    }

    public enum ProblemGroupStatus
    {
        Open,
        InProgress,
        Completed,
        Cancelled
    }
}