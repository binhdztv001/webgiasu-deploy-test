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
        private readonly IFriendshipService _friendshipService;
        private readonly IMessageService _messageService;

        public StudentController(IProblemService problemService, ISolutionService solutionService, 
            IPaymentService paymentService, IUserService userService, IRatingService ratingService,
            IFriendshipService friendshipService, IMessageService messageService)
        {
            _problemService = problemService;
            _solutionService = solutionService;
            _paymentService = paymentService;
            _userService = userService;
            _ratingService = ratingService;
            _friendshipService = friendshipService;
            _messageService = messageService;
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

            var currentUser = _userService.GetUserById(userId);
            var hasPremium = PremiumService.HasPremium(currentUser);

            var problem = _problemService.GetProblemById(id);
            if (problem == null || problem.StudentId != userId)
            {
                return NotFound();
            }

            User assignedTutor = null;

            if (problem.AssignedTutorId.HasValue)
            {
                if (hasPremium)
                {
                    // Premium: xem full hồ sơ
                    assignedTutor = _userService.GetUserById(problem.AssignedTutorId.Value);
                }
                else
                {
                    // Free: chỉ xem thông tin cơ bản
                    var tutor = _userService.GetUserById(problem.AssignedTutorId.Value);
                    if (tutor != null)
                    {
                        assignedTutor = new User
                        {
                            Id = tutor.Id,
                            FullName = tutor.FullName,
                        };
                    }
                }
            }

            var model = new ProblemDetailsViewModel
            {
                Problem = problem,
                Student = _userService.GetUserById(problem.StudentId),
                AssignedTutor = assignedTutor,
                Solution = _solutionService.GetSolutionByProblemId(problem.Id),
                Payment = _paymentService.GetPaymentByProblemId(problem.Id)
            };

            // Check if student has rated this problem
            ViewBag.HasRated = await _ratingService.HasStudentRatedProblemAsync(id, userId);

            ViewBag.HasPremium = hasPremium;
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

        // API: Lấy tin nhắn với 1 user
        [HttpGet]
        public IActionResult GetMessages(int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) 
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            // Kiểm tra có phải bạn bè không
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

            return Json(new { 
                success = true, 
                messages = messageList,
                currentUserId = currentUserId,
                currentUserName = currentUser?.FullName
            });
        }

        // API: Lấy danh sách conversations
        [HttpGet]
        public IActionResult GetConversations()
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

            return Json(new { 
                success = true, 
                conversations = conversationList,
                totalUnreadCount = _messageService.GetTotalUnreadCount(currentUserId)
            });
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

        // ==================== FRIENDSHIP ACTIONS ====================
        
        public IActionResult FriendShip()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            // Lấy tất cả users (bao gồm cả Student và Tutor) trừ user đang đăng nhập
            var allUsers = _userService.GetUsersByRole(UserRole.Student)
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

        // API: Gửi lời mời kết bạn
        [HttpPost]
        public IActionResult SendFriendRequest([FromBody] FriendRequestModel model)
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

        // API: Chấp nhận lời mời kết bạn
        [HttpPost]
        public IActionResult AcceptFriendRequest([FromBody] FriendshipActionModel model)
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

        // API: Từ chối lời mời kết bạn
        [HttpPost]
        public IActionResult DeclineFriendRequest([FromBody] FriendshipActionModel model)
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

        // API: Hủy lời mời đã gửi
        [HttpPost]
        public IActionResult CancelFriendRequest([FromBody] FriendshipActionModel model)
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

        // API: Hủy kết bạn
        [HttpPost]
        public IActionResult Unfriend([FromBody] FriendRequestModel model)
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
            var userId = GetCurrentUserId();
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var messages = _messageService.GetRecentMessagesForDropdown(userId);

            ViewBag.TotalUnread = _messageService.GetTotalUnreadCount(userId);

            return View(messages);
        }

        // Đăng ký premium
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
