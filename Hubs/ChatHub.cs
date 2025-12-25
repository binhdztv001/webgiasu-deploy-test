using Microsoft.AspNetCore.SignalR;
using Webgiasu.Models;
using Webgiasu.Services;

namespace Webgiasu.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IFriendshipService _friendshipService;
        private readonly AppDbContext _context;
        
        // Dictionary ?? l?u mapping userId -> connectionId
        private static readonly Dictionary<int, string> _connections = new Dictionary<int, string>();

        public ChatHub(IFriendshipService friendshipService, AppDbContext context)
        {
            _friendshipService = friendshipService;
            _context = context;
        }

        // Khi user k?t n?i
        public async Task Connect(int userId)
        {
            _connections[userId] = Context.ConnectionId;
            await Clients.Caller.SendAsync("Connected", userId);
        }

        // Khi user ng?t k?t n?i
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = _connections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (userId != 0)
            {
                _connections.Remove(userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // G?i tin nh?n
        public async Task SendMessage(int senderId, int receiverId, string content)
        {
            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(content))
                {
                    await Clients.Caller.SendAsync("Error", "N?i dung tin nh?n không ???c r?ng!");
                    return;
                }

                // Ki?m tra 2 user có là b?n bè không
                if (!_friendshipService.AreFriends(senderId, receiverId))
                {
                    await Clients.Caller.SendAsync("Error", "B?n ch? có th? chat v?i b?n bè!");
                    return;
                }

                // T?o message
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content.Trim(),
                    SentDate = DateTime.Now,
                    IsRead = false,
                    Type = MessageType.Text,
                    IsDeletedBySender = false,
                    IsDeletedByReceiver = false
                };

                // L?u vào database
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Load sender info
                var sender = _context.Users.Find(senderId);

                // T?o object ?? g?i ?i
                var messageDto = new
                {
                    id = message.Id,
                    senderId = message.SenderId,
                    receiverId = message.ReceiverId,
                    content = message.Content,
                    sentDate = message.SentDate.ToString("HH:mm"),
                    senderName = sender?.FullName,
                    isRead = message.IsRead
                };

                // G?i cho ng??i nh?n (n?u online)
                if (_connections.ContainsKey(receiverId))
                {
                    var receiverConnectionId = _connections[receiverId];
                    await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", messageDto);
                }

                // G?i l?i cho ng??i g?i (?? hi?n th? tin nh?n v?a g?i)
                await Clients.Caller.SendAsync("MessageSent", messageDto);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", "Có l?i x?y ra khi g?i tin nh?n!");
                Console.WriteLine($"Error in SendMessage: {ex.Message}");
            }
        }

        // ?ánh d?u tin nh?n ?ã ??c
        public async Task MarkAsRead(int messageId, int userId)
        {
            try
            {
                var message = _context.Messages.Find(messageId);
                if (message != null && message.ReceiverId == userId && !message.IsRead)
                {
                    message.IsRead = true;
                    message.ReadDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Thông báo cho ng??i g?i (n?u online)
                    if (_connections.ContainsKey(message.SenderId))
                    {
                        var senderConnectionId = _connections[message.SenderId];
                        await Clients.Client(senderConnectionId).SendAsync("MessageRead", messageId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MarkAsRead: {ex.Message}");
            }
        }

        // User ?ang gõ tin nh?n
        public async Task Typing(int senderId, int receiverId)
        {
            if (_connections.ContainsKey(receiverId))
            {
                var receiverConnectionId = _connections[receiverId];
                await Clients.Client(receiverConnectionId).SendAsync("UserTyping", senderId);
            }
        }

        // User d?ng gõ
        public async Task StopTyping(int senderId, int receiverId)
        {
            if (_connections.ContainsKey(receiverId))
            {
                var receiverConnectionId = _connections[receiverId];
                await Clients.Client(receiverConnectionId).SendAsync("UserStoppedTyping", senderId);
            }
        }
    }
}
