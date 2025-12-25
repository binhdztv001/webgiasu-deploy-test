using Microsoft.AspNetCore.Mvc;
using Webgiasu.Models;
using Webgiasu.Models.ViewModels;
using Webgiasu.Services;

namespace Webgiasu.Controllers
{
    public class TutorController : Controller
    {
        private readonly IProblemService _problemService;
        private readonly ISolutionService _solutionService;
        private readonly IUserService _userService;
        private readonly IRatingService _ratingService;

        public TutorController(IProblemService problemService, ISolutionService solutionService, 
            IUserService userService, IRatingService ratingService)
        {
            _problemService = problemService;
            _solutionService = solutionService;
            _userService = userService;
            _ratingService = ratingService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var model = new TutorDashboardViewModel
            {
                AvailableProblems = _problemService.GetAvailableProblems(),
                MyAssignedProblems = _problemService.GetProblemsByTutorId(userId),
                MySolutions = _solutionService.GetSolutionsByTutorId(userId)
            };

            // Calculate total earnings
            model.TotalEarnings = 0;
            foreach (var solution in model.MySolutions)
            {
                var problem = _problemService.GetProblemById(solution.ProblemId);
                if (problem != null)
                {
                    model.TotalEarnings += (int)problem.Price;
                }
            }

            // Get average rating
            var avgRating = await _ratingService.GetAverageRatingForTutorAsync(userId);
            ViewBag.AverageRating = avgRating;
            ViewBag.TotalRatings = (await _ratingService.GetRatingsByTutorIdAsync(userId)).Count;

            return View(model);
        }

        public IActionResult AvailableProblems()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var problems = _problemService.GetAvailableProblems();
            return View(problems);
        }

        public IActionResult ProblemDetails(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

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
        public IActionResult AcceptProblem(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            if (_problemService.AssignProblemToTutor(id, userId))
            {
                TempData["Success"] = "Bạn đã nhận bài toán thành công!";
                return RedirectToAction("MyProblems");
            }
            else
            {
                TempData["Error"] = "Không thể nhận bài toán này!";
                return RedirectToAction("AvailableProblems");
            }
        }

        public IActionResult MyProblems()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var problems = _problemService.GetProblemsByTutorId(userId);
            return View(problems);
        }

        [HttpGet]
        public IActionResult SubmitSolution(int? problemId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            // If no problemId provided, redirect to MyProblems
            if (!problemId.HasValue)
            {
                TempData["Warning"] = "Vui lòng chọn bài toán để gửi lời giải!";
                return RedirectToAction("MyProblems");
            }

            var problem = _problemService.GetProblemById(problemId.Value);
            if (problem == null)
            {
                TempData["Error"] = "Không tìm thấy bài toán!";
                return RedirectToAction("MyProblems");
            }

            // Check if tutor is assigned to this problem
            if (problem.AssignedTutorId != userId)
            {
                TempData["Error"] = "Bạn không có quyền gửi lời giải cho bài toán này!";
                return RedirectToAction("MyProblems");
            }

            var model = new SubmitSolutionViewModel
            {
                ProblemId = problemId.Value
            };

            ViewBag.Problem = problem;
            return View(model);
        }

        [HttpPost]
        public IActionResult SubmitSolution(SubmitSolutionViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var solution = new Solution
                {
                    ProblemId = model.ProblemId,
                    TutorId = userId,
                    Content = model.Content,
                    FileUrl = model.SolutionFile != null ? $"/files/{model.SolutionFile.FileName}" : ""
                };

                if (_solutionService.CreateSolution(solution))
                {
                    // Update problem status
                    var problem = _problemService.GetProblemById(model.ProblemId);
                    if (problem != null)
                    {
                        problem.Status = ProblemStatus.Solved;
                        _problemService.UpdateProblem(problem);
                    }

                    TempData["Success"] = "Gửi lời giải thành công!";
                    return RedirectToAction("MyProblems");
                }
            }

            var prob = _problemService.GetProblemById(model.ProblemId);
            ViewBag.Problem = prob;
            return View(model);
        }

        public IActionResult MySolutions()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var solutions = _solutionService.GetSolutionsByTutorId(userId);
            return View(solutions);
        }

        // Profile Management
        public IActionResult Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var user = _userService.GetUserById(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public IActionResult UpdateProfile(string fullName, string email, string phoneNumber)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var user = _userService.GetUserById(userId);
            if (user != null)
            {
                user.FullName = fullName;
                user.Email = email;
                user.PhoneNumber = phoneNumber;
                
                _userService.UpdateUser(user);
                HttpContext.Session.SetString("UserName", user.FullName);
                
                TempData["Success"] = "Cập nhật hồ sơ thành công!";
            }
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var user = _userService.GetUserById(userId);
            if (user != null)
            {
                if (user.Password == currentPassword)
                {
                    user.Password = newPassword;
                    _userService.UpdateUser(user);
                    TempData["Success"] = "Đổi mật khẩu thành công!";
                }
                else
                {
                    TempData["Error"] = "Mật khẩu hiện tại không đúng!";
                }
            }
            return RedirectToAction("Profile");
        }

        // NEW: View My Ratings
        [HttpGet]
        public async Task<IActionResult> MyRatings()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var model = await _ratingService.GetTutorRatingsSummaryAsync(userId);
            return View(model);
        }

        // Messages
        public IActionResult Messages()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            // TODO: Implement messaging logic
            return View();
        }

        // Earnings / Payment History
        public IActionResult Earnings()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var solutions = _solutionService.GetSolutionsByTutorId(userId);
            var model = new TutorEarningsViewModel
            {
                Solutions = solutions,
                TotalEarnings = 0
            };

            foreach (var solution in solutions)
            {
                var problem = _problemService.GetProblemById(solution.ProblemId);
                if (problem != null && problem.Status == ProblemStatus.Solved)
                {
                    model.TotalEarnings += (int)problem.Price;
                }
            }

            return View(model);
        }

        // Account Settings
                public IActionResult Settings()
                {
                    var userId = GetCurrentUserId();
                    if (userId == 0) return RedirectToAction("Login", "Account");

                    var user = _userService.GetUserById(userId);
                    if (user == null) return NotFound();

                    return View(user);
                }

                [HttpPost]
                public IActionResult UpdateTutorProfile(string? bio, string? subjects, string? education, int? experienceYears, string? certificates)
                {
                    var userId = GetCurrentUserId();
                    if (userId == 0) return RedirectToAction("Login", "Account");

                    var user = _userService.GetUserById(userId);
                    if (user != null)
                    {
                        user.Bio = bio;
                        user.Subjects = subjects;
                        user.Education = education;
                        user.ExperienceYears = experienceYears;
                        user.Certificates = certificates;
                
                        _userService.UpdateUser(user);
                        TempData["Success"] = "Cập nhật hồ sơ gia sư thành công!";
                    }
                    return RedirectToAction("Settings");
                }
            }
        }
