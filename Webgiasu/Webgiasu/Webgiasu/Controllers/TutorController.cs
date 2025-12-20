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
        private readonly IFriendshipService _friendshipService;
        private readonly IRatingService _ratingService;
        private readonly IMessageService _messageService;

        public TutorController(IProblemService problemService, ISolutionService solutionService,
            IFriendshipService friendshipService,
            IUserService userService, IRatingService ratingService, IMessageService messageService)
        {
            _problemService = problemService;
            _solutionService = solutionService;
            _userService = userService;
            _friendshipService = friendshipService;
            _ratingService = ratingService;
            _messageService = messageService;
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
        public IActionResult Messages(int? userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return RedirectToAction("Login", "Account");

            // Lấy danh sách bạn bè
            var friends = _friendshipService.GetFriends(currentUserId);
            ViewBag.Friends = friends;

            // Lấy danh sách conversations (bạn bè đã chat)
            var conversations = _messageService.GetConversations(currentUserId);
            ViewBag.Conversations = conversations;

            // Nếu có userId được chọn, lấy tin nhắn với user đó
            if (userId.HasValue && userId.Value > 0)
            {
                var selectedUser = _userService.GetUserById(userId.Value);

                // Kiểm tra có phải bạn bè không
                if (selectedUser != null && _friendshipService.AreFriends(currentUserId, userId.Value))
                {
                    ViewBag.SelectedUser = selectedUser;
                    ViewBag.SelectedUserId = userId.Value;

                    var messages = _messageService.GetMessages(currentUserId, userId.Value);
                    ViewBag.Messages = messages;
                }
            }
            else if (conversations.Any())
            {
                // Tự động chọn conversation đầu tiên
                var firstUser = conversations.First();
                ViewBag.SelectedUser = firstUser;
                ViewBag.SelectedUserId = firstUser.Id;

                var messages = _messageService.GetMessages(currentUserId, firstUser.Id);
                ViewBag.Messages = messages;
            }

            // Lấy tổng số tin nhắn chưa đọc
            ViewBag.TotalUnreadCount = _messageService.GetTotalUnreadCount(currentUserId);
            ViewBag.CurrentUserId = currentUserId;

            return View();
        }

        public IActionResult FriendShip()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            // Lấy tất cả users (bao gồm cả Student và Tutor) trừ user đang đăng nhập
            var allUsers = _userService.GetUsersByRole(UserRole.Tutor)
                .Where(u => u.Id != userId) // Loại bỏ user đang đăng nhập
                .ToList();

            // Lấy danh sách lời mời nhận được
            var receivedRequests = _friendshipService.GetReceivedFriendRequests(userId);
            ViewBag.ReceivedRequests = receivedRequests;
            ViewBag.ReceivedCount = receivedRequests.Count;

            // Lấy danh sách lời mời đã gửi
            var sentRequests = _friendshipService.GetSentFriendRequests(userId);
            ViewBag.SentRequests = sentRequests;
            ViewBag.SentCount = sentRequests.Count;

            // Lấy danh sách bạn bè
            var friends = _friendshipService.GetFriends(userId);
            ViewBag.Friends = friends;
            ViewBag.FriendsCount = friends.Count;

            // Lấy trạng thái kết bạn cho tất cả users
            var friendshipStatuses = new Dictionary<int, FriendshipStatus?>();
            foreach (var user in allUsers)
            {
                friendshipStatuses[user.Id] = _friendshipService.GetFriendshipStatus(userId, user.Id);
            }
            ViewBag.FriendshipStatuses = friendshipStatuses;

            return View(allUsers);
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

        // Premium
        public IActionResult Premium()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var user = _userService.GetUserById(userId);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // Fake for testing, change code after adding payment function
        [HttpPost]
        public IActionResult UpgradePremium()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var user = _userService.GetUserById(userId);
            if (user == null)
                return NotFound();

            // FAKE PAYMENT
            user.IsPremium = true;
            user.PremiumExpiredAt = DateTime.Now.AddMonths(1); // demo 1 tháng

            _userService.UpdateUser(user);

            HttpContext.Session.SetString("IsPremium", "true");

            TempData["Success"] = "Nâng cấp Premium thành công!";
            return RedirectToAction("Premium");
        }
    }
}
