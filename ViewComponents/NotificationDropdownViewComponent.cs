using Microsoft.AspNetCore.Mvc;
using Webgiasu.Models.ViewModels;
using Webgiasu.Services;

namespace Webgiasu.ViewComponents
{
    public class NotificationDropdownViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationDropdownViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public IViewComponentResult Invoke()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return View(new NotificationDropdownVM());

            var notifications = _notificationService.GetRecentNotifications(userId.Value, 4);
            var unreadCount = _notificationService.GetUnreadCount(userId.Value);

            return View(new NotificationDropdownVM
            {
                Notifications = notifications,
                UnreadCount = unreadCount
            });
        }
    }
}