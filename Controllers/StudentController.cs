using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Webgiasu.Hubs;
using Webgiasu.Models;
using Webgiasu.Models.ViewModels;
using Webgiasu.Services;

namespace Webgiasu.Controllers
{
    // ✅ ADD: Missing model classes
    

    public class StudentController : Controller
    {
        public class ReactionRequest
        {
            public int PostId { get; set; }
            public int CommentId { get; set; }
            public string ReactionType { get; set; } = "like";
        }

        public class CommentCreateModel
        {
            public int PostId { get; set; }
            public string Content { get; set; } = "";
            public bool IsAnonymous { get; set; } = true;
        }

        private readonly AppDbContext _db;
        private readonly IProblemService _problemService;
        private readonly ISolutionService _solutionService;
        private readonly IPaymentService _paymentService;
        private readonly IUserService _userService;
        private readonly IRatingService _ratingService;
        private readonly IFriendshipService _friendshipService;
        private readonly IMessageService _messageService;
        private readonly ICommunityService _communityService;
        private readonly IHubContext<CommunityHub> _hubContext;
        private readonly IProblemGroupService _problemGroupService;
        private readonly ISePayGateway _sePayGateway;
        private readonly IPremiumService _premiumService;
        private readonly INotificationService _notificationService;
        private readonly ITutorApplicationService _tutorApplicationService;
        private readonly IStatisticsExportService _statisticsExportService;


        public StudentController(AppDbContext db, IProblemService problemService, ISolutionService solutionService, 
            IPaymentService paymentService, IUserService userService, IRatingService ratingService,
            IFriendshipService friendshipService, IMessageService messageService, IPremiumService premiumService,
            ICommunityService communityService, IHubContext<CommunityHub> hubContext, IProblemGroupService problemGroupService, ISePayGateway sePayGateway
            , INotificationService notificationService, ITutorApplicationService tutorApplicationService, IStatisticsExportService statisticsExportService)
        {
            _db = db;
            _problemService = problemService;
            _solutionService = solutionService;
            _paymentService = paymentService;
            _userService = userService;
            _ratingService = ratingService;
            _friendshipService = friendshipService;
            _messageService = messageService;
            _communityService = communityService;
            _premiumService = premiumService;
            _hubContext = hubContext;
            _problemGroupService = problemGroupService;
            _sePayGateway = sePayGateway;
            _notificationService = notificationService;
            _tutorApplicationService = tutorApplicationService;
            _statisticsExportService = statisticsExportService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        public IActionResult Dashboard()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Dashboard: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang chủ!";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult CreateProblem()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                return View(new CreateProblemViewModel());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CreateProblem GET: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        public async Task<IActionResult> ProblemDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var problem = _problemService.GetProblemById(id);
                if (problem == null)
                {
                    return NotFound();
                }

                // ✅ Kiểm tra quyền truy cập: Owner HOẶC thành viên nhóm
                bool hasAccess = problem.StudentId == userId; // Owner

                if (!hasAccess)
                {
                    // Kiểm tra xem user có phải là thành viên nhóm không
                    var group = _problemGroupService.GetGroupByProblemId(problem.Id);
                    if (group != null)
                    {
                        var members = _problemGroupService.GetGroupMembers(group.Id);
                        hasAccess = members.Any(m => m.UserId == userId);
                    }
                }

                if (!hasAccess)
                {
                    TempData["Error"] = "Bạn không có quyền xem bài toán này!";
                    return RedirectToAction("Dashboard");
                }

                // ✅ Load group info nếu là bài toán nhóm
                var groupInfo = _problemGroupService.GetGroupByProblemId(problem.Id);
                bool isPremium = _premiumService.IsPremium(userId);

                var model = new ProblemDetailsViewModel
                {
                    Problem = problem,
                    Student = _userService.GetUserById(problem.StudentId),
                    AssignedTutor = isPremium && problem.AssignedTutorId.HasValue
                        ? _userService.GetUserById(problem.AssignedTutorId.Value)
                        : null,
                    Solution = _solutionService.GetSolutionByProblemId(problem.Id),
                    Payment = _paymentService.GetPaymentByProblemId(problem.Id),
                    Group = groupInfo, // ✅ Thêm thông tin nhóm
                    GroupMembers = groupInfo != null ? _problemGroupService.GetGroupMembers(groupInfo.Id) : null
                };

                if (problem.Status == ProblemStatus.WaitingForTutor)
                {
                    var apps = _tutorApplicationService.GetApplicationsForProblem(id, false);
                    ViewBag.ApplicationCount = apps.Count(a => a.Status == ApplicationStatus.Pending);
                }

                // Check if student has rated this problem
                ViewBag.HasRated = await _ratingService.HasStudentRatedProblemAsync(id, userId);
                ViewBag.IsPremium = isPremium;
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ProblemDetails: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải chi tiết bài toán!";
                return RedirectToAction("Dashboard");
            }
        }

        // NEW: Rate Tutor Action
        [HttpGet]
        public async Task<IActionResult> RateTutor(int problemId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var problem = _problemService.GetProblemById(problemId);
                if (problem == null || problem.StudentId != userId || !problem.AssignedTutorId.HasValue)
                {
                    TempData["Error"] = "Không tìm thấy bài toán hoặc chưa có Mentor nhận.";
                    return RedirectToAction("Dashboard");
                }

            // Check if already rated
                if (await _ratingService.HasStudentRatedProblemAsync(problemId, userId))
                {
                    TempData["Warning"] = "Bạn đã đánh giá Mentor cho bài toán này rồi!";
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in RateTutor GET: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RateTutor(RatingViewModel model)
        {
            try
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
                    // ✅ NEW: Tạo thông báo cho Tutor khi nhận được đánh giá
                    var student = _userService.GetUserById(userId);
                    if (student != null)
                    {
                        _notificationService.NotifyTutorRatingReceived(
                            model.TutorId,
                            model.ProblemId,
                            student.FullName,
                            model.Stars
                        );
                    }

                    TempData["Success"] = "Đánh giá Mentor thành công! Cảm ơn phản hồi của bạn.";
                    return RedirectToAction("ProblemDetails", new { id = model.ProblemId });
                }
                else
                {
                    TempData["Error"] = "Bạn đã đánh giá bài toán này rồi!";
                    return RedirectToAction("ProblemDetails", new { id = model.ProblemId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in RateTutor POST: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi đánh giá!";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult Payments()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var payments = _paymentService.GetPaymentsByStudentId(userId);
                return View(payments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Payments: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách thanh toán!";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public IActionResult ProcessPayment(int paymentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var payment = _paymentService.GetPaymentById(paymentId);
            if (payment != null && payment.StudentId == userId)
            {
                return RedirectToAction("PaymentCheckout", new { paymentId });
            }
            TempData["Error"] = "Không tìm thấy giao dịch hoặc không hợp lệ.";
            return RedirectToAction("Payments");
        }

        [HttpGet]
        public IActionResult PaymentCheckout(int paymentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var payment = _paymentService.GetPaymentById(paymentId);
            if (payment == null || payment.StudentId != userId || payment.Status != PaymentStatus.Pending)
                return NotFound();

            var successUrl = Url.Action("PaymentResult", "Student",
                new { paymentId, status = "success" }, Request.Scheme);

            var errorUrl = Url.Action("PaymentResult", "Student",
                new { paymentId, status = "error" }, Request.Scheme);

            var cancelUrl = Url.Action("PaymentResult", "Student",
                new { paymentId, status = "cancel" }, Request.Scheme);

            var checkout = _sePayGateway.BuildCheckout(
                payment,
                successUrl!,
                errorUrl!,
                cancelUrl!
            );

            return View(checkout);
        }


        [HttpGet]
        public IActionResult PaymentResult(int paymentId, string status)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var payment = _paymentService.GetPaymentById(paymentId);
            if (payment == null || payment.StudentId != userId)
            {
                return NotFound();
            }

            if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Success"] = "Thanh toán đang được xác nhận. Vui lòng đợi webhook cập nhật trạng thái.";
            }
            else if (string.Equals(status, "cancel", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Warning"] = "Bạn đã hủy thanh toán.";
            }
            else
            {
                TempData["Error"] = "Thanh toán không thành công.";
            }

            return RedirectToAction("Payments");
        }

        // Profile Management
        public IActionResult Profile()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                if (user == null) return NotFound();

                return View(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Profile: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải hồ sơ!";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public IActionResult UpdateProfile(string fullName, string email, string phoneNumber, int? level)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                if (user != null)
                {
                    user.FullName = fullName;
                    user.Email = email;
                    user.PhoneNumber = phoneNumber;

                    // Update Level
                    if (level.HasValue && Enum.IsDefined(typeof(EducationLevel), level.Value))
                    {
                        user.Level = (EducationLevel)level.Value;
                    }
                    else
                    {
                        user.Level = null;
                    }

                    _userService.UpdateUser(user);
                    HttpContext.Session.SetString("UserName", user.FullName);

                    TempData["Success"] = "Cập nhật hồ sơ thành công!";
                }
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in UpdateProfile: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật hồ sơ!";
                return RedirectToAction("Profile");
            }
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ChangePassword: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi đổi mật khẩu!";
                return RedirectToAction("Profile");
            }
        }

        // NEW ACTIONS
        public IActionResult MyProblems()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var problems = _problemService.GetProblemsByStudentId(userId);

                // Get groups user is member of
                var groups = _problemGroupService.GetGroupsByUserId(userId);
                var groupProblemIds = groups.Select(g => g.ProblemId).ToHashSet();
                var problemGroupMap = groups.ToDictionary(g => g.ProblemId, g => g.Id);

                // ✅ THÊM: Đếm số application cho mỗi bài
                var applicationCounts = new Dictionary<int, int>();
                foreach (var problem in problems.Where(p => p.Status == ProblemStatus.WaitingForTutor))
                {
                    var apps = _tutorApplicationService.GetApplicationsForProblem(problem.Id, false);
                    applicationCounts[problem.Id] = apps.Count(a => a.Status == ApplicationStatus.Pending);
                }

                ViewBag.GroupProblemIds = groupProblemIds;
                ViewBag.ProblemGroupMap = problemGroupMap;
                ViewBag.ApplicationCounts = applicationCounts; // ✅ THÊM

                return View(problems);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in MyProblems: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách bài toán!";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult BrowseTutors()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var tutors = _userService.GetUsersByRole(UserRole.Tutor);
                return View(tutors);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in BrowseTutors: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách Mentor!";
                return RedirectToAction("Dashboard");
            }
        }
        public IActionResult Messages(int? userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0) return RedirectToAction("Login", "Account");

