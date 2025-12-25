namespace Webgiasu.Models
{
    public class Message
    {
        public int Id { get; set; }
        
        // User g?i tin nh?n
        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;
        
        // User nh?n tin nh?n
        public int ReceiverId { get; set; }
        public User Receiver { get; set; } = null!;
        
        // N?i dung tin nh?n
        public string Content { get; set; } = string.Empty;
        
        // Th?i gian g?i
        public DateTime SentDate { get; set; }
        
        // ?ã ??c ch?a
        public bool IsRead { get; set; }
        
        // Th?i gian ??c
        public DateTime? ReadDate { get; set; }
        
        // ?ã xóa b?i sender
        public bool IsDeletedBySender { get; set; }
        
        // ?ã xóa b?i receiver
        public bool IsDeletedByReceiver { get; set; }
        
        // Lo?i tin nh?n: Text, Image, File
        public MessageType Type { get; set; }
        
        // File ?ính kèm (n?u có)
        public string? AttachmentUrl { get; set; }
    }

    public enum MessageType
    {
        Text = 0,
        Image = 1,
        File = 2
    }
}
