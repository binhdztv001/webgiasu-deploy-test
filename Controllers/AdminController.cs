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

        public AdminController(IUserService userService, IProblemService problemService, 
            IPaymentService paymentService, ISolutionService solutionService)
        {
            _userService = userService;
            _problemService = problemService;
            _paymentService = paymentService;
            _solutionService = solutionService;
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
    }
}
