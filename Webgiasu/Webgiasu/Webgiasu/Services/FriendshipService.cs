using Microsoft.EntityFrameworkCore;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly AppDbContext _context;

        public FriendshipService(AppDbContext context)
        {
            _context = context;
        }

        public bool SendFriendRequest(int requesterId, int addresseeId)
        {
            try
            {
                // Ki?m tra không th? g?i l?i m?i cho chính mình
                if (requesterId == addresseeId)
                    return false;

                // Ki?m tra xem ?ã có friendship ch?a (c? 2 chi?u)
                var existing = _context.Friendships
                    .FirstOrDefault(f => 
                        (f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
                        (f.RequesterId == addresseeId && f.AddresseeId == requesterId));

                if (existing != null)
                {
                    // N?u ?ã có và ?ang pending ho?c accepted thì không cho g?i l?i
                    if (existing.Status == FriendshipStatus.Pending || 
                        existing.Status == FriendshipStatus.Accepted)
                        return false;
                    
                    // N?u declined ho?c blocked, có th? t?o l?i m?i m?i
                    _context.Friendships.Remove(existing);
                }

                var friendship = new Friendship
                {
                    RequesterId = requesterId,
                    AddresseeId = addresseeId,
                    Status = FriendshipStatus.Pending,
                    RequestedDate = DateTime.Now
                };

                _context.Friendships.Add(friendship);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AcceptFriendRequest(int friendshipId, int userId)
        {
            try
            {
                var friendship = _context.Friendships.Find(friendshipId);
                
                if (friendship == null || 
                    friendship.AddresseeId != userId || 
                    friendship.Status != FriendshipStatus.Pending)
                    return false;

                friendship.Status = FriendshipStatus.Accepted;
                friendship.RespondedDate = DateTime.Now;
                
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DeclineFriendRequest(int friendshipId, int userId)
        {
            try
            {
                var friendship = _context.Friendships.Find(friendshipId);
                
                if (friendship == null || 
                    friendship.AddresseeId != userId || 
                    friendship.Status != FriendshipStatus.Pending)
                    return false;

                friendship.Status = FriendshipStatus.Declined;
                friendship.RespondedDate = DateTime.Now;
                
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool CancelFriendRequest(int friendshipId, int userId)
        {
            try
            {
                var friendship = _context.Friendships.Find(friendshipId);
                
                if (friendship == null || 
                    friendship.RequesterId != userId || 
                    friendship.Status != FriendshipStatus.Pending)
                    return false;

                _context.Friendships.Remove(friendship);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Unfriend(int userId, int friendId)
        {
            try
            {
                var friendship = _context.Friendships
                    .FirstOrDefault(f => 
                        ((f.RequesterId == userId && f.AddresseeId == friendId) ||
                         (f.RequesterId == friendId && f.AddresseeId == userId)) &&
                        f.Status == FriendshipStatus.Accepted);

                if (friendship == null)
                    return false;

                _context.Friendships.Remove(friendship);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<Friendship> GetReceivedFriendRequests(int userId)
        {
            return _context.Friendships
                .Include(f => f.Requester)
                .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                .OrderByDescending(f => f.RequestedDate)
                .ToList();
        }

        public List<Friendship> GetSentFriendRequests(int userId)
        {
            return _context.Friendships
                .Include(f => f.Addressee)
                .Where(f => f.RequesterId == userId && f.Status == FriendshipStatus.Pending)
                .OrderByDescending(f => f.RequestedDate)
                .ToList();
        }

        public List<User> GetFriends(int userId)
        {
            var friendships = _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f => 
                    (f.RequesterId == userId || f.AddresseeId == userId) && 
                    f.Status == FriendshipStatus.Accepted)
                .ToList();

            var friends = new List<User>();
            foreach (var friendship in friendships)
            {
                if (friendship.RequesterId == userId)
                    friends.Add(friendship.Addressee);
                else
                    friends.Add(friendship.Requester);
            }

            return friends;
        }

        public FriendshipStatus? GetFriendshipStatus(int userId, int friendId)
        {
            var friendship = _context.Friendships
                .FirstOrDefault(f => 
                    (f.RequesterId == userId && f.AddresseeId == friendId) ||
                    (f.RequesterId == friendId && f.AddresseeId == userId));

            return friendship?.Status;
        }

        public bool AreFriends(int userId, int friendId)
        {
            return _context.Friendships
                .Any(f => 
                    ((f.RequesterId == userId && f.AddresseeId == friendId) ||
                     (f.RequesterId == friendId && f.AddresseeId == userId)) &&
                    f.Status == FriendshipStatus.Accepted);
        }

        public Friendship? GetFriendshipById(int friendshipId)
        {
            return _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .FirstOrDefault(f => f.Id == friendshipId);
        }
    }
}
