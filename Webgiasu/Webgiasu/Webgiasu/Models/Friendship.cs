namespace Webgiasu.Models
{
    public class Friendship
    {
        public int Id { get; set; }
        
        // User gửi lời mời kết bạn
        public int RequesterId { get; set; }
        public User Requester { get; set; } = null!;
        
        // User nhận lời mời kết bạn
        public int AddresseeId { get; set; }
        public User Addressee { get; set; } = null!;
        
        // Trạng thái kết bạn: Pending, Accepted, Declined, Blocked
        public FriendshipStatus Status { get; set; }
        
        // Thời gian gửi lời mời
        public DateTime RequestedDate { get; set; }
        
        // Thời gian chấp nhận/từ chối
        public DateTime? RespondedDate { get; set; }
    }

    public enum FriendshipStatus
    {
        Pending = 0,    // Đang chờ xác nhận
        Accepted = 1,   // Đã chấp nhận - là bạn bè
        Declined = 2,   // Đã từ chối
        Blocked = 3     // Đã chặn
    }
}
