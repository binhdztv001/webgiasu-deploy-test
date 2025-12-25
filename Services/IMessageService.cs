using Webgiasu.Models;

namespace Webgiasu.Services
{
    public interface IMessageService
    {
        // L?y danh sách conversation (ng??i ?ã chat)
        List<User> GetConversations(int userId);
        
        // L?y tin nh?n gi?a 2 user
        List<Message> GetMessages(int userId, int otherUserId, int take = 50);
        List<Message> GetRecentMessagesForDropdown(int userId, int take = 5);

        // ??m s? tin nh?n ch?a ??c
        int GetUnreadCount(int userId, int senderId);
        
        // ??m t?ng s? tin nh?n ch?a ??c
        int GetTotalUnreadCount(int userId);
        
        // L?y tin nh?n cu?i cùng v?i m?i ng??i
        Message? GetLastMessage(int userId, int otherUserId);
    }
}
