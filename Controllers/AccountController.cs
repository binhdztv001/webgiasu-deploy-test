using Microsoft.AspNetCore.Mvc;
using Webgiasu.Models;
using Webgiasu.Models.ViewModels;
using Webgiasu.Services;

namespace Webgiasu.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login(string role = "Student")
        {
            var model = new LoginViewModel();
            if (Enum.TryParse<UserRole>(role, out var userRole))
            {
                model.Role = userRole;
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _userService.Login(model.Username, model.Password, model.Role);
                if (user != null)
                {
                    if (user.Role == UserRole.Tutor && !user.IsApproved)
                    {
                        TempData["Error"] = "Tài khoản Mentor của bạn chưa được duyệt. Vui lòng chờ admin phê duyệt.";
                        return View(model);
                    }

                    // Store user info in session
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.FullName);
                    HttpContext.Session.SetString("UserRole", user.Role.ToString());

                    // Redirect based on role
                    return user.Role switch
                    {
                        UserRole.Student => RedirectToAction("Dashboard", "Student"),
                        UserRole.Tutor => RedirectToAction("Dashboard", "Tutor"),
                        UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
                        UserRole.School => RedirectToAction("Dashboard", "School"),
                        _ => RedirectToAction("Index", "Home")
                    };
                }
                else
                {
                    TempData["Error"] = "Tên đăng nhập hoặc mật khẩu không đúng!";
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string role = "Student")
        {
            var model = new RegisterViewModel();
            if (Enum.TryParse<UserRole>(role, out var userRole))
            {
                model.Role = userRole;
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Password != model.ConfirmPassword)
                {
                    TempData["Error"] = "Mật khẩu xác nhận không khớp!";
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    Password = model.Password,
                    FullName = model.FullName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Role = model.Role
                };

                if (_userService.Register(user))
                {
                if (model.Role == UserRole.Tutor)
                {
                    TempData["Success"] = "Đăng ký thành công! Vui lòng chờ admin duyệt tài khoản Mentor của bạn.";
                    }
                    else
                    {
                        TempData["Success"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";
                    }
                    return RedirectToAction("Login", new { role = model.Role.ToString() });
                }
                else
                {
                    TempData["Error"] = "Tên đăng nhập đã tồn tại!";
                }
            }
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
