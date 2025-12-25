using Microsoft.AspNetCore.Mvc;
using Webgiasu.Models.ViewModels;
using Webgiasu.Services;
using Webgiasu.Models;

public class MessageDropdownViewComponent : ViewComponent
{
    private readonly IMessageService _messageService;
    private readonly IUserService _userService;

    public MessageDropdownViewComponent(IMessageService messageService, IUserService userService)
    {
        _messageService = messageService;
        _userService = userService;
    }

    public IViewComponentResult Invoke()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return View(new MessageDropdownVM());

        var messages = _messageService.GetRecentMessagesForDropdown(userId.Value);
        var unreadCount = _messageService.GetTotalUnreadCount(userId.Value);

        // Xác định controller đích theo role của user
        var user = _userService.GetUserById(userId.Value);
        var targetController = "Student";
        if (user != null && user.Role == UserRole.Tutor)
        {
            targetController = "Tutor";
        }

        ViewBag.MessageController = targetController;

        return View(new MessageDropdownVM
        {
            Messages = messages,
            UnreadCount = unreadCount
        });
    }
}