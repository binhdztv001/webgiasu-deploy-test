using Webgiasu.Models;

namespace Webgiasu.Services
{
    public interface IFriendshipService
    {
        // G?i l?i m?i k?t b?n
        bool SendFriendRequest(int requesterId, int addresseeId);
        
        // Ch?p nh?n l?i m?i k?t b?n
        bool AcceptFriendRequest(int friendshipId, int userId);
        
        // T? ch?i l?i m?i k?t b?n
        bool DeclineFriendRequest(int friendshipId, int userId);
        
        // H?y l?i m?i ?ã g?i
        bool CancelFriendRequest(int friendshipId, int userId);
        
        // H?y k?t b?n
        bool Unfriend(int userId, int friendId);
        
        // L?y danh sách l?i m?i k?t b?n nh?n ???c
        List<Friendship> GetReceivedFriendRequests(int userId);
        
        // L?y danh sách l?i m?i ?ã g?i
        List<Friendship> GetSentFriendRequests(int userId);
        
        // L?y danh sách b?n bè
        List<User> GetFriends(int userId);
        
        // Ki?m tra tr?ng thái k?t b?n
        FriendshipStatus? GetFriendshipStatus(int userId, int friendId);
        
        // Ki?m tra ?ã là b?n bè ch?a
        bool AreFriends(int userId, int friendId);
        
        // L?y friendship theo id
        Friendship? GetFriendshipById(int friendshipId);
    }
}
