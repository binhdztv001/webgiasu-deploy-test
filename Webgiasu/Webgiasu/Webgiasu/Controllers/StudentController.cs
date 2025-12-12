using Microsoft.AspNetCore.Mvc;
using Webgiasu.Models;
using Webgiasu.Models.ViewModels;
using Webgiasu.Services;

namespace Webgiasu.Controllers
{
    public class StudentController : Controller
    {
        private readonly IProblemService _problemService;
        private readonly ISolutionService _solutionService;
        private readonly IPaymentService _paymentService;
        private readonly IUserService _userService;
        private readonly IRatingService _ratingService;

        public StudentController(IProblemService problemService, ISolutionService solutionService, 
            IPaymentService paymentService, IUserService userService, IRatingService ratingService)
        {
            _problemService = problemService;
            _solutionService = solutionService;
            _paymentService = paymentService;
            _userService = userService;
            _ratingService = ratingService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        public IActionResult Dashboard()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var model = new StudentDashboardViewModel
            {
                MyProblems = _problemService.GetProblemsByStudentId(userId),
                Solutions = new List<Solution>(),
                Payments = _paymentService.GetPaymentsByStudentId(userId)
            };

            // Get solutions for student's problems
            foreach (var problem in model.MyProblems)
            {
                var solution = _solutionService.GetSolutionByProblemId(problem.Id);
                if (solution != null)
                {
                    model.Solutions.Add(solution);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateProblem()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            return View(new CreateProblemViewModel());
        }

        [HttpPost]
        public IActionResult CreateProblem(CreateProblemViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                // Calculate price based on difficulty
                decimal price = model.Difficulty switch
                {
                    DifficultyLevel.Easy => 40000,
                    DifficultyLevel.Medium => 60000,
                    DifficultyLevel.Hard => 90000,
                    _ => 50000
                };

                var problem = new Problem
                {
                    StudentId = userId,
                    Title = model.Title,
                    Description = model.Description,
                    Type = model.Type,
                    Difficulty = model.Difficulty,
                    ImageUrl = model.ImageFile != null ? $"/images/{model.ImageFile.FileName}" : "/images/default.jpg",
                    Deadline = model.Deadline,
                    Price = price
                };

                if (_problemService.CreateProblem(problem))
                {
                    // Create payment record
                    var payment = new Payment
                    {
                        StudentId = userId,
                        ProblemId = problem.Id,
                        Amount = price
                    };
                    _paymentService.CreatePayment(payment);

                    TempData["Success"] = "Đăng bài toán thành công!";
                    return RedirectToAction("Dashboard");
                }
            }
            return View(model);
        }

        public async Task<IActionResult> ProblemDetails(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var problem = _problemService.GetProblemById(id);
            if (problem == null || problem.StudentId != userId)
            {
                return NotFound();
            }

            var model = new ProblemDetailsViewModel
            {
                Problem = problem,
                Student = _userService.GetUserById(problem.StudentId),
                AssignedTutor = problem.AssignedTutorId.HasValue ? _userService.GetUserById(problem.AssignedTutorId.Value) : null,
                Solution = _solutionService.GetSolutionByProblemId(problem.Id),
                Payment = _paymentService.GetPaymentByProblemId(problem.Id)
            };

            // Check if student has rated this problem
            ViewBag.HasRated = await _ratingService.HasStudentRatedProblemAsync(id, userId);

            return View(model);
        }

        // NEW: Rate Tutor Action
        [HttpGet]
        public async Task<IActionResult> RateTutor(int problemId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var problem = _problemService.GetProblemById(problemId);
            if (problem == null || problem.StudentId != userId || !problem.AssignedTutorId.HasValue)
            {
                TempData["Error"] = "Không tìm thấy bài toán hoặc chưa có gia sư nhận.";
                return RedirectToAction("Dashboard");
            }

            // Check if already rated
            if (await _ratingService.HasStudentRatedProblemAsync(problemId, userId))
            {
                TempData["Warning"] = "Bạn đã đánh giá gia sư cho bài toán này rồi!";
                return RedirectToAction("ProblemDetails", new { id = problemId });
            }

            // Check if solution exists and problem is solved
            var solution = _solutionService.GetSolutionByProblemId(problemId);
            if (solution == null || problem.Status != ProblemStatus.Solved)
            {
                TempData["Warning"] = "Bài toán chưa được hoàn thành!";
                return RedirectToAction("ProblemDetails", new { id = problemId });
            }

            var tutor = _userService.GetUserById(problem.AssignedTutorId.Value);

            var model = new RatingViewModel
            {
                ProblemId = problemId,
                ProblemTitle = problem.Title,
                TutorId = problem.AssignedTutorId.Value,
                TutorName = tutor?.FullName
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RateTutor(RatingViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Stars < 1 || model.Stars > 5)
            {
                TempData["Error"] = "Đánh giá phải từ 1 đến 5 sao!";
                return View(model);
            }

            var problem = _problemService.GetProblemById(model.ProblemId);
            if (problem == null || problem.StudentId != userId)
            {
                TempData["Error"] = "Không hợp lệ!";
                return RedirectToAction("Dashboard");
            }

            var success = await _ratingService.AddRatingAsync(
                model.ProblemId, 
                userId, 
                model.TutorId, 
                model.Stars, 
                model.Comment
            );

            if (success)
            {
                TempData["Success"] = "Đánh giá gia sư thành công! Cảm ơn phản hồi của bạn.";
                return RedirectToAction("ProblemDetails", new { id = model.ProblemId });
            }
            else
            {
                TempData["Error"] = "Bạn đã đánh giá bài toán này rồi!";
                return RedirectToAction("ProblemDetails", new { id = model.ProblemId });
            }
        }

        public IActionResult Payments()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var payments = _paymentService.GetPaymentsByStudentId(userId);
            return View(payments);
        }

        [HttpPost]
        public IActionResult ProcessPayment(int paymentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var payment = _paymentService.GetPaymentById(paymentId);
            if (payment != null && payment.StudentId == userId)
            {
                _paymentService.UpdatePaymentStatus(paymentId, PaymentStatus.Completed);
                TempData["Success"] = "Thanh toán thành công!";
            }
            return RedirectToAction("Payments");
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

        // NEW ACTIONS
        public IActionResult MyProblems()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var problems = _problemService.GetProblemsByStudentId(userId);
            return View(problems);
        }

        public IActionResult BrowseTutors()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var tutors = _userService.GetUsersByRole(UserRole.Tutor);
            return View(tutors);
        }

        public IActionResult Messages()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            // TODO: Implement messaging functionality
            return View();
        }

        public IActionResult RateTutors()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var problems = _problemService.GetProblemsByStudentId(userId)
                .Where(p => p.Status == ProblemStatus.Solved)
                .ToList();
            
            // Get all tutors for displaying in view
            var tutorIds = problems.Where(p => p.AssignedTutorId.HasValue)
                                   .Select(p => p.AssignedTutorId.Value)
                                   .Distinct()
                                   .ToList();
            
            var tutors = new List<User>();
            foreach (var tutorId in tutorIds)
            {
                var tutor = _userService.GetUserById(tutorId);
                if (tutor != null)
                {
                    tutors.Add(tutor);
                }
            }
            
            ViewBag.Tutors = tutors;
            
            return View(problems);
        }

        public IActionResult Settings()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var user = _userService.GetUserById(userId);
            return View(user);
        }

        public IActionResult PaymentHistory()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var payments = _paymentService.GetPaymentsByStudentId(userId);
            return View(payments);
        }
    }
}
