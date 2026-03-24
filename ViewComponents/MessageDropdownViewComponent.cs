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

        // Determine target controller based on session role
        var userRole = HttpContext.Session.GetString("UserRole");
        var targetController = userRole == "Tutor" ? "Tutor" : "Student";

        return View(new MessageDropdownVM
        {
            Messages = messages,
            UnreadCount = unreadCount,
            ControllerName = targetController
        });
    }
}