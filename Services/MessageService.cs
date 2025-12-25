using Microsoft.EntityFrameworkCore;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public class MessageService : IMessageService
    {
        private readonly AppDbContext _context;
        private readonly IFriendshipService _friendshipService;

        public MessageService(AppDbContext context, IFriendshipService friendshipService)
        {
            _context = context;
            _friendshipService = friendshipService;
        }

        public List<User> GetConversations(int userId)
        {
            try
            {
                // Lấy danh sách user đãã chat (cho bạn bè)
                var friends = _friendshipService.GetFriends(userId);
                
                // Lấyy những người đã có tin nhắn
                var userIdsWithMessages = _context.Messages
                    .Where(m => (m.SenderId == userId || m.ReceiverId == userId) &&
                               !m.IsDeletedBySender && !m.IsDeletedByReceiver)
                    .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .ToList();

                // L?c ch? nh?ng b?n bè ?ã có tin nh?n
                var conversations = friends
                    .Where(f => userIdsWithMessages.Contains(f.Id))
                    .ToList();

                // Sắp xếp theo tin nhắn gần nhất
                conversations = conversations
                    .OrderByDescending(u => GetLastMessage(userId, u.Id)?.SentDate ?? DateTime.MinValue)
                    .ToList();

                return conversations;
            }
            catch
            {
                return new List<User>();
            }
        }

        public List<Message> GetMessages(int userId, int otherUserId, int take = 50)
        {
            try
            {
                // Kiểm tra có phải bạn bè không
                if (!_friendshipService.AreFriends(userId, otherUserId))
                {
                    return new List<Message>();
                }

                // Lấyy tin nhắn giữa 2 user
                var messages = _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Where(m => ((m.SenderId == userId && m.ReceiverId == otherUserId && !m.IsDeletedBySender) ||
                                (m.SenderId == otherUserId && m.ReceiverId == userId && !m.IsDeletedByReceiver)))
                    .OrderByDescending(m => m.SentDate)
                    .Take(take)
                    .ToList();

                // Reverse ?? tin nh?n c? ? trên
                messages.Reverse();

                return messages;
            }
            catch
            {
                return new List<Message>();
            }
        }

        public List<Message> GetRecentMessagesForDropdown(int userId, int take = 5)
        {
            return _context.Messages
                .Include(m => m.Sender)
                .Where(m =>
                    m.ReceiverId == userId &&
                    !m.IsDeletedByReceiver)
                .OrderByDescending(m => m.SentDate)
                .Take(take)
                .ToList();
        }


        public int GetUnreadCount(int userId, int senderId)
        {
            try
            {
                return _context.Messages
                    .Count(m => m.ReceiverId == userId && 
                               m.SenderId == senderId && 
                               !m.IsRead &&
                               !m.IsDeletedByReceiver);
            }
            catch
            {
                return 0;
            }
        }

        public int GetTotalUnreadCount(int userId)
        {
            try
            {
                return _context.Messages
                    .Count(m => m.ReceiverId == userId && 
                               !m.IsRead &&
                               !m.IsDeletedByReceiver);
            }
            catch
            {
                return 0;
            }
        }

        public Message? GetLastMessage(int userId, int otherUserId)
        {
            try
            {
                return _context.Messages
                    .Where(m => ((m.SenderId == userId && m.ReceiverId == otherUserId && !m.IsDeletedBySender) ||
                                (m.SenderId == otherUserId && m.ReceiverId == userId && !m.IsDeletedByReceiver)))
                    .OrderByDescending(m => m.SentDate)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}
