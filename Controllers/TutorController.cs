using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Webgiasu.Hubs;
using Webgiasu.Models;
using Webgiasu.Models.ViewModels;
using Webgiasu.Services;

namespace Webgiasu.Controllers
{
    public class CommentCreateModel
    {
        public int PostId { get; set; }
        public string Content { get; set; } = "";
        public bool IsAnonymous { get; set; } = true;
    }

    public class UpdatePostModel
    {
        public int PostId { get; set; }
        public string Content { get; set; }
        public string? Category { get; set; }
    }

    public class DeletePostModel
    {
        public int PostId { get; set; }
    }

    public class ReactionRequest
    {
        public int PostId { get; set; }
        public int CommentId { get; set; }
        public string ReactionType { get; set; } = "like";
    }
    public class TutorController : Controller
    {
        private readonly IProblemService _problemService;
        private readonly ISolutionService _solutionService;
        private readonly IUserService _userService;
        private readonly IFriendshipService _friendshipService;
        private readonly IRatingService _ratingService;
        private readonly IMessageService _messageService;
        private readonly ICommunityService _communityService;
        private readonly IHubContext<CommunityHub> _hubContext;
        private readonly IPremiumService _premiumService;
        private readonly AppDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly IPaymentService _paymentService;
        private readonly ITutorApplicationService _tutorApplicationService;

        public TutorController(IProblemService problemService, ISolutionService solutionService, IFriendshipService friendshipService,
            IUserService userService, IRatingService ratingService, IMessageService messageService, 
            ICommunityService communityService, IHubContext<CommunityHub> hubContext, IPremiumService premiumService, AppDbContext db
            , INotificationService notificationService, IPaymentService paymentService, ITutorApplicationService tutorApplicationService)
        {
            _problemService = problemService;
            _solutionService = solutionService;
            _userService = userService;
            _friendshipService = friendshipService;
            _ratingService = ratingService;
            _messageService = messageService;
            _communityService = communityService;
            _hubContext = hubContext;
            _premiumService = premiumService;
            _db = db;
            _notificationService = notificationService;
            _paymentService = paymentService;
            _tutorApplicationService = tutorApplicationService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Dashboard: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang chủ!";
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult AvailableProblems()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var problems = _problemService.GetAvailableProblems();
                return View(problems);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in AvailableProblems: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách bài toán!";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult ProblemDetails(int id)
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

                var model = new ProblemDetailsViewModel
                {
                    Problem = problem,
                    Student = _userService.GetUserById(problem.StudentId),
                    AssignedTutor = problem.AssignedTutorId.HasValue ? _userService.GetUserById(problem.AssignedTutorId.Value) : null,
                    Solution = _solutionService.GetSolutionByProblemId(problem.Id)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ProblemDetails: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải chi tiết bài toán!";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet]
        public IActionResult ApplyForProblem(int problemId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var problem = _problemService.GetProblemById(problemId);
                if (problem == null)
                {
                    TempData["Error"] = "Không tìm thấy bài toán!";
                    return RedirectToAction("AvailableProblems");
                }

                // ✅ Kiểm tra bài toán có còn available không
                if (problem.Status != ProblemStatus.WaitingForTutor)
                {
                    TempData["Warning"] = "Bài toán này đã có Mentor nhận rồi!";
                    return RedirectToAction("AvailableProblems");
                }

                // ✅ Kiểm tra đã apply chưa
                if (_tutorApplicationService.HasTutorApplied(problemId, userId))
                {
                    TempData["Warning"] = "Bạn đã đăng ký bài này rồi!";
                    return RedirectToAction("MyApplications");
                }

                // ✅ Tạo model với giá trị mặc định
                var model = new ApplyProblemViewModel
                {
                    ProblemId = problemId,
                    ProblemTitle = problem.Title,
                    OriginalPrice = problem.Price,
                    Deadline = problem.Deadline,
                    ProposedPrice = problem.Price, // Default = giá gốc
                    EstimatedDays = CalculateEstimatedDays(problem.Deadline) // Tự động tính
                };

                ViewBag.Problem = problem;
                ViewBag.Student = _userService.GetUserById(problem.StudentId);

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ApplyForProblem GET: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("AvailableProblems");
            }
        }

        private int CalculateEstimatedDays(DateTime deadline)
        {
            var daysLeft = (deadline - DateTime.Now).Days;

            if (daysLeft <= 1) return 1;
            if (daysLeft <= 3) return 2;
            if (daysLeft <= 7) return Math.Max(1, daysLeft - 1);

            return Math.Min(7, daysLeft / 2); // Mặc định = 1/2 thời gian còn lại
        }

        // ✅ THAY THẾ AcceptProblem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyForProblem(ApplyProblemViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                if (!ModelState.IsValid)
                {
                    // ✅ Reload problem info khi validation fail
                    var prob = _problemService.GetProblemById(model.ProblemId);
                    if (prob != null)
                    {
                        model.ProblemTitle = prob.Title;
                        model.OriginalPrice = prob.Price;
                        model.Deadline = prob.Deadline;
                        ViewBag.Problem = prob;
                        ViewBag.Student = _userService.GetUserById(prob.StudentId);
                    }

                    TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                    return View(model);
                }

                var problem = _problemService.GetProblemById(model.ProblemId);
                if (problem == null || problem.Status != ProblemStatus.WaitingForTutor)
                {
                    TempData["Error"] = "Bài toán không còn khả dụng!";
                    return RedirectToAction("AvailableProblems");
                }

                var success = _tutorApplicationService.ApplyForProblem(
                    model.ProblemId,
                    userId,
                    model.Proposal,
                    model.ProposedPrice,
                    model.EstimatedDays
                );

                if (success)
                {
                    var tutor = _userService.GetUserById(userId);
                    if (tutor != null)
                    {
                        _notificationService.NotifyTutorApplied(
                            problem.StudentId,
                            model.ProblemId,
                            tutor.FullName
                        );
                    }

                    TempData["Success"] = "✅ Đã gửi đơn đăng ký thành công! Chờ học sinh duyệt.";
                    return RedirectToAction("MyApplications");
                }
                else
                {
                    TempData["Error"] = "Không thể đăng ký. Bạn có thể đã đăng ký trước đó!";
                    return RedirectToAction("AvailableProblems");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ApplyForProblem POST: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("AvailableProblems");
            }
        }

