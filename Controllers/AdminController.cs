using Microsoft.AspNetCore.Mvc;
using Webgiasu.Models;
using Webgiasu.Models.ViewModels;
using Webgiasu.Services;

namespace Webgiasu.Controllers
{
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IProblemService _problemService;
        private readonly IPaymentService _paymentService;
        private readonly ISolutionService _solutionService;
        private readonly INotificationService _notificationService;

        public AdminController(IUserService userService, IProblemService problemService, 
            IPaymentService paymentService, ISolutionService solutionService, INotificationService notificationService)
        {
            _userService = userService;
            _problemService = problemService;
            _paymentService = paymentService;
            _solutionService = solutionService;
            _notificationService = notificationService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        public IActionResult Dashboard()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            var model = new AdminDashboardViewModel
            {
                TotalStudents = _userService.GetUsersByRole(UserRole.Student).Count,
                TotalTutors = _userService.GetUsersByRole(UserRole.Tutor).Count(u => u.IsApproved),
                TotalProblems = _problemService.GetAllProblems().Count,
                TotalRevenue = _paymentService.GetTotalRevenue(),
                PendingTutorApprovals = _userService.GetPendingTutors().Count,
                ActiveProblems = _problemService.GetAllProblems().Count(p => p.Status == ProblemStatus.InProgress || p.Status == ProblemStatus.WaitingForTutor)
            };

            return View(model);
        }

