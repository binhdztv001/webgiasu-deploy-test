using Webgiasu.Models;

namespace Webgiasu.Services
{
    public interface INotificationService
    {
        List<Notification> GetRecentNotifications(int userId, int count = 5);
        List<Notification> GetAllNotifications(int userId);
        int GetUnreadCount(int userId);
        bool CreateNotification(Notification notification);
        bool MarkAsRead(int notificationId);
        bool MarkAllAsRead(int userId);

        // Helper methods
        bool NotifyProblemCreated(int userId, int problemId, string problemTitle);
        bool NotifyTutorAccepted(int userId, int problemId, string tutorName);
        bool NotifySolutionSubmitted(int userId, int problemId);
        bool NotifyPaymentCompleted(int userId, int paymentId, decimal amount);
        bool NotifyFriendRequestSent(int receiverId, int senderId, string senderName);
        bool NotifyFriendRequestAccepted(int userId, int acceptedByUserId, string acceptedByUserName);
        bool NotifyTutorSolutionSubmitted(int tutorId, int problemId, string problemTitle);
        bool NotifyTutorRatingReceived(int tutorId, int problemId, string studentName, int stars);
        bool NotifyTutorPaymentReceived(int tutorId, int problemId, decimal amount);
        bool NotifyTutorApplied(int studentId, int problemId, string tutorName);
        bool NotifyApplicationApproved(int tutorId, int problemId, string problemTitle);
        bool NotifyApplicationRejected(int tutorId, int problemId, string reason);
        bool NotifyEnterpriseMentorCreated(int enterpriseId, int mentorId, string mentorName);
    }
}

