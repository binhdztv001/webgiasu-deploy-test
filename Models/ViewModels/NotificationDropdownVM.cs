namespace Webgiasu.Models.ViewModels
{
    public class NotificationDropdownVM
    {
        public List<Notification> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
    }
}