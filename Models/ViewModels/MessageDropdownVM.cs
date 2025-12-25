namespace Webgiasu.Models.ViewModels
{
    public class MessageDropdownVM
    {
        public List<Message> Messages { get; set; } = new();
        public int UnreadCount { get; set; }
    }
}