        // ✅ THÊM ACTION: Danh sách đơn đăng ký của Tutor
        public IActionResult MyApplications()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var applications = _tutorApplicationService.GetTutorApplications(userId);
            return View(applications);
        }

        public IActionResult MyProblems()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var problems = _problemService.GetProblemsByTutorId(userId);
                return View(problems);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in MyProblems: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách bài toán!";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet]
        public IActionResult SubmitSolution(int? problemId)
        {
            try
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

                // ✅ Thêm thông tin Student và Tutor cho AI Chat
                ViewBag.Student = _userService.GetUserById(problem.StudentId);
                ViewBag.Tutor = _userService.GetUserById(userId);

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SubmitSolution GET: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("MyProblems");
            }
        }

        [HttpPost]
        public IActionResult SubmitSolution(SubmitSolutionViewModel model)
        {
            try
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

                            // ✅ TẠO NOTIFICATION CHO STUDENT
                            _notificationService.NotifySolutionSubmitted(
                                problem.StudentId,
                                model.ProblemId
                            );

                            // ✅ NEW: TẠO NOTIFICATION CHO TUTOR
                            _notificationService.NotifyTutorSolutionSubmitted(
                                userId,
                                model.ProblemId,
                                problem.Title
                            );
                        }

                        TempData["Success"] = "Gửi lời giải thành công!";
                        return RedirectToAction("MyProblems");
                    }
                }

                var prob = _problemService.GetProblemById(model.ProblemId);
                ViewBag.Problem = prob;
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SubmitSolution POST: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi gửi lời giải!";
                return RedirectToAction("MyProblems");
            }
        }

        public IActionResult MySolutions()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var solutions = _solutionService.GetSolutionsByTutorId(userId);
                return View(solutions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in MySolutions: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách lời giải!";
                return RedirectToAction("Dashboard");
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

        // NEW: View My Ratings
        [HttpGet]
        public async Task<IActionResult> MyRatings()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var model = await _ratingService.GetTutorRatingsSummaryAsync(userId);
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in MyRatings: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải đánh giá!";
                return RedirectToAction("Dashboard");
            }
        }

        // Messages
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

        public IActionResult FriendShip()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var allUsers = _userService.GetUsersByRole(UserRole.Tutor)
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

        // Earnings / Payment History
        public IActionResult Earnings()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var solutions = _solutionService.GetSolutionsByTutorId(userId);

                // ✅ TÍNH TỔNG THU NHẬP THỰC TẾ - CHỈ TÍNH BÀI ĐÃ THANH TOÁN
                int totalEarnings = 0;

                foreach (var solution in solutions)
                {
                    var payment = _paymentService.GetPaymentByProblemId(solution.ProblemId);

                    // Chỉ tính khi payment đã completed
                    if (payment != null && payment.Status == PaymentStatus.Completed)
                    {
                        totalEarnings += (int)payment.Amount;
                    }
                }

                var model = new TutorEarningsViewModel
                {
                    Solutions = solutions,
                    TotalEarnings = totalEarnings
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Earnings: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải thu nhập!";
                return RedirectToAction("Dashboard");
            }
        }

        // Account Settings
        public IActionResult Settings()
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
                Console.WriteLine($"❌ Error in Settings: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public IActionResult UpdateTutorProfile(string? bio, string? subjects, string? education, int? experienceYears, string? certificates)
        {
            try
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

                    _userService.UpdateUser(user);  TempData["Success"] = "Cập nhật hồ sơ Mentor thành công!";
                }
                return RedirectToAction("Settings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in UpdateTutorProfile: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật hồ sơ!";
                return RedirectToAction("Settings");
            }
        }


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
                        isLiked
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

                // Build anonymous payload for realtime clients (author shown anonymous)
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

        [HttpGet]
        public async Task<IActionResult> GetAllPosts()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    Console.WriteLine("❌ User not authenticated");
                    return Unauthorized();
                }

                Console.WriteLine($"📡 GetAllPosts called by user {userId}");

                var posts = await _communityService.GetAllPostsAsync();
                Console.WriteLine($"📦 Retrieved {posts.Count} posts from service");

                var postsData = new List<object>(posts.Count);
                foreach (var p in posts)
                {
                    var likeCount = await _communityService.GetPostLikeCountAsync(p.Id);
                    var isLiked = await _communityService.IsPostLikedAsync(p.Id, userId);
                    var reactionStats = await _communityService.GetPostReactionStatsAsync(p.Id);
                    var userReaction = await _communityService.GetUserPostReactionAsync(p.Id, userId);

                    var postData = new
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
                    };

                    Console.WriteLine($"  📝 Post {p.Id}: {p.Content.Substring(0, Math.Min(50, p.Content.Length))}...");
                    postsData.Add(postData);
                }

                Console.WriteLine($"✅ Returning {postsData.Count} posts to client");
                return Json(postsData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetAllPosts: {ex.Message}");
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
                    IsAnonymous = model.IsAnonymous,
                    CreatedAt = DateTime.Now
                };

                var created = await _communityService.AddCommentAsync(comment);

                // Build payload: authorName MUST be anonymous pattern
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

                // Broadcast to group
                await _hubContext.Clients.Group($"post-{created.PostId}").SendAsync("CommentAdded", payload);

                return Json(new { success = true, comment = payload });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TogglePostLike([FromBody] int postId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var isLiked = await _communityService.TogglePostLikeAsync(postId, userId);
                var likeCount = await _communityService.GetPostLikeCountAsync(postId); // note: use your field name

                // broadcast realtime
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

                // need to get comment to know postId
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

                // Broadcast realtime
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

        [HttpGet]
        public async Task<IActionResult> GetPostReactions(int postId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var stats = await _communityService.GetPostReactionStatsAsync(postId);
                var userReaction = await _communityService.GetUserPostReactionAsync(postId, userId);

                return Json(new
                {
                    success = true,
                    reactionStats = stats,
                    userReaction
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCommentReactions(int commentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var stats = await _communityService.GetCommentReactionStatsAsync(commentId);
                var userReaction = await _communityService.GetUserCommentReactionAsync(commentId, userId);

                return Json(new
                {
                    success = true,
                    reactionStats = stats,
                    userReaction
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePost([FromBody] UpdatePostModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { success = false, message = "Chưa đăng nhập" });

            if (model == null || string.IsNullOrWhiteSpace(model.Content))
                return BadRequest(new { success = false, message = "Nội dung không được để trống" });

            try
            {
                var success = await _communityService.UpdatePostAsync(model.PostId, userId, model.Content, model.Category);

                if (!success)
                    return BadRequest(new { success = false, message = "Không thể cập nhật bài viết" });

                // Broadcast via SignalR
                await _hubContext.Clients.All.SendAsync("PostUpdated", new
                {
                    postId = model.PostId,
                    content = model.Content,
                    category = model.Category,
                    updatedAt = DateTime.Now.ToString("HH:mm, dd/MM")
                });

                return Json(new { success = true, message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost([FromBody] DeletePostModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var success = await _communityService.DeletePostAsync(model.PostId, userId);

                if (!success)
                    return BadRequest(new { success = false, message = "Không thể xóa bài viết" });

                // Broadcast via SignalR
                await _hubContext.Clients.All.SendAsync("PostDeleted", new { postId = model.PostId });

                return Json(new { success = true, message = "Xóa bài viết thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPostById(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

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

        // ==================== STATISTICS ====================
        public IActionResult Statistics()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var assignedProblems = _problemService.GetProblemsByTutorId(userId);
                var solutions = _solutionService.GetSolutionsByTutorId(userId);

                // Thống kê tổng quan
                ViewBag.TotalProblems = assignedProblems.Count;
                ViewBag.SolvedProblems = assignedProblems.Count(p => p.Status == ProblemStatus.Solved);
                ViewBag.InProgressProblems = assignedProblems.Count(p => p.Status == ProblemStatus.InProgress);
                ViewBag.WaitingProblems = 0; // Tutor không có trạng thái "chờ"

                // Thống kê thu nhập
                ViewBag.TotalEarnings = assignedProblems
                    .Where(p => p.Status == ProblemStatus.Solved)
                    .Sum(p => p.Price);
                ViewBag.PendingEarnings = assignedProblems
                    .Where(p => p.Status == ProblemStatus.InProgress)
                    .Sum(p => p.Price);

                // Thống kê theo môn học
                var problemsByType = assignedProblems.GroupBy(p => p.Type)
                    .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                    .ToList();
                ViewBag.ProblemsByType = (IEnumerable<dynamic>)problemsByType;

                // Thống kê theo độ khó
                var problemsByDifficulty = assignedProblems.GroupBy(p => p.Difficulty)
                    .Select(g => new { Difficulty = g.Key.ToString(), Count = g.Count() })
                    .ToList();
                ViewBag.ProblemsByDifficulty = (IEnumerable<dynamic>)problemsByDifficulty;

                // Thống kê theo thời gian (6 tháng gần nhất)
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);
                var problemsByMonth = assignedProblems.Where(p => p.CreatedDate >= sixMonthsAgo)
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

                // Thống kê thu nhập theo tháng
                var earningsByMonth = assignedProblems
                    .Where(p => p.Status == ProblemStatus.Solved && p.CreatedDate >= sixMonthsAgo)
                    .GroupBy(p => new { p.CreatedDate.Year, p.CreatedDate.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Month}/{g.Key.Year}",
                        Amount = g.Sum(p => p.Price),
                        Order = g.Key.Year * 12 + g.Key.Month
                    })
                    .OrderBy(x => x.Order)
                    .ToList();
                ViewBag.EarningsByMonth = (IEnumerable<dynamic>)earningsByMonth;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Statistics: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải thống kê!";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult MyTeachingClasses()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                if (user == null || user.Role != UserRole.Tutor)
                {
                    TempData["Error"] = "Chỉ Mentor mới có thể xem trang này!";
                    return RedirectToAction("Dashboard");
                }

                if (!user.Level.HasValue || user.Level.Value != EducationLevel.DaiHoc)
                {
                    TempData["Error"] = "Tính năng này chỉ dành cho Mentor Đại học!";
                    return RedirectToAction("Dashboard");
                }

                // ✅ LẤY DANH SÁCH LỚP HỌC MÀ MENTOR ĐANG DẠY
                var myClasses = _db.SchoolClasses
                    .Where(c => c.TutorId == userId)
                    .Include(c => c.School)
                    .Include(c => c.Students)
                    .OrderByDescending(c => c.CreatedDate)
                    .ToList();

                var classViewModels = myClasses.Select(c => new ClassSchoolViewModel
                {
                    Id = c.Id,
                    ClassName = c.ClassName ?? "Không có tên",
                    Subject = c.Subject ?? "Chưa xác định",
                    Description = c.Description ?? "",
                    SchoolName = c.School?.FullName ?? "Chưa cập nhật",
                    TutorName = user.FullName, // Chính mình
                    TutorEmail = user.Email ?? "",
                    StudentCount = _db.ClassStudents.Count(cs => cs.ClassId == c.Id),
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
                Console.WriteLine($"❌ Error in MyTeachingClasses: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách lớp học!";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult TeachingClassDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                if (user == null || user.Role != UserRole.Tutor ||
                    !user.Level.HasValue || user.Level.Value != EducationLevel.DaiHoc)
                {
                    TempData["Error"] = "Bạn không có quyền truy cập!";
                    return RedirectToAction("Dashboard");
                }

                // Kiểm tra xem Mentor có phải là giảng viên của lớp này không
                var classInfo = _db.SchoolClasses
                    .Include(c => c.School)
                    .Include(c => c.Tutor)
                    .FirstOrDefault(c => c.Id == id);

                if (classInfo == null)
                {
                    TempData["Error"] = "Không tìm thấy lớp học!";
                    return RedirectToAction("MyTeachingClasses");
                }

                if (classInfo.TutorId != userId)
                {
                    TempData["Error"] = "Bạn không phải là giảng viên của lớp này!";
                    return RedirectToAction("MyTeachingClasses");
                }

                // ✅ DEBUG: Log để kiểm tra
                Console.WriteLine($"📊 TeachingClassDetails - ClassId: {id}");

                // ✅ LẤY DANH SÁCH HỌC SINH ĐÚNG CÁCH
                var students = _db.ClassStudents
                    .Where(cs => cs.ClassId == id)
                    .Include(cs => cs.Student)
                    .Select(cs => new StudentInClassViewModel
                    {
                        Id = cs.Student!.Id,
                        FullName = cs.Student.FullName,
                        Email = cs.Student.Email ?? "",
                        PhoneNumber = cs.Student.PhoneNumber ?? "",
                        JoinedDate = cs.JoinedDate.ToString("dd/MM/yyyy")
                    })
                    .ToList();

                // ✅ LOG KẾT QUẢ
                Console.WriteLine($"✅ Found {students.Count} students");
                foreach (var s in students)
                {
                    Console.WriteLine($"   - {s.FullName} (ID: {s.Id})");
                }

                // ✅ GÁN VÀO VIEWBAG
                ViewBag.Class = classInfo;
                ViewBag.Students = students; // ← QUAN TRỌNG: Phải gán vào ViewBag
                ViewBag.School = classInfo.School;
                ViewBag.Tutor = classInfo.Tutor;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in TeachingClassDetails: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("MyTeachingClasses");
            }
        }

        public IActionResult MyClassSchedules()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                if (user == null || user.Role != UserRole.Tutor ||
                    !user.Level.HasValue || user.Level.Value != EducationLevel.DaiHoc)
                {
                    TempData["Error"] = "Tính năng này chỉ dành cho Mentor Đại học!";
                    return RedirectToAction("Dashboard");
                }

                // Lấy danh sách lớp học mà Mentor đang dạy
                var myClassIds = _db.SchoolClasses
                    .Where(c => c.TutorId == userId)
                    .Select(c => c.Id)
                    .ToList();

                // Lấy tất cả lịch trao đổi của các lớp học đó
                var schedules = _db.ClassSchedules
                    .Where(s => myClassIds.Contains(s.ClassId))
                    .Include(s => s.Class)
                        .ThenInclude(c => c!.School)
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

                // Thống kê
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

        public IActionResult ScheduleDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                if (user == null || user.Role != UserRole.Tutor ||
                    !user.Level.HasValue || user.Level.Value != EducationLevel.DaiHoc)
                {
                    TempData["Error"] = "Bạn không có quyền truy cập!";
                    return RedirectToAction("Dashboard");
                }

                // Kiểm tra xem Mentor có phải là giảng viên của lớp này không
                var schedule = _db.ClassSchedules
                    .Include(s => s.Class)
                        .ThenInclude(c => c!.School)
                    .Include(s => s.Creator)
                    .FirstOrDefault(s => s.Id == id);

                if (schedule == null)
                {
                    TempData["Error"] = "Không tìm thấy lịch trao đổi!";
                    return RedirectToAction("MyClassSchedules");
                }

                // Kiểm tra Mentor có dạy lớp này không
                if (schedule.Class?.TutorId != userId)
                {
                    TempData["Error"] = "Bạn không có quyền xem lịch trao đổi này!";
                    return RedirectToAction("MyClassSchedules");
                }

                // ✅ Lấy số học sinh từ bảng ClassStudents
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
                    TutorName = user.FullName, // Chính mình
                    TutorEmail = user.Email,
                    TotalStudents = studentCount
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

        [HttpPost]
        public IActionResult WithdrawApplication([FromBody] WithdrawApplicationRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                var success = _tutorApplicationService.WithdrawApplication(request.ApplicationId, userId);

                if (success)
                {
                    return Json(new { success = true, message = "Đã rút đơn thành công!" });
                }

                return Json(new { success = false, message = "Không thể rút đơn!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in WithdrawApplication: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        // ✅ THÊM REQUEST MODEL
        public class WithdrawApplicationRequest
        {
            public int ApplicationId { get; set; }
        }

    }
}