                var friends = _friendshipService.GetFriends(currentUserId);
                ViewBag.Friends = friends;

                var conversations = _messageService.GetConversations(currentUserId);
                ViewBag.Conversations = conversations;

                if (userId.HasValue && userId.Value > 0)
                {
                    var selectedUser = _userService.GetUserById(userId.Value);

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
                    var firstUser = conversations.First();
                    ViewBag.SelectedUser = firstUser;
                    ViewBag.SelectedUserId = firstUser.Id;

                    var messages = _messageService.GetMessages(currentUserId, firstUser.Id);
                    ViewBag.Messages = messages;
                }

                ViewBag.TotalUnreadCount = _messageService.GetTotalUnreadCount(currentUserId);
                ViewBag.CurrentUserId = currentUserId;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Messages: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải tin nhắn!";
                return RedirectToAction("Dashboard");
            }
        }

        // API: Lấy tin nhắn với 1 user
        [HttpGet]
        public IActionResult GetMessages(int userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                if (!_friendshipService.AreFriends(currentUserId, userId))
                    return Json(new { success = false, message = "Bạn chỉ có thể chat với bạn bè!" });

                var messages = _messageService.GetMessages(currentUserId, userId);
                var currentUser = _userService.GetUserById(currentUserId);

                var messageList = messages.Select(m => new
                {
                    id = m.Id,
                    senderId = m.SenderId,
                    receiverId = m.ReceiverId,
                    content = m.Content,
                    sentDate = m.SentDate.ToString("HH:mm"),
                    sentDateFull = m.SentDate.ToString("dd/MM/yyyy HH:mm"),
                    isRead = m.IsRead,
                    isMine = m.SenderId == currentUserId,
                    senderName = m.Sender?.FullName,
                    senderAvatar = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(m.Sender?.FullName ?? "User")}&size=32"
                }).ToList();

                return Json(new
                {
                    success = true,
                    messages = messageList,
                    currentUserId = currentUserId,
                    currentUserName = currentUser?.FullName
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetMessages: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi tải tin nhắn!" });
            }
        }

        // API: Lấy danh sách conversations
        [HttpGet]
        public IActionResult GetConversations()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                var conversations = _messageService.GetConversations(currentUserId);

                var conversationList = conversations.Select(u => {
                    var lastMessage = _messageService.GetLastMessage(currentUserId, u.Id);
                    var unreadCount = _messageService.GetUnreadCount(currentUserId, u.Id);

                    return new
                    {
                        userId = u.Id,
                        userName = u.FullName,
                        avatar = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(u.FullName)}&size=40",
                        lastMessage = lastMessage?.Content ?? "",
                        lastMessageTime = lastMessage != null ? GetTimeAgo(lastMessage.SentDate) : "",
                        unreadCount = unreadCount,
                        role = u.Role.ToString()
                    };
                }).ToList();

                return Json(new
                {
                    success = true,
                    conversations = conversationList,
                    totalUnreadCount = _messageService.GetTotalUnreadCount(currentUserId)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetConversations: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        // Helper method cho time ago
        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "vừa xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} ngày";

            return dateTime.ToString("dd/MM/yyyy");
        }

        public IActionResult RateTutors()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in RateTutors: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        // ==================== STATISTICS ====================
        public IActionResult Statistics()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var problems = _problemService.GetProblemsByStudentId(userId);
                var payments = _paymentService.GetPaymentsByStudentId(userId);
                var groupPayments = _paymentService.GetGroupPaymentsByUserId(userId);

                // Thống kê tổng quan
                ViewBag.TotalProblems = problems.Count;
                ViewBag.SolvedProblems = problems.Count(p => p.Status == ProblemStatus.Solved);
                ViewBag.InProgressProblems = problems.Count(p => p.Status == ProblemStatus.InProgress);
                ViewBag.WaitingProblems = problems.Count(p => p.Status == ProblemStatus.WaitingForTutor);

                // Thống kê thanh toán
                ViewBag.TotalSpent = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount)
                                   + groupPayments.Where(gp => gp.Status == PaymentStatus.Completed).Sum(gp => gp.Amount);
                ViewBag.PendingPayments = payments.Count(p => p.Status == PaymentStatus.Pending)
                                        + groupPayments.Count(gp => gp.Status == PaymentStatus.Pending);

                // Thống kê theo môn học
                var problemsByType = problems.GroupBy(p => p.Type)
                    .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                    .ToList();
                ViewBag.ProblemsByType = (IEnumerable<dynamic>)problemsByType;

                // Thống kê theo độ khó
                var problemsByDifficulty = problems.GroupBy(p => p.Difficulty)
                    .Select(g => new { Difficulty = g.Key.ToString(), Count = g.Count() })
                    .ToList();
                ViewBag.ProblemsByDifficulty = (IEnumerable<dynamic>)problemsByDifficulty;

                // Thống kê theo thời gian (6 tháng gần nhất)
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);
                var problemsByMonth = problems.Where(p => p.CreatedDate >= sixMonthsAgo)
                    .GroupBy(p => new { p.CreatedDate.Year, p.CreatedDate.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Month}/{g.Key.Year}",
                        Count = g.Count(),
                        Order = g.Key.Year * 12 + g.Key.Month
                    })
                    .OrderBy(x => x.Order)
                    .ToList();
                ViewBag.ProblemsByMonth = (IEnumerable<dynamic>)problemsByMonth;

                // Thống kê chi tiêu theo tháng
                var spendingByMonth = payments.Where(p => p.Status == PaymentStatus.Completed && p.CompletedDate >= sixMonthsAgo)
                    .GroupBy(p => new { p.CompletedDate!.Value.Year, p.CompletedDate.Value.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Month}/{g.Key.Year}",
                        Amount = g.Sum(p => p.Amount),
                        Order = g.Key.Year * 12 + g.Key.Month
                    })
                    .OrderBy(x => x.Order)
                    .ToList();
                ViewBag.SpendingByMonth = (IEnumerable<dynamic>)spendingByMonth;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Statistics: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải thống kê!";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult Settings()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                return View(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Settings: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult PaymentHistory()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var payments = _paymentService.GetPaymentsByStudentId(userId);
                return View(payments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in PaymentHistory: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải lịch sử thanh toán!";
                return RedirectToAction("Dashboard");
            }
        }

        // ==================== FRIENDSHIP ACTIONS ====================

        public IActionResult FriendShip()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var allUsers = _userService.GetUsersByRole(UserRole.Student)
                    .Where(u => u.Id != userId)
                    .ToList();

                var receivedRequests = _friendshipService.GetReceivedFriendRequests(userId);
                ViewBag.ReceivedRequests = receivedRequests;
                ViewBag.ReceivedCount = receivedRequests.Count;

                var sentRequests = _friendshipService.GetSentFriendRequests(userId);
                ViewBag.SentRequests = sentRequests;
                ViewBag.SentCount = sentRequests.Count;

                var friends = _friendshipService.GetFriends(userId);
                ViewBag.Friends = friends;
                ViewBag.FriendsCount = friends.Count;

                var friendshipStatuses = new Dictionary<int, FriendshipStatus?>();
                foreach (var user in allUsers)
                {
                    friendshipStatuses[user.Id] = _friendshipService.GetFriendshipStatus(userId, user.Id);
                }
                ViewBag.FriendshipStatuses = friendshipStatuses;

                return View(allUsers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in FriendShip: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách bạn bè!";
                return RedirectToAction("Dashboard");
            }
        }

        // API: Gửi lời mời kết bạn
        [HttpPost]
        public IActionResult SendFriendRequest([FromBody] FriendRequestModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                var success = _friendshipService.SendFriendRequest(userId, model.UserId);

                if (success)
                {
                    // ✅ Tạo thông báo cho người nhận lời mời
                    var sender = _userService.GetUserById(userId);
                    if (sender != null)
                    {
                        _notificationService.NotifyFriendRequestSent(
                            model.UserId,
                            userId,
                            sender.FullName
                        );
                    }

                    return Json(new { success = true, message = "Đã gửi lời mời kết bạn!" });
                }
                else
                    return Json(new { success = false, message = "Không thể gửi lời mời. Có thể đã gửi trước đó!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SendFriendRequest: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        // API: Chấp nhận lời mời kết bạn
        [HttpPost]
        public IActionResult AcceptFriendRequest([FromBody] FriendshipActionModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                var success = _friendshipService.AcceptFriendRequest(model.FriendshipId, userId);

                if (success)
                {
                    // ✅ Tạo thông báo cho người gửi lời mời
                    var friendship = _friendshipService.GetFriendshipById(model.FriendshipId);
                    var accepter = _userService.GetUserById(userId);

                    if (friendship != null && accepter != null)
                    {
                        _notificationService.NotifyFriendRequestAccepted(
                            friendship.RequesterId,
                            userId,
                            accepter.FullName
                        );
                    }

                    return Json(new { success = true, message = "Đã chấp nhận lời mời kết bạn!" });
                }
                else
                    return Json(new { success = false, message = "Không thể chấp nhận lời mời!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in AcceptFriendRequest: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        // API: Từ chối lời mời kết bạn
        [HttpPost]
        public IActionResult DeclineFriendRequest([FromBody] FriendshipActionModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                var success = _friendshipService.DeclineFriendRequest(model.FriendshipId, userId);

                if (success)
                    return Json(new { success = true, message = "Đã từ chối lời mời kết bạn!" });
                else
                    return Json(new { success = false, message = "Không thể từ chối lời mời!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in DeclineFriendRequest: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        // API: Hủy lời mời đã gửi
        [HttpPost]
        public IActionResult CancelFriendRequest([FromBody] FriendshipActionModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                var success = _friendshipService.CancelFriendRequest(model.FriendshipId, userId);

                if (success)
                    return Json(new { success = true, message = "Đã hủy lời mời kết bạn!" });
                else
                    return Json(new { success = false, message = "Không thể hủy lời mời!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CancelFriendRequest: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        // API: Hủy kết bạn
        [HttpPost]
        public IActionResult Unfriend([FromBody] FriendRequestModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                var success = _friendshipService.Unfriend(userId, model.UserId);

                if (success)
                    return Json(new { success = true, message = "Đã hủy kết bạn!" });
                else
                    return Json(new { success = false, message = "Không thể hủy kết bạn!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Unfriend: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        // Model cho request
        public class FriendRequestModel
        {
            public int UserId { get; set; }
        }

        public class FriendshipActionModel
        {
            public int FriendshipId { get; set; }
        }

        public IActionResult _StudentLayout()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return RedirectToAction("Login", "Account");

                var messages = _messageService.GetRecentMessagesForDropdown(userId);

                ViewBag.TotalUnread = _messageService.GetTotalUnreadCount(userId);

                return View(messages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in _StudentLayout: {ex.Message}");
                return RedirectToAction("Login", "Account");
            }
        }

        // ==================== COMMUNITY ACTIONS ====================

        public async Task<IActionResult> Community()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                var posts = await _communityService.GetAllPostsAsync();
                var userPostCount = posts.Count(p => p.UserId == userId);

                ViewBag.CurrentUserId = userId;
                ViewBag.CurrentUserName = user?.FullName ?? "User";
                ViewBag.UserPostCount = userPostCount;
                ViewBag.Posts = posts;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Community: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang cộng đồng!";
                return RedirectToAction("Dashboard");
            }
        }



        // ✅ FIX: Add enriched data with reactions
        [HttpGet]
        public async Task<IActionResult> GetAllPosts()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var posts = await _communityService.GetAllPostsAsync();

                var postsData = new List<object>();
                foreach (var p in posts)
                {
                    var likeCount = await _communityService.GetPostLikeCountAsync(p.Id);
                    var isLiked = await _communityService.IsPostLikedAsync(p.Id, userId);
                    var reactionStats = await _communityService.GetPostReactionStatsAsync(p.Id);
                    var userReaction = await _communityService.GetUserPostReactionAsync(p.Id, userId);

                    postsData.Add(new
                    {
                        id = p.Id,
                        userId = p.UserId,
                        content = p.Content,
                        category = p.Category,
                        authorName = $"Người Ẩn Danh #{p.UserId}",
                        authorAvatar = $"https://ui-avatars.com/api/?name=Anon+{p.UserId}&background=667eea&color=fff",
                        createdAt = p.CreatedAt.ToString("H:mm, dd/MM"),
                        commentCount = p.Comments?.Where(c => !c.IsDeleted).Count() ?? 0,
                        likeCount,
                        isLiked,
                        reactionStats,
                        userReaction
                    });
                }

                return Json(postsData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetAllPosts: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }



        // ✅ FIX: Add enriched comment data
        [HttpGet]
        public async Task<IActionResult> GetPostComments(int postId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var comments = await _communityService.GetPostCommentsAsync(postId);

                var enriched = new List<object>();
                foreach (dynamic c in comments)
                {
                    int cid = (int)c.Id;
                    var likeCount = await _communityService.GetCommentLikeCountAsync(cid);
                    var isLiked = await _communityService.IsCommentLikedAsync(cid, userId);
                    var reactionStats = await _communityService.GetCommentReactionStatsAsync(cid);
                    var userReaction = await _communityService.GetUserCommentReactionAsync(cid, userId);

                    enriched.Add(new
                    {
                        c.Id,
                        c.PostId,
                        c.Content,
                        c.IsAnonymous,
                        authorName = c.authorName,
                        authorAvatar = c.authorAvatar,
                        createdAt = c.createdAt,
                        likeCount,
                        isLiked,
                        reactionStats,
                        userReaction
                    });
                }

                return Json(enriched);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetPostComments: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostComment([FromBody] CommentCreateModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            if (model == null || string.IsNullOrWhiteSpace(model.Content))
                return BadRequest(new { success = false, message = "Nội dung trống." });

            try
            {
                var comment = new CommunityComment
                {
                    PostId = model.PostId,
                    UserId = userId,
                    Content = model.Content.Trim(),
                    IsAnonymous = true,
                    CreatedAt = DateTime.Now
                };

                var created = await _communityService.AddCommentAsync(comment);

                var payload = new
                {
                    id = created.Id,
                    postId = created.PostId,
                    content = created.Content,
                    isAnonymous = created.IsAnonymous,
                    authorName = $"Người Ẩn Danh #{created.UserId}",
                    authorAvatar = $"https://ui-avatars.com/api/?name=Anon+{created.UserId}&background=999&color=fff",
                    createdAt = created.CreatedAt.ToString("HH:mm, dd/MM")
                };

                await _hubContext.Clients.Group($"post-{created.PostId}").SendAsync("CommentAdded", payload);

                return Json(new { success = true, comment = payload });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CommunityPost model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var post = new CommunityPost
                {
                    UserId = userId,
                    Title = model.Title ?? "",
                    Content = model.Content,
                    Category = model.Category,
                    CreatedAt = DateTime.Now
                };

                var createdPost = await _communityService.CreatePostAsync(post);

                var postPayload = new
                {
                    id = createdPost.Id,
                    userId = createdPost.UserId,
                    content = createdPost.Content,
                    category = createdPost.Category,
                    authorName = $"Người Ẩn Danh #{createdPost.UserId}",
                    authorAvatar = $"https://ui-avatars.com/api/?name=Anon+{createdPost.UserId}&background=667eea&color=fff",
                    createdAt = createdPost.CreatedAt.ToString("H:mm, dd/MM"),
                    commentCount = 0,
                    likeCount = 0
                };

                await _hubContext.Clients.All.SendAsync("PostCreated", postPayload);

                return Json(new { success = true, postId = createdPost.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ✅ ADD: Missing reaction endpoints
        [HttpPost]
        public async Task<IActionResult> ReactToPost([FromBody] ReactionRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var userReaction = await _communityService.TogglePostReactionAsync(request.PostId, userId, request.ReactionType);
                var reactionStats = await _communityService.GetPostReactionStatsAsync(request.PostId);

                var payload = new
                {
                    postId = request.PostId,
                    userId,
                    userReaction,
                    reactionStats
                };

                await _hubContext.Clients.Group($"post-{request.PostId}").SendAsync("PostReactionUpdated", payload);

                return Json(new { success = true, data = payload });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReactToComment([FromBody] ReactionRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var userReaction = await _communityService.ToggleCommentReactionAsync(request.CommentId, userId, request.ReactionType);
                var reactionStats = await _communityService.GetCommentReactionStatsAsync(request.CommentId);

                var comment = await _communityService.GetCommentByIdAsync(request.CommentId);
                var postId = comment?.PostId ?? 0;

                var payload = new
                {
                    commentId = request.CommentId,
                    postId,
                    userId,
                    userReaction,
                    reactionStats
                };

                if (postId > 0)
                {
                    await _hubContext.Clients.Group($"post-{postId}").SendAsync("CommentReactionUpdated", payload);
                }

                return Json(new { success = true, data = payload });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ ADD: Toggle like endpoints (backward compatibility)
        [HttpPost]
        public async Task<IActionResult> TogglePostLike([FromBody] int postId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var isLiked = await _communityService.TogglePostLikeAsync(postId, userId);
                var likeCount = await _communityService.GetPostLikeCountAsync(postId);

                await _hubContext.Clients.Group($"post-{postId}").SendAsync("PostLikeUpdated", new
                {
                    postId,
                    userId,
                    isLiked,
                    likeCount
                });

                return Json(new { success = true, postId, isLiked, likeCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleCommentLike([FromBody] int commentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var isLiked = await _communityService.ToggleCommentLikeAsync(commentId, userId);
                var likeCount = await _communityService.GetCommentLikeCountAsync(commentId);

                var comment = await _communityService.GetCommentByIdAsync(commentId);
                var postId = comment?.PostId ?? 0;

                if (postId > 0)
                {
                    await _hubContext.Clients.Group($"post-{postId}").SendAsync("CommentLikeUpdated", new
                    {
                        commentId,
                        postId,
                        userId,
                        isLiked,
                        likeCount
                    });
                }

                return Json(new { success = true, commentId, isLiked, likeCount, postId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPostReactions(int postId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var reactionStats = await _communityService.GetPostReactionStatsAsync(postId);
                var userReaction = await _communityService.GetUserPostReactionAsync(postId, userId);

                return Json(new
                {
                    success = true,
                    reactionStats,
                    userReaction
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCommentReactions(int commentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var reactionStats = await _communityService.GetCommentReactionStatsAsync(commentId);
                var userReaction = await _communityService.GetUserCommentReactionAsync(commentId, userId);

                return Json(new
                {
                    success = true,
                    reactionStats,
                    userReaction
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPostById(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var post = await _communityService.GetPostByIdAsync(id);

                if (post == null)
                    return NotFound(new { success = false, message = "Không tìm thấy bài viết" });

                return Json(new
                {
                    success = true,
                    id = post.Id,
                    content = post.Content,
                    category = post.Category,
                    userId = post.UserId,
                    isOwner = post.UserId == userId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetPostById: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ FIX: Update signature to match service
        [HttpPost]
        public async Task<IActionResult> UpdatePost([FromBody] UpdatePostModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var post = await _communityService.GetPostByIdAsync(model.PostId);

                if (post == null)
                    return NotFound(new { success = false, message = "Post not found" });

                if (post.UserId != userId)
                    return Unauthorized();

                var success = await _communityService.UpdatePostAsync(model.PostId, userId, model.Content, model.Category);

                if (!success)
                    return BadRequest(new { success = false, message = "Không thể cập nhật bài viết" });

                await _hubContext.Clients.All.SendAsync("PostUpdated", new
                {
                    postId = model.PostId,
                    content = model.Content,
                    category = model.Category
                });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ✅ FIX: Update signature to match service
        [HttpPost]
        public async Task<IActionResult> DeletePost([FromBody] DeletePostModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var post = await _communityService.GetPostByIdAsync(model.PostId);

                if (post == null)
                    return NotFound(new { success = false, message = "Post not found" });

                if (post.UserId != userId)
                    return Unauthorized();

                var deleted = await _communityService.DeletePostAsync(model.PostId, userId);

                if (!deleted)
                    return BadRequest(new { success = false, message = "Không thể xóa bài viết" });

                await _hubContext.Clients.All.SendAsync("PostDeleted", new
                {
                    postId = model.PostId
                });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Replace the existing CreateProblem POST action body with this improved version that creates a group when requested.
        [HttpPost]
        public IActionResult CreateProblem(CreateProblemViewModel model, decimal? CustomPrice)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                if (ModelState.IsValid)
                {
                    // TÍNH GIÁ THEO CẤP HỌC
                    decimal price;

                    if (CustomPrice.HasValue && CustomPrice.Value >= 10000)
                    {
                        // Sử dụng giá tùy chỉnh
                        price = CustomPrice.Value;
                    }
                    else
                    {
                        // Sử dụng giá mặc định theo cấp học
                        price = model.Difficulty switch
                        {
                            DifficultyLevel.TieuHoc => 30000,
                            DifficultyLevel.THCS => 50000,
                            DifficultyLevel.THPT => 70000,
                            DifficultyLevel.DaiHoc => 100000,
                            _ => 50000
                        };
                    }

                    string? attachmentUrl = null;

                    if (model.AttachmentFile != null && model.AttachmentFile.Length > 0)
                    {
                        var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                        var allowedMimeTypes = new[]
                        {
                    "application/pdf",
                    "application/msword",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };

                        var ext = Path.GetExtension(model.AttachmentFile.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(ext))
                        {
                            ModelState.AddModelError("AttachmentFile", "Chỉ cho phép file PDF, DOC, DOCX.");
                            return View(model);
                        }

                        if (!allowedMimeTypes.Contains(model.AttachmentFile.ContentType))
                        {
                            ModelState.AddModelError("AttachmentFile", "Định dạng file không hợp lệ.");
                            return View(model);
                        }

                        if (model.AttachmentFile.Length > 10 * 1024 * 1024)
                        {
                            ModelState.AddModelError("AttachmentFile", "File tối đa 10MB.");
                            return View(model);
                        }

                        // Upload file
                        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files");
                        Directory.CreateDirectory(uploadDir);

                        var fileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadDir, fileName);

                        using var stream = new FileStream(filePath, FileMode.Create);
                        model.AttachmentFile.CopyTo(stream);

                        attachmentUrl = "/files/" + fileName;
                    }

                    // Create Problem
                    var problem = new Problem
                    {
                        StudentId = userId,
                        Title = model.Title,
                        Description = model.Description,
                        Type = model.Type,
                        Difficulty = model.Difficulty,  // Giờ là cấp học
                        ImageUrl = model.ImageFile != null ? $"/images/{model.ImageFile.FileName}" : "/images/default.jpg",
                        AttachmentFile = attachmentUrl,
                        Deadline = model.Deadline,
                        Price = price,
                        CreatedDate = DateTime.Now,
                        Status = ProblemStatus.WaitingForTutor
                    };

                    if (_problemService.CreateProblem(problem))
                    {
                        // If the user created a group problem, create the group using the group service
                        if (model.IsGroupMode)
                        {
                            // Use provided group name or fallback
                            var groupName = string.IsNullOrWhiteSpace(model.GroupName) ? $"Nhóm_{userId}_{DateTime.Now:yyyyMMddHHmmss}" : model.GroupName;

                            var group = _problemGroupService.CreateProblemGroup(problem.Id, userId, groupName, price);
                            if (group != null)
                            {
                                // Attempt to create initial group payment for the owner if member record exists
                                try
                                {
                                    var members = _problemGroupService.GetGroupMembers(group.Id);
                                    var ownerMember = members.FirstOrDefault(m => m.UserId == userId);
                                    if (ownerMember != null)
                                    {
                                        // CreateGroupPayment signature used elsewhere: (groupId, memberId, userId, amount)
                                        _paymentService.CreateGroupPayment(group.Id, ownerMember.Id, userId, group.PricePerMember);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"⚠️ Warning creating owner group payment: {ex.Message}");
                                }

                                // Notification - created group & problem
                                _notificationService.NotifyProblemCreated(userId, problem.Id, problem.Title);

                                TempData["Success"] = "Tạo nhóm và đăng bài toán thành công!";
                                return RedirectToAction("MyGroups");
                            }
                            else
                            {
                                // If group creation failed, rollback problem maybe -- for now inform user
                                TempData["Error"] = "Không thể tạo nhóm sau khi đăng bài toán. Vui lòng thử lại.";
                                return RedirectToAction("Dashboard");
                            }
                        }
                        else
                        {
                            // Individual problem: create payment
                            var payment = new Payment
                            {
                                StudentId = userId,
                                ProblemId = problem.Id,
                                Amount = price
                            };
                            _paymentService.CreatePayment(payment);

                            // Notification
                            _notificationService.NotifyProblemCreated(userId, problem.Id, problem.Title);

                            TempData["Success"] = "Đăng bài toán thành công!";
                            return RedirectToAction("Dashboard");
                        }
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CreateProblem POST: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tạo bài toán!";
                return View(model);
            }
        }

        /// <summary>
        /// Hiển thị chi tiết nhóm và danh sách thành viên
        /// </summary>
        public IActionResult GroupDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var group = _problemGroupService.GetGroupById(id);
                if (group == null)
                {
                    TempData["Error"] = "Không tìm thấy nhóm!";
                    return RedirectToAction("Dashboard");
                }

                var members = _problemGroupService.GetGroupMembers(id);
                var isOwner = group.CreatedByUserId == userId;
                var isMember = members.Any(m => m.UserId == userId);

                // Get friends who are not yet members or invited
                var friends = _friendshipService.GetFriends(userId);
                var memberUserIds = members.Select(m => m.UserId).ToList();
                var invites = _problemGroupService.GetGroupInvites(id);
                var invitedUserIds = invites.Where(i => i.Status == GroupInviteStatus.Pending)
                                            .Select(i => i.InvitedUserId).ToList();

                var availableFriends = friends.Where(f =>
                    !memberUserIds.Contains(f.Id) &&
                    !invitedUserIds.Contains(f.Id)
                ).ToList();

                var model = new GroupDetailsViewModel
                {
                    Group = group,
                    Problem = group.Problem,
                    Members = members,
                    PendingInvites = invites.Where(i => i.Status == GroupInviteStatus.Pending).ToList(),
                    AvailableFriendsToInvite = availableFriends,
                    IsOwner = isOwner,
                    IsMember = isMember,
                    PricePerMember = group.PricePerMember
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GroupDetails: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải thông tin nhóm!";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// Mời bạn bè vào nhóm
        /// </summary>
        [HttpPost]
        public IActionResult InviteFriend(int groupId, int friendId)
        {
            try
            {
                Console.WriteLine($"📥 InviteFriend called: groupId={groupId}, friendId={friendId}");

                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    Console.WriteLine("❌ User not logged in");
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });
                }

                Console.WriteLine($"✅ Current user: {userId}");

                var group = _problemGroupService.GetGroupById(groupId);
                if (group == null)
                {
                    Console.WriteLine($"❌ Group not found: {groupId}");
                    return Json(new { success = false, message = "Không tìm thấy nhóm!" });
                }

                Console.WriteLine($"✅ Group found: {group.GroupName}");

                // Check if user is member of the group
                var members = _problemGroupService.GetGroupMembers(groupId);
                Console.WriteLine($"📋 Group has {members.Count} members");

                if (!members.Any(m => m.UserId == userId))
                {
                    Console.WriteLine($"❌ User {userId} is not a member");
                    return Json(new { success = false, message = "Bạn không phải thành viên của nhóm!" });
                }

                Console.WriteLine($"✅ User is a member");

                // Check if they are friends
                if (!_friendshipService.AreFriends(userId, friendId))
                {
                    Console.WriteLine($"❌ Not friends: {userId} and {friendId}");
                    return Json(new { success = false, message = "Chỉ có thể mời bạn bè!" });
                }

                Console.WriteLine($"✅ They are friends");

                var invite = _problemGroupService.CreateInvite(groupId, friendId, userId);
                if (invite != null)
                {
                    Console.WriteLine($"✅ Invite created successfully");
                    return Json(new { success = true, message = "Đã gửi lời mời thành công!" });
                }
                else
                {
                    Console.WriteLine($"❌ Failed to create invite");
                    return Json(new { success = false, message = "Không thể gửi lời mời. Có thể đã mời trước đó!" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in InviteFriend: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Đã xảy ra lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Chấp nhận lời mời tham gia nhóm
        /// </summary>
        [HttpPost]
        public IActionResult AcceptGroupInvite(int inviteId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                if (_problemGroupService.AcceptInvite(inviteId, userId))
                {
                    return Json(new { success = true, message = "Đã tham gia nhóm thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể chấp nhận lời mời!" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in AcceptGroupInvite: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        /// <summary>
        /// Từ chối lời mời tham gia nhóm
        /// </summary>
        [HttpPost]
        public IActionResult RejectGroupInvite(int inviteId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                if (_problemGroupService.RejectInvite(inviteId, userId))
                {
                    return Json(new { success = true, message = "Đã từ chối lời mời!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể từ chối lời mời!" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in RejectGroupInvite: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        /// <summary>
        /// Rời khỏi nhóm (không phải owner)
        /// </summary>
        [HttpPost]
        public IActionResult LeaveGroup(int groupId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                var group = _problemGroupService.GetGroupById(groupId);
                if (group == null)
                    return Json(new { success = false, message = "Không tìm thấy nhóm!" });

                // ✅ Kiểm tra: Không cho phép owner rời nhóm
                if (group.CreatedByUserId == userId)
                    return Json(new { success = false, message = "Người tạo nhóm không thể rời khỏi nhóm!" });

                // ✅ KIỂM TRA MỚI: Không cho phép rời nhóm nếu tutor đã nhận bài
                var problem = _problemService.GetProblemById(group.ProblemId);
                if (problem != null)
                {
                    // Kiểm tra nếu đã có tutor nhận bài
                    if (problem.AssignedTutorId.HasValue)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Không thể rời nhóm khi Mentor đã nhận bài! Bạn cần hoàn thành thanh toán phần của mình."
                        });
                    }

                    // Kiểm tra nếu đã có lời giải (double check)
                    var solution = _solutionService.GetSolutionByProblemId(problem.Id);
                    if (solution != null)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Không thể rời nhóm khi Mentor đã gửi lời giải! Bạn đã xem được kết quả và cần hoàn thành thanh toán."
                        });
                    }

                    // Kiểm tra nếu bài toán đang được giải hoặc đã hoàn thành
                    if (problem.Status == ProblemStatus.InProgress || problem.Status == ProblemStatus.Solved)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Không thể rời nhóm khi bài toán đang được giải hoặc đã hoàn thành!"
                        });
                    }
                }

                // ✅ KIỂM TRA THÊM: Không cho phép rời nếu đã thanh toán
                var members = _problemGroupService.GetGroupMembers(groupId);
                var currentMember = members.FirstOrDefault(m => m.UserId == userId);

                if (currentMember != null && currentMember.PaymentStatus == GroupPaymentStatus.Paid)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bạn đã thanh toán cho nhóm này. Không thể rời nhóm sau khi đã thanh toán!"
                    });
                }

                // ✅ Cho phép rời nhóm nếu chưa có tutor và chưa thanh toán
                if (_problemGroupService.RemoveMember(groupId, userId))
                {
                    return Json(new { success = true, message = "Đã rời khỏi nhóm thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể rời khỏi nhóm!" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in LeaveGroup: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        /// <summary>
        /// Xóa thành viên khỏi nhóm (chỉ owner)
        /// </summary>
        [HttpPost]
        public IActionResult RemoveMember(int groupId, int memberId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                var group = _problemGroupService.GetGroupById(groupId);
                if (group == null)
                    return Json(new { success = false, message = "Không tìm thấy nhóm!" });

                if (group.CreatedByUserId != userId)
                    return Json(new { success = false, message = "Chỉ người tạo nhóm mới có quyền xóa thành viên!" });

                if (_problemGroupService.RemoveMember(groupId, memberId))
                {
                    return Json(new { success = true, message = "Đã xóa thành viên khỏi nhóm!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa thành viên!" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in RemoveMember: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        /// <summary>
        /// Danh sách tất cả nhóm của user
        /// </summary>
        public IActionResult MyGroups()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var groups = _problemGroupService.GetGroupsByUserId(userId);
                return View(groups);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in MyGroups: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách nhóm!";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// Danh sách lời mời nhóm đang chờ
        /// </summary>
        public IActionResult GroupInvites()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var invites = _problemGroupService.GetPendingInvitesForUser(userId);
                return View(invites);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GroupInvites: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải lời mời nhóm!";
                return RedirectToAction("Dashboard");
            }
        }

        public class UpdatePostModel
        {
            public int PostId { get; set; }
            public string Content { get; set; } = "";
            public string? Category { get; set; }
        }

        public class DeletePostModel
        {
            public int PostId { get; set; }
        }


        /// <summary>
        /// Xử lý thanh toán cho thành viên nhóm
        /// </summary>
        [HttpPost]
        public IActionResult ProcessGroupPayment(int memberId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var member = _db.ProblemGroupMembers
                    .Include(m => m.Group)
                    .FirstOrDefault(m => m.Id == memberId && m.UserId == userId);

                if (member == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin thành viên!";
                    return RedirectToAction("MyGroups");
                }

                var group = _problemGroupService.GetGroupById(member.GroupId);
                if (group == null)
                {
                    TempData["Error"] = "Không tìm thấy nhóm!";
                    return RedirectToAction("MyGroups");
                }

                // Tạo hoặc lấy GroupPayment hiện có
                var groupPayment = _paymentService.GetGroupPaymentByMemberId(memberId);
                if (groupPayment == null)
                {
                    groupPayment = _paymentService.CreateGroupPayment(
                        group.Id,
                        memberId,
                        userId,
                        group.PricePerMember
                    );

                    if (groupPayment == null)
                    {
                        TempData["Error"] = "Không thể tạo thanh toán!";
                        return RedirectToAction("GroupDetails", new { id = group.Id });
                    }
                }

                return RedirectToAction("GroupPaymentCheckout", new { groupPaymentId = groupPayment.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi ProcessGroupPayment: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("MyGroups");
            }
        }

        /// <summary>
        /// Trang thanh toán nhóm
        /// </summary>
        [HttpGet]
        public IActionResult GroupPaymentCheckout(int groupPaymentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var groupPayment = _paymentService.GetGroupPaymentById(groupPaymentId);
                if (groupPayment == null || groupPayment.UserId != userId)
                {
                    TempData["Error"] = "Không tìm thấy giao dịch!";
                    return RedirectToAction("MyGroups");
                }

                if (groupPayment.Status != PaymentStatus.Pending)
                {
                    TempData["Warning"] = "Giao dịch này đã được xử lý!";
                    return RedirectToAction("GroupDetails", new { id = groupPayment.GroupId });
                }

                var successUrl = Url.Action("GroupPaymentResult", "Student",
                    new { groupPaymentId, status = "success" }, Request.Scheme);
                var errorUrl = Url.Action("GroupPaymentResult", "Student",
                    new { groupPaymentId, status = "error" }, Request.Scheme);
                var cancelUrl = Url.Action("GroupPaymentResult", "Student",
                    new { groupPaymentId, status = "cancel" }, Request.Scheme);

                // Tạo Payment tạm để tương thích với SePay gateway
                var tempPayment = new Payment
                {
                    Id = groupPayment.Id,
                    StudentId = groupPayment.UserId,
                    ProblemId = groupPayment.GroupId,
                    Amount = groupPayment.Amount
                };

                var checkout = _sePayGateway.BuildCheckout(tempPayment, successUrl!, errorUrl!, cancelUrl!);
                ViewBag.GroupPayment = groupPayment;
                ViewBag.Group = _problemGroupService.GetGroupById(groupPayment.GroupId);

                return View(checkout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi GroupPaymentCheckout: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("MyGroups");
            }
        }

        /// <summary>
        /// Xử lý kết quả thanh toán nhóm
        /// </summary>
        [HttpGet]
        public IActionResult GroupPaymentResult(int groupPaymentId, string status)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var groupPayment = _paymentService.GetGroupPaymentById(groupPaymentId);
                if (groupPayment == null || groupPayment.UserId != userId)
                {
                    TempData["Error"] = "Không tìm thấy giao dịch!";
                    return RedirectToAction("MyGroups");
                }

                if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Success"] = "Thanh toán đang được xác nhận. Vui lòng đợi cập nhật.";
                }
                else if (string.Equals(status, "cancel", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Warning"] = "Bạn đã hủy thanh toán.";
                }
                else
                {
                    TempData["Error"] = "Thanh toán không thành công.";
                }

                return RedirectToAction("GroupDetails", new { id = groupPayment.GroupId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi GroupPaymentResult: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("MyGroups");
            }
        }
        // Premium
        [HttpGet]
        public IActionResult Premium()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return RedirectToAction("Login", "Account");

                var user = _premiumService.GetPremiumUser(userId.Value);

                ViewBag.IsPremium = user?.IsPremium ?? false;
                ViewBag.PremiumExpiredAt = user?.PremiumExpiredAt;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public IActionResult UpgradePremium()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            if (_premiumService.IsPremium(userId.Value))
                return BadRequest("Already premium");

            _premiumService.UpgradeToPremium(userId.Value);
            return Ok();
        }


        // Add these methods inside the StudentController class (near other actions)

        [HttpGet]
        public IActionResult EditProblem(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var problem = _problemService.GetProblemById(id);
                if (problem == null)
                {
                    TempData["Error"] = "Không tìm thấy bài toán!";
                    return RedirectToAction("MyProblems");
                }

                if (problem.StudentId != userId)
                {
                    TempData["Error"] = "Bạn không có quyền sửa bài toán này!";
                    return RedirectToAction("MyProblems");
                }

                if (problem.Status != ProblemStatus.WaitingForTutor)
                {
                    TempData["Error"] = "Không thể sửa bài toán đã có Mentor nhận!";
                    return RedirectToAction("ProblemDetails", new { id });
                }

                var model = new CreateProblemViewModel
                {
                    Title = problem.Title,
                    Description = problem.Description,
                    Type = problem.Type,
                    Difficulty = problem.Difficulty,
                    Deadline = problem.Deadline
                };

                ViewBag.ProblemId = problem.Id;
                ViewBag.CurrentImageUrl = problem.ImageUrl;
                ViewBag.CurrentAttachmentUrl = problem.AttachmentFile;
                ViewBag.CurrentPrice = problem.Price;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in EditProblem GET: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("MyProblems");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProblem(int id, CreateProblemViewModel model, decimal? CustomPrice)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var problem = _problemService.GetProblemById(id);
                if (problem == null || problem.StudentId != userId || problem.Status != ProblemStatus.WaitingForTutor)
                {
                    TempData["Error"] = "Không thể sửa bài toán này!";
                    return RedirectToAction("MyProblems");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.ProblemId = id;
                    ViewBag.CurrentImageUrl = problem.ImageUrl;
                    ViewBag.CurrentAttachmentUrl = problem.AttachmentFile;
                    ViewBag.CurrentPrice = problem.Price;
                    return View(model);
                }

                // ✅ TÍNH GIÁ THEO CẤP HỌC (GIỐNG CREATEPROBLEM)
                decimal price;

                if (CustomPrice.HasValue && CustomPrice.Value >= 10000)
                {
                    // Sử dụng giá tùy chỉnh
                    price = CustomPrice.Value;
                }
                else
                {
                    // Sử dụng giá mặc định theo cấp học
                    price = model.Difficulty switch
                    {
                        DifficultyLevel.TieuHoc => 30000,
                        DifficultyLevel.THCS => 50000,
                        DifficultyLevel.THPT => 70000,
                        DifficultyLevel.DaiHoc => 100000,
                        _ => 50000
                    };
                }

                // ✅ UPDATE PROBLEM FIELDS
                problem.Title = model.Title;
                problem.Description = model.Description;
                problem.Type = model.Type;
                problem.Difficulty = model.Difficulty;
                problem.Deadline = model.Deadline;
                problem.Price = price;

                // ✅ HANDLE IMAGE UPLOAD
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    Directory.CreateDirectory(uploadDir);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
                    var filePath = Path.Combine(uploadDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.ImageFile.CopyTo(stream);
                    }

                    problem.ImageUrl = "/images/" + fileName;
                }

                // ✅ HANDLE ATTACHMENT UPLOAD
                if (model.AttachmentFile != null && model.AttachmentFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                    var ext = Path.GetExtension(model.AttachmentFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("AttachmentFile", "Chỉ cho phép file PDF, DOC, DOCX.");
                        ViewBag.ProblemId = id;
                        ViewBag.CurrentImageUrl = problem.ImageUrl;
                        ViewBag.CurrentAttachmentUrl = problem.AttachmentFile;
                        ViewBag.CurrentPrice = problem.Price;
                        return View(model);
                    }

                    if (model.AttachmentFile.Length > 10 * 1024 * 1024)
                    {
                        ModelState.AddModelError("AttachmentFile", "File tối đa 10MB.");
                        ViewBag.ProblemId = id;
                        ViewBag.CurrentImageUrl = problem.ImageUrl;
                        ViewBag.CurrentAttachmentUrl = problem.AttachmentFile;
                        ViewBag.CurrentPrice = problem.Price;
                        return View(model);
                    }

                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files");
                    Directory.CreateDirectory(uploadDir);

                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.AttachmentFile.CopyTo(stream);
                    }

                    problem.AttachmentFile = "/files/" + fileName;
                }

                // ✅ UPDATE DATABASE
                if (_problemService.UpdateProblem(problem))
                {
                    // ✅ UPDATE PAYMENT AMOUNT
                    var payment = _paymentService.GetPaymentByProblemId(problem.Id);
                    if (payment != null && payment.Status == PaymentStatus.Pending)
                    {
                        payment.Amount = price;
                        _db.SaveChanges();
                    }

                    TempData["Success"] = "Cập nhật bài toán thành công!";
                    return RedirectToAction("ProblemDetails", new { id });
                }

                TempData["Error"] = "Không thể cập nhật bài toán!";
                ViewBag.ProblemId = id;
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in EditProblem POST: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật bài toán!";
                return RedirectToAction("MyProblems");
            }
        }

        [HttpPost]
        public IActionResult DeleteProblem(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                var problem = _problemService.GetProblemById(id);
                if (problem == null)
                    return Json(new { success = false, message = "Không tìm thấy bài toán!" });

                if (problem.StudentId != userId)
                    return Json(new { success = false, message = "Bạn không có quyền xóa bài toán này!" });

                if (problem.Status != ProblemStatus.WaitingForTutor)
                    return Json(new { success = false, message = "Không thể xóa bài toán đã có Mentor nhận!" });

                var payment = _paymentService.GetPaymentByProblemId(id);
                if (payment != null)
                {
                    _db.Payments.Remove(payment);
                }

                // Delete problem via service (service calls SaveChanges)
                if (_problemService.DeleteProblem(id))
                {
                    // ensure any pending removals are saved (in case service didn't)
                    _db.SaveChanges();
                    return Json(new { success = true, message = "Xóa bài toán thành công!" });
                }

                return Json(new { success = false, message = "Không thể xóa bài toán!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in DeleteProblem: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa bài toán!" });
            }
        }

        public IActionResult MyClassSchedules()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin người dùng!";
                    return RedirectToAction("Dashboard");
                }

                // ✅ KIỂM TRA QUYỀN TRUY CẬP - CHỈ HỌC SINH ĐẠI HỌC
                if (user.Role != UserRole.Student)
                {
                    TempData["Error"] = "Chỉ học sinh mới có thể xem trang này!";
                    return RedirectToAction("Dashboard");
                }

                if (!user.Level.HasValue || user.Level.Value != EducationLevel.DaiHoc)
                {
                    TempData["Error"] = "Tính năng này chỉ dành cho học sinh Đại học!";
                    return RedirectToAction("Dashboard");
                }

                // ✅ LẤY DANH SÁCH LỚP HỌC MÀ HỌC SINH THAM GIA
                var myClassIds = _db.ClassStudents
                    .Where(cs => cs.StudentId == userId)
                    .Select(cs => cs.ClassId)
                    .ToList();

                // ✅ LẤY TẤT CẢ LỊCH TRAO ĐỔI CỦA CÁC LỚP HỌC ĐÓ
                var schedules = _db.ClassSchedules
                    .Where(s => myClassIds.Contains(s.ClassId))
                    .Include(s => s.Class)
                        .ThenInclude(c => c!.School)
                    .Include(s => s.Class)
                        .ThenInclude(c => c!.Tutor)
                    .OrderByDescending(s => s.ScheduleDate)
                    .ThenBy(s => s.StartTime)
                    .Select(s => new ScheduleListViewModel
                    {
                        Id = s.Id,
                        Title = s.Title,
                        ScheduleDate = s.ScheduleDate,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        MeetingType = s.MeetingType,
                        Status = s.Status,
                        ClassName = s.Class!.ClassName,
                        Subject = s.Class.Subject,
                        TotalStudents = _db.ClassStudents.Count(cs => cs.ClassId == s.ClassId)
                    })
                    .ToList();

                // ✅ THỐNG KÊ
                ViewBag.TotalSchedules = schedules.Count;
                ViewBag.UpcomingSchedules = schedules.Count(s => s.Status == ScheduleStatus.Upcoming);
                ViewBag.CompletedSchedules = schedules.Count(s => s.Status == ScheduleStatus.Completed);

                return View(schedules);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in MyClassSchedules: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải lịch trao đổi!";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult MyClassSchool()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin người dùng!";
                    return RedirectToAction("Dashboard");
                }

                // ✅ KIỂM TRA QUYỀN TRUY CẬP - CHỈ HỌC SINH ĐẠI HỌC
                if (user.Role != UserRole.Student)
                {
                    TempData["Error"] = "Chỉ học sinh mới có thể xem trang này!";
                    return RedirectToAction("Dashboard");
                }

                if (!user.Level.HasValue || user.Level.Value != EducationLevel.DaiHoc)
                {
                    TempData["Error"] = "Tính năng này chỉ dành cho học sinh Đại học!";
                    return RedirectToAction("Dashboard");
                }

                // ✅ LẤY DANH SÁCH LỚP HỌC
                var myClasses = _db.ClassStudents
                    .Where(cs => cs.StudentId == userId)
                    .Include(cs => cs.Class)
                        .ThenInclude(c => c.School)
                    .Include(cs => cs.Class)
                        .ThenInclude(c => c.Tutor)
                    .Include(cs => cs.Class)
                        .ThenInclude(c => c.Students)
                    .Select(cs => cs.Class!)
                    .Where(c => c != null)
                    .OrderByDescending(c => c.CreatedDate)
                    .ToList();

                var classViewModels = myClasses
                    .Where(c => c != null)
                    .Select(c => new ClassSchoolViewModel
                    {
                        Id = c.Id,
                        ClassName = c.ClassName ?? "Không có tên",
                        Subject = c.Subject ?? "Chưa xác định",
                        Description = c.Description ?? "",
                        SchoolName = c.School?.FullName ?? "Chưa cập nhật",
                        TutorName = c.Tutor?.FullName ?? "Chưa có",
                        TutorEmail = c.Tutor?.Email ?? "",
                        StudentCount = c.Students?.Count ?? 0,
                        StartDate = c.StartDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định",
                        EndDate = c.EndDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định",
                        Status = c.Status,
                        CreatedDate = c.CreatedDate.ToString("dd/MM/yyyy HH:mm")
                    }).ToList();

                ViewBag.TotalClasses = classViewModels.Count;
                ViewBag.ActiveClasses = classViewModels.Count(c => c.Status == ClassStatus.Active);

                return View(classViewModels);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in MyClassSchool: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách lớp học!";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult ClassSchoolDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                if (user == null || user.Role != UserRole.Student ||
                    !user.Level.HasValue || user.Level.Value != EducationLevel.DaiHoc)
                {
                    TempData["Error"] = "Bạn không có quyền truy cập!";
                    return RedirectToAction("Dashboard");
                }

                // Kiểm tra xem học sinh có thuộc lớp học của lịch này không
                var schedule = _db.ClassSchedules
                    .Include(s => s.Class)
                        .ThenInclude(c => c!.School)
                    .Include(s => s.Class)
                        .ThenInclude(c => c!.Tutor)
                    .Include(s => s.Creator)
                    .FirstOrDefault(s => s.Id == id);

                if (schedule == null)
                {
                    TempData["Error"] = "Không tìm thấy lịch trao đổi!";
                    return RedirectToAction("MyClassSchedules");
                }

                // Kiểm tra học sinh có trong lớp này không
                var isInClass = _db.ClassStudents
                    .Any(cs => cs.ClassId == schedule.ClassId && cs.StudentId == userId);

                if (!isInClass)
                {
                    TempData["Error"] = "Bạn không có quyền xem lịch trao đổi này!";
                    return RedirectToAction("MyClassSchedules");
                }

                // ✅ FIX: Lấy số học sinh từ bảng ClassStudents thay vì navigation property
                var studentCount = _db.ClassStudents
                    .Count(cs => cs.ClassId == schedule.ClassId);

                var model = new ScheduleDetailsViewModel
                {
                    Id = schedule.Id,
                    Title = schedule.Title,
                    ScheduleDate = schedule.ScheduleDate,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    MeetingType = schedule.MeetingType,
                    Location = schedule.Location,
                    Content = schedule.Content,
                    Notes = schedule.Notes,
                    Status = schedule.Status,
                    CreatedDate = schedule.CreatedDate,
                    CreatorName = schedule.Creator?.FullName ?? "Nhà trường",
                    ClassId = schedule.ClassId,
                    ClassName = schedule.Class!.ClassName,
                    Subject = schedule.Class.Subject,
                    TutorName = schedule.Class.Tutor?.FullName,
                    TutorEmail = schedule.Class.Tutor?.Email,
                    TotalStudents = studentCount // ✅ Sử dụng giá trị đếm trực tiếp
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ScheduleDetails: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("MyClassSchedules");
            }
        }

        // ✅ Action: Mark notification as read
        [HttpPost]
        public IActionResult MarkNotificationAsRead(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            _notificationService.MarkAsRead(id);
            return Ok();
        }

        // ✅ Action: View all notifications
        public IActionResult Notifications()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var notifications = _notificationService.GetRecentNotifications(userId, 50);
            return View(notifications);
        }

        // ✅ Action: Mark all as read
        [HttpPost]
        public IActionResult MarkAllNotificationsAsRead()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            _notificationService.MarkAllAsRead(userId);
            return RedirectToAction("Notifications");
        }


        // ✅ ACTION: Xem danh sách Tutors đăng ký
public IActionResult ViewTutorApplications(int problemId)
{
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var problem = _problemService.GetProblemById(problemId);
                if (problem == null || problem.StudentId != userId)
                {
                    TempData["Error"] = "Không tìm thấy bài toán!";
                    return RedirectToAction("MyProblems");
                }

                // ✅ LẤY APPLICATIONS VỚI ƯU TIÊN PREMIUM
                var applications = _tutorApplicationService.GetApplicationsForProblem(problemId, prioritizePremium: true);

                ViewBag.Problem = problem;
                return View(applications);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("MyProblems");
            }
        }

        // ✅ ACTION: Duyệt Tutor
        [HttpPost]
        public IActionResult ApproveApplication([FromBody] ApproveApplicationRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    Console.WriteLine("❌ User not logged in");
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });
                }

                Console.WriteLine($"📝 ApproveApplication called by user {userId} for application {request.ApplicationId}");

                var success = _tutorApplicationService.ApproveApplication(request.ApplicationId, userId);

                if (success)
                {
                    var application = _tutorApplicationService.GetApplicationById(request.ApplicationId);
                    if (application != null && application.Problem != null)
                    {
                        Console.WriteLine($"✅ Sending notification to tutor {application.TutorId}");
                        _notificationService.NotifyApplicationApproved(
                            application.TutorId,
                            application.ProblemId,
                            application.Problem.Title
                        );
                    }

                    return Json(new { success = true, message = "Đã duyệt Mentor thành công!" });
                }

                Console.WriteLine($"❌ ApproveApplication returned false");
                return Json(new { success = false, message = "Không thể duyệt! Có thể bài toán đã được nhận hoặc đơn đã xử lý." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ApproveApplication controller: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Đã xảy ra lỗi: {ex.Message}" });
            }
        }

        // ✅ THÊM REQUEST MODEL
        public class ApproveApplicationRequest
        {
            public int ApplicationId { get; set; }
        }

        // ✅ ACTION: Từ chối Tutor
        [HttpPost]
        public IActionResult RejectApplication(int applicationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                var success = _tutorApplicationService.RejectApplication(applicationId, userId);

                return Json(new { success = success, message = success ? "Đã từ chối!" : "Không thể từ chối!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }


        // ✅ EXPORT TO PDF
        [HttpGet]
        public IActionResult ExportStatisticsToPdf()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                var problems = _problemService.GetProblemsByStudentId(userId);
                var payments = _paymentService.GetPaymentsByStudentId(userId);
                var groupPayments = _paymentService.GetGroupPaymentsByUserId(userId);

                var data = new StudentStatisticsExportModel
                {
                    TotalProblems = problems.Count,
                    SolvedProblems = problems.Count(p => p.Status == ProblemStatus.Solved),
                    InProgressProblems = problems.Count(p => p.Status == ProblemStatus.InProgress),
                    WaitingProblems = problems.Count(p => p.Status == ProblemStatus.WaitingForTutor),
                    TotalSpent = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount)
                               + groupPayments.Where(gp => gp.Status == PaymentStatus.Completed).Sum(gp => gp.Amount),
                    PendingPayments = payments.Count(p => p.Status == PaymentStatus.Pending)
                                    + groupPayments.Count(gp => gp.Status == PaymentStatus.Pending),

                    ProblemsByType = problems.GroupBy(p => p.Type)
                        .Select(g => new TypeStatistic { Type = g.Key.ToString(), Count = g.Count() })
                        .ToList(),

                    ProblemsByDifficulty = problems.GroupBy(p => p.Difficulty)
                        .Select(g => new DifficultyStatistic { Difficulty = g.Key.ToString(), Count = g.Count() })
                        .ToList(),

                    ProblemsByMonth = problems.Where(p => p.CreatedDate >= DateTime.Now.AddMonths(-6))
                        .GroupBy(p => new { p.CreatedDate.Year, p.CreatedDate.Month })
                        .Select(g => new MonthStatistic
                        {
                            Month = $"{g.Key.Month}/{g.Key.Year}",
                            Count = g.Count()
                        })
                        .ToList(),

                    SpendingByMonth = payments.Where(p => p.Status == PaymentStatus.Completed && p.CompletedDate >= DateTime.Now.AddMonths(-6))
                        .GroupBy(p => new { p.CompletedDate!.Value.Year, p.CompletedDate.Value.Month })
                        .Select(g => new MonthStatistic
                        {
                            Month = $"{g.Key.Month}/{g.Key.Year}",
                            Amount = g.Sum(p => p.Amount)
                        })
                        .ToList()
                };

                var pdfBytes = _statisticsExportService.ExportStudentStatisticsToPdf(data, user?.FullName ?? "Học sinh");

                return File(pdfBytes, "application/pdf", $"ThongKeHocTap_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ExportStatisticsToPdf: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi xuất PDF!";
                return RedirectToAction("Statistics");
            }
        }

        // ✅ EXPORT TO EXCEL
        [HttpGet]
        public IActionResult ExportStatisticsToExcel()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                var problems = _problemService.GetProblemsByStudentId(userId);
                var payments = _paymentService.GetPaymentsByStudentId(userId);
                var groupPayments = _paymentService.GetGroupPaymentsByUserId(userId);

                var data = new StudentStatisticsExportModel
                {
                    TotalProblems = problems.Count,
                    SolvedProblems = problems.Count(p => p.Status == ProblemStatus.Solved),
                    InProgressProblems = problems.Count(p => p.Status == ProblemStatus.InProgress),
                    WaitingProblems = problems.Count(p => p.Status == ProblemStatus.WaitingForTutor),
                    TotalSpent = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount)
                               + groupPayments.Where(gp => gp.Status == PaymentStatus.Completed).Sum(gp => gp.Amount),
                    PendingPayments = payments.Count(p => p.Status == PaymentStatus.Pending)
                                    + groupPayments.Count(gp => gp.Status == PaymentStatus.Pending),

                    ProblemsByType = problems.GroupBy(p => p.Type)
                        .Select(g => new TypeStatistic { Type = g.Key.ToString(), Count = g.Count() })
                        .ToList(),

                    ProblemsByDifficulty = problems.GroupBy(p => p.Difficulty)
                        .Select(g => new DifficultyStatistic { Difficulty = g.Key.ToString(), Count = g.Count() })
                        .ToList(),

                    ProblemsByMonth = problems.Where(p => p.CreatedDate >= DateTime.Now.AddMonths(-6))
                        .GroupBy(p => new { p.CreatedDate.Year, p.CreatedDate.Month })
                        .Select(g => new MonthStatistic
                        {
                            Month = $"{g.Key.Month}/{g.Key.Year}",
                            Count = g.Count()
                        })
                        .ToList(),

                    SpendingByMonth = payments.Where(p => p.Status == PaymentStatus.Completed && p.CompletedDate >= DateTime.Now.AddMonths(-6))
                        .GroupBy(p => new { p.CompletedDate!.Value.Year, p.CompletedDate.Value.Month })
                        .Select(g => new MonthStatistic
                        {
                            Month = $"{g.Key.Month}/{g.Key.Year}",
                            Amount = g.Sum(p => p.Amount)
                        })
                        .ToList()
                };

                var excelBytes = _statisticsExportService.ExportStudentStatisticsToExcel(data, user?.FullName ?? "Học sinh");

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ThongKeHocTap_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ExportStatisticsToExcel: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi xuất Excel!";
                return RedirectToAction("Statistics");
            }
        }

    }
}
