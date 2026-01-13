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
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Vui lòng nhập đầy đủ thông tin!";
                    return View(model);
                }

                // Validate Level for Student and Tutor
                if ((model.Role == UserRole.Student || model.Role == UserRole.Tutor) && !model.Level.HasValue)
                {
                    TempData["Error"] = "Vui lòng chọn cấp học!";
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    Password = model.Password, // TODO: Hash password
                    FullName = model.FullName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Role = model.Role,
                    Level = model.Level, // ✅ Lưu Level
                    RegisteredDate = DateTime.Now,
                    IsApproved = model.Role == UserRole.Student
                };

                if (_userService.Register(user))
                {
                    TempData["Success"] = "Đăng ký thành công!";
                    return RedirectToAction("Login", new { role = model.Role });
                }
                else
                {
                    TempData["Error"] = "Tên đăng nhập đã tồn tại!";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Đã xảy ra lỗi: {ex.Message}";
                return View(model);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