        public IActionResult ManageTutors()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            var tutors = _userService.GetUsersByRole(UserRole.Tutor);
            return View(tutors);
        }

        public IActionResult PendingTutors()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var pendingTutors = _userService.GetPendingTutors();
            ViewBag.PendingTutorApprovals = pendingTutors.Count;
            return View(pendingTutors);
        }

        [HttpPost]
        public IActionResult ApproveTutor(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            if (_userService.ApproveTutor(id))
            {
                TempData["Success"] = "Đã duyệt gia sư thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể duyệt gia sư!";
            }
            return RedirectToAction("PendingTutors");
        }

        public IActionResult ManageProblems()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            var problems = _problemService.GetAllProblems();
            return View(problems);
        }

        public IActionResult ProblemDetails(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            var problem = _problemService.GetProblemById(id);
            if (problem == null)
            {
                return NotFound();
            }

            var model = new ProblemDetailsViewModel
            {
                Problem = problem,
                Student = _userService.GetUserById(problem.StudentId),
                AssignedTutor = problem.AssignedTutorId.HasValue ? _userService.GetUserById(problem.AssignedTutorId.Value) : null,
                Solution = _solutionService.GetSolutionByProblemId(problem.Id)
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult DeleteProblem(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            if (_problemService.DeleteProblem(id))
            {
                TempData["Success"] = "Xóa bài toán thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể xóa bài toán!";
            }
            return RedirectToAction("ManageProblems");
        }

        public IActionResult ManagePayments()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            var payments = _paymentService.GetAllPayments();
            return View(payments);
        }

        public IActionResult Statistics()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            var model = new AdminStatisticsViewModel();

            // Problems by type
            var allProblems = _problemService.GetAllProblems();
            foreach (ProblemType type in Enum.GetValues(typeof(ProblemType)))
            {
                model.ProblemsByType[type] = allProblems.Count(p => p.Type == type);
            }

            // Revenue by month (last 6 months)
            var allPayments = _paymentService.GetAllPayments().Where(p => p.Status == PaymentStatus.Completed);
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                var monthKey = month.ToString("MM/yyyy");
                var revenue = allPayments.Where(p => p.CompletedDate.HasValue && 
                    p.CompletedDate.Value.Month == month.Month && 
                    p.CompletedDate.Value.Year == month.Year).Sum(p => p.Amount);
                model.RevenueByMonth[monthKey] = revenue;
            }

            // Top tutors
            var tutors = _userService.GetUsersByRole(UserRole.Tutor).Where(t => t.IsApproved).ToList();
            var tutorStats = new Dictionary<int, int>();
            foreach (var tutor in tutors)
            {
                var solutionCount = _solutionService.GetSolutionsByTutorId(tutor.Id).Count;
                tutorStats[tutor.Id] = solutionCount;
            }
            model.TopTutors = tutors.OrderByDescending(t => tutorStats.ContainsKey(t.Id) ? tutorStats[t.Id] : 0).Take(5).ToList();

            return View(model);
        }

        public IActionResult ManageUsers()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            var users = _userService.GetAllUsers();
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;
            return View(new AdminCreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(AdminCreateUserViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            if (string.IsNullOrWhiteSpace(model.Username) ||
                string.IsNullOrWhiteSpace(model.Password) ||
                string.IsNullOrWhiteSpace(model.FullName))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin bắt buộc!";
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                TempData["Error"] = "Mật khẩu xác nhận không khớp!";
                return View(model);
            }

            var usernameExists = _userService.GetAllUsers()
                .Any(u => u.Username.Equals(model.Username, StringComparison.OrdinalIgnoreCase));
            if (usernameExists)
            {
                TempData["Error"] = "Tên đăng nhập đã tồn tại!";
                return View(model);
            }

            var user = new User
            {
                Username = model.Username.Trim(),
                Password = model.Password,
                FullName = model.FullName.Trim(),
                Email = model.Email?.Trim() ?? string.Empty,
                PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty,
                Role = model.Role,
                IsApproved = model.Role == UserRole.Tutor ? model.IsApproved : true,
                Level = (model.Role == UserRole.Student || model.Role == UserRole.Tutor) ? model.Level : null,
                RegisteredDate = DateTime.Now
            };

            if (_userService.Register(user))
            {
                TempData["Success"] = "Tạo người dùng thành công!";
                return RedirectToAction("ManageUsers");
            }

            TempData["Error"] = "Không thể tạo người dùng!";
            return View(model);
        }

        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            var user = _userService.GetUserById(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("ManageUsers");
            }

            var model = new AdminEditUserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsApproved = user.IsApproved,
                Level = user.Level
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(AdminEditUserViewModel model)
        {
            var currentAdminId = GetCurrentUserId();
            if (currentAdminId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            var existing = _userService.GetUserById(model.Id);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("ManageUsers");
            }

            var usernameExists = _userService.GetAllUsers()
                .Any(u => u.Id != model.Id && u.Username.Equals(model.Username, StringComparison.OrdinalIgnoreCase));
            if (usernameExists)
            {
                TempData["Error"] = "Tên đăng nhập đã tồn tại!";
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.Password) && model.Password != model.ConfirmPassword)
            {
                TempData["Error"] = "Mật khẩu xác nhận không khớp!";
                return View(model);
            }

            if (existing.Role == UserRole.Admin && model.Role != UserRole.Admin)
            {
                var adminCount = _userService.GetUsersByRole(UserRole.Admin).Count;
                if (adminCount <= 1)
                {
                    TempData["Error"] = "Không thể thay đổi vai trò của admin cuối cùng!";
                    return View(model);
                }
            }

            existing.Username = model.Username.Trim();
            existing.FullName = model.FullName.Trim();
            existing.Email = model.Email?.Trim() ?? string.Empty;
            existing.PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty;
            existing.Role = model.Role;
            existing.IsApproved = model.Role == UserRole.Tutor ? model.IsApproved : true;
            existing.Level = (model.Role == UserRole.Student || model.Role == UserRole.Tutor) ? model.Level : null;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                existing.Password = model.Password;
            }

            if (_userService.UpdateUser(existing))
            {
                TempData["Success"] = "Cập nhật người dùng thành công!";
                return RedirectToAction("ManageUsers");
            }

            TempData["Error"] = "Không thể cập nhật người dùng!";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            var currentAdminId = GetCurrentUserId();
            if (currentAdminId == 0) return RedirectToAction("Login", "Account");

            if (id == currentAdminId)
            {
                TempData["Error"] = "Bạn không thể tự xóa tài khoản của chính mình!";
                return RedirectToAction("ManageUsers");
            }

            var user = _userService.GetUserById(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("ManageUsers");
            }

            if (user.Role == UserRole.Admin)
            {
                var adminCount = _userService.GetUsersByRole(UserRole.Admin).Count;
                if (adminCount <= 1)
                {
                    TempData["Error"] = "Không thể xóa admin cuối cùng!";
                    return RedirectToAction("ManageUsers");
                }
            }

            if (_userService.DeleteUser(id))
            {
                TempData["Success"] = "Xóa người dùng thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể xóa người dùng. Có thể tài khoản đang phát sinh dữ liệu liên quan!";
            }

            return RedirectToAction("ManageUsers");
        }

        public IActionResult Reports()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            // TODO: Implement report model and logic
            return View();
        }

        public IActionResult Settings()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            // TODO: Implement settings model and logic
            return View();
        }

        public IActionResult Logs()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;

            // TODO: Implement logs model and logic
            return View();
        }

        public IActionResult Notifications()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.PendingTutorApprovals = _userService.GetPendingTutors().Count;
            
            var notifications = _notificationService.GetRecentNotifications(userId, 50);
            return View(notifications);
        }

        [HttpPost]
        public IActionResult MarkAllNotificationsAsRead()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            _notificationService.MarkAllAsRead(userId);
            return RedirectToAction("Notifications");
        }

        [HttpPost]
        public IActionResult MarkNotificationAsRead(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            _notificationService.MarkAsRead(id);
            return Ok();
        }
    }
}
