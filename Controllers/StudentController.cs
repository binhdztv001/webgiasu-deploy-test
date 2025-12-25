using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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

        public StudentController(IProblemService problemService, ISolutionService solutionService, 
            IPaymentService paymentService, IUserService userService, IRatingService ratingService,
            IFriendshipService friendshipService, IMessageService messageService, 
            ICommunityService communityService, IHubContext<CommunityHub> hubContext, IProblemGroupService problemGroupService)
        {
            _problemService = problemService;
            _solutionService = solutionService;
            _paymentService = paymentService;
            _userService = userService;
            _ratingService = ratingService;
            _friendshipService = friendshipService;
            _messageService = messageService;
            _communityService = communityService;
            _hubContext = hubContext;
            _problemGroupService = problemGroupService;
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

                ViewBag.HasRated = await _ratingService.HasStudentRatedProblemAsync(id, userId);

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
                    TempData["Error"] = "Không tìm thấy bài toán hoặc chưa có gia sư nhận.";
                    return RedirectToAction("Dashboard");
                }

                if (await _ratingService.HasStudentRatedProblemAsync(problemId, userId))
                {
                    TempData["Warning"] = "Bạn đã đánh giá gia sư cho bài toán này rồi!";
                    return RedirectToAction("ProblemDetails", new { id = problemId });
                }

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
                    TempData["Success"] = "Đánh giá gia sư thành công! Cảm ơn phản hồi của bạn.";
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
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ProcessPayment: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi xử lý thanh toán!";
                return RedirectToAction("Payments");
            }
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
        public IActionResult UpdateProfile(string fullName, string email, string phoneNumber)
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

        public IActionResult MyProblems()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var problems = _problemService.GetProblemsByStudentId(userId);

                // Get groups user is member of
                var groups = _problemGroupService.GetGroupsByUserId(userId);

                // ✅ Create HashSet of problem IDs that are in groups
                var groupProblemIds = groups.Select(g => g.ProblemId).ToHashSet();

                // ✅ Create mapping: ProblemId -> GroupId for quick lookup
                var problemGroupMap = groups.ToDictionary(g => g.ProblemId, g => g.Id);

                ViewBag.GroupProblemIds = groupProblemIds;
                ViewBag.ProblemGroupMap = problemGroupMap;

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
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách gia sư!";
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
                    return Json(new { success = true, message = "Đã gửi lời mời kết bạn!" });
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
                    return Json(new { success = true, message = "Đã chấp nhận lời mời kết bạn!" });
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

        // ✅ UPDATE CreateProblem POST method
        [HttpPost]
        public IActionResult CreateProblem(CreateProblemViewModel model, decimal? CustomPrice, bool IsGroupMode, string? GroupName)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                if (ModelState.IsValid)
                {
                    // Calculate price
                    decimal price;

                    if (model.Difficulty == DifficultyLevel.options && CustomPrice.HasValue)
                    {
                        price = CustomPrice.Value;

                        if (price < 10000)
                        {
                            TempData["Error"] = "Giá tối thiểu là 10,000 đ!";
                            return View(model);
                        }
                    }
                    else
                    {
                        price = model.Difficulty switch
                        {
                            DifficultyLevel.Easy => 40000,
                            DifficultyLevel.Medium => 60000,
                            DifficultyLevel.Hard => 90000,
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
                        Difficulty = model.Difficulty,
                        ImageUrl = model.ImageFile != null ? $"/images/{model.ImageFile.FileName}" : "/images/default.jpg",
                        AttachmentFile = attachmentUrl,
                        Deadline = model.Deadline,
                        Price = price,
                        CreatedDate = DateTime.Now,
                        Status = ProblemStatus.WaitingForTutor
                    };

                    if (_problemService.CreateProblem(problem))
                    {
                        // ✅ If Group Mode, create Problem Group
                        if (IsGroupMode)
                        {
                            var group = _problemGroupService.CreateProblemGroup(
                                problemId: problem.Id,
                                createdByUserId: userId,
                                groupName: GroupName,
                                totalPrice: price
                            );

                            if (group != null)
                            {
                                TempData["Success"] = "Tạo nhóm và đăng bài toán thành công! Bạn có thể mời bạn bè tham gia.";
                                return RedirectToAction("GroupDetails", new { id = group.Id });
                            }
                            else
                            {
                                TempData["Warning"] = "Đăng bài toán thành công nhưng không thể tạo nhóm!";
                            }
                        }
                        else
                        {
                            // Individual mode - create payment as before
                            var payment = new Payment
                            {
                                StudentId = userId,
                                ProblemId = problem.Id,
                                Amount = price
                            };
                            _paymentService.CreatePayment(payment);

                            TempData["Success"] = "Đăng bài toán thành công!";
                        }

                        return RedirectToAction("Dashboard");
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

                if (group.CreatedByUserId == userId)
                    return Json(new { success = false, message = "Người tạo nhóm không thể rời khỏi nhóm!" });

                if (_problemGroupService.RemoveMember(groupId, userId))
                {
                    return Json(new { success = true, message = "Đã rời khỏi nhóm!" });
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
    }
}
