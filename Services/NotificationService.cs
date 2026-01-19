using Webgiasu.Models;
using Microsoft.EntityFrameworkCore;

namespace Webgiasu.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;

        public NotificationService(AppDbContext db)
        {
            _db = db;
        }

        public List<Notification> GetRecentNotifications(int userId, int count = 5)
        {
            return _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(count)
                .ToList();
        }

        public int GetUnreadCount(int userId)
        {
            return _db.Notifications
                .Count(n => n.UserId == userId && !n.IsRead);
        }

        public bool CreateNotification(Notification notification)
        {
            try
            {
                _db.Notifications.Add(notification);
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating notification: {ex.Message}");
                return false;
            }
        }

        public bool MarkAsRead(int notificationId)
        {
            try
            {
                var notification = _db.Notifications.Find(notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                    _db.SaveChanges();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool MarkAllAsRead(int userId)
        {
            try
            {
                var notifications = _db.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToList();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                _db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ========== STUDENT NOTIFICATIONS ==========

        public bool NotifyProblemCreated(int userId, int problemId, string problemTitle)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = NotificationType.ProblemCreated,
                Title = "Đăng bài thành công",
                Message = $"Bài toán '{problemTitle}' đã được đăng thành công",
                Link = $"/Student/ProblemDetails?id={problemId}", // ✅ FIX
                ProblemId = problemId
            };

            return CreateNotification(notification);
        }

        public bool NotifyTutorAccepted(int userId, int problemId, string tutorName)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = NotificationType.TutorAccepted,
                Title = "Mentor đã nhận yêu cầu",
                Message = $"{tutorName} đã nhận bài toán của bạn",
                Link = $"/Student/ProblemDetails?id={problemId}", // ✅ FIX
                ProblemId = problemId
            };

            return CreateNotification(notification);
        }

        public bool NotifySolutionSubmitted(int userId, int problemId)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = NotificationType.SolutionSubmitted,
                Title = "Bài giải đã sẵn sàng",
                Message = "Mentor đã hoàn thành bài toán của bạn",
                Link = $"/Student/ProblemDetails?id={problemId}", // ✅ FIX
                ProblemId = problemId
            };

            return CreateNotification(notification);
        }

        public bool NotifyPaymentCompleted(int userId, int paymentId, decimal amount)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = NotificationType.PaymentCompleted,
                Title = "Thanh toán thành công",
                Message = $"Giao dịch {amount:N0}đ hoàn tất",
                Link = $"/Student/Payments", // ✅ OK
                PaymentId = paymentId
            };

            return CreateNotification(notification);
        }

        public bool NotifyFriendRequestSent(int receiverId, int senderId, string senderName)
        {
            var notification = new Notification
            {
                UserId = receiverId,
                Type = NotificationType.FriendRequest,
                Title = "Lời mời kết bạn mới",
                Message = $"{senderName} đã gửi lời mời kết bạn cho bạn",
                Link = "/Student/FriendShip" // ✅ OK
            };

            return CreateNotification(notification);
        }

        public bool NotifyFriendRequestAccepted(int userId, int acceptedByUserId, string acceptedByUserName)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = NotificationType.FriendRequest,
                Title = "Lời mời kết bạn được chấp nhận",
                Message = $"{acceptedByUserName} đã chấp nhận lời mời kết bạn của bạn",
                Link = "/Student/FriendShip" // ✅ OK
            };

            return CreateNotification(notification);
        }

        // ========== TUTOR NOTIFICATIONS ==========

        public bool NotifyTutorSolutionSubmitted(int tutorId, int problemId, string problemTitle)
        {
            var notification = new Notification
            {
                UserId = tutorId,
                Type = NotificationType.SolutionSubmitted,
                Title = "Gửi bài giải thành công",
                Message = $"Bạn đã gửi lời giải cho bài toán '{problemTitle}'",
                Link = $"/Tutor/ProblemDetails?id={problemId}", // ✅ FIX
                ProblemId = problemId
            };

            return CreateNotification(notification);
        }

        public bool NotifyTutorRatingReceived(int tutorId, int problemId, string studentName, int stars)
        {
            var starText = stars switch
            {
                5 => "⭐⭐⭐⭐⭐ Xuất sắc",
                4 => "⭐⭐⭐⭐ Tốt",
                3 => "⭐⭐⭐ Trung bình",
                2 => "⭐⭐ Cần cải thiện",
                1 => "⭐ Kém",
                _ => $"{stars} sao"
            };

            var notification = new Notification
            {
                UserId = tutorId,
                Type = NotificationType.RatingReceived,
                Title = "Nhận được đánh giá mới",
                Message = $"{studentName} đã đánh giá {starText} cho bài giải của bạn",
                Link = $"/Tutor/MyRatings", // ✅ OK
                ProblemId = problemId
            };

            return CreateNotification(notification);
        }

        public bool NotifyTutorPaymentReceived(int tutorId, int problemId, decimal amount)
        {
            var notification = new Notification
            {
                UserId = tutorId,
                Type = NotificationType.PaymentReceived,
                Title = "Nhận được thanh toán",
                Message = $"Bạn đã nhận {amount:N0}đ từ bài giải ###{problemId}",
                Link = $"/Tutor/Earnings", // ✅ OK
                ProblemId = problemId,
                PaymentId = problemId
            };

            return CreateNotification(notification);
        }

        // ========== NEW APPLICATION SYSTEM ==========

        /// <summary>
        /// Thông báo cho Student khi có Tutor apply
        /// </summary>
        public bool NotifyTutorApplied(int studentId, int problemId, string tutorName)
        {
            try
            {
                if (studentId <= 0 || problemId <= 0 || string.IsNullOrWhiteSpace(tutorName))
                {
                    Console.WriteLine("❌ Invalid notification parameters");
                    return false;
                }

                var notification = new Notification
                {
                    UserId = studentId,
                    Type = NotificationType.TutorApplied,
                    Title = "Mentor mới đăng ký",
                    Message = $"{tutorName} đã đăng ký nhận bài toán của bạn",
                    Link = $"/Student/ViewTutorApplications?problemId={problemId}", // ✅ FIX
                    ProblemId = problemId,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                return CreateNotification(notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error NotifyTutorApplied: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Thông báo cho Tutor khi application được chấp nhận
        /// </summary>
        public bool NotifyApplicationApproved(int tutorId, int problemId, string problemTitle)
        {
            try
            {
                if (tutorId <= 0 || problemId <= 0 || string.IsNullOrWhiteSpace(problemTitle))
                {
                    Console.WriteLine("❌ Invalid notification parameters");
                    return false;
                }

                var notification = new Notification
                {
                    UserId = tutorId,
                    Type = NotificationType.ApplicationApproved,
                    Title = "Đơn đăng ký được chấp nhận",
                    Message = $"Bạn đã được chọn giải bài '{problemTitle}'",
                    Link = $"/Tutor/ProblemDetails?id={problemId}", // ✅ FIX
                    ProblemId = problemId,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                return CreateNotification(notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error NotifyApplicationApproved: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Thông báo cho Tutor khi application bị từ chối
        /// </summary>
        public bool NotifyApplicationRejected(int tutorId, int problemId, string reason)
        {
            try
            {
                if (tutorId <= 0 || problemId <= 0)
                {
                    return false;
                }

                var notification = new Notification
                {
                    UserId = tutorId,
                    Type = NotificationType.ApplicationRejected,
                    Title = "Đơn đăng ký bị từ chối",
                    Message = !string.IsNullOrWhiteSpace(reason)
                        ? $"Đơn đăng ký của bạn không được chấp nhận: {reason}"
                        : "Học sinh đã chọn Mentor khác",
                    Link = $"/Tutor/MyApplications", // ✅ FIX
                    ProblemId = problemId,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                return CreateNotification(notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error NotifyApplicationRejected: {ex.Message}");
                return false;
            }
        }
    }
}