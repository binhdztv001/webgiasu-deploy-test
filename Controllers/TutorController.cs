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

        public TutorController(IProblemService problemService, ISolutionService solutionService, IFriendshipService friendshipService,
            IUserService userService, IRatingService ratingService, IMessageService messageService, 
            ICommunityService communityService, IHubContext<CommunityHub> hubContext, IPremiumService premiumService)
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

        [HttpPost]
        public IActionResult AcceptProblem(int id)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in AcceptProblem: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi nhận bài toán!";
                return RedirectToAction("AvailableProblems");
            }
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

                    _userService.UpdateUser(user);
                    TempData["Success"] = "Cập nhật hồ sơ gia sư thành công!";
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

    }
}
