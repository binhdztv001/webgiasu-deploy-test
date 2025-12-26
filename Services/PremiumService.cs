using Webgiasu.Models;

namespace Webgiasu.Services
{
    public class PremiumService : IPremiumService
    {
        private readonly AppDbContext _context;

        public PremiumService(AppDbContext context)
        {
            _context = context;
        }

        public bool IsPremium(int userId)
        {
            var user = _context.Users.Find(userId);
            if (user == null) return false;

            if (!user.IsPremium) return false;

            if (user.PremiumExpiredAt.HasValue &&
                user.PremiumExpiredAt <= DateTime.Now)
            {
                user.IsPremium = false;
                _context.SaveChanges();
                return false;
            }

            return true;
        }

        public User? GetPremiumUser(int userId)
        {
            var user = _context.Users.Find(userId);
            if (user == null) return null;

            if (user.IsPremium &&
                user.PremiumExpiredAt.HasValue &&
                user.PremiumExpiredAt <= DateTime.Now)
            {
                user.IsPremium = false;
                _context.SaveChanges();
            }

            return user;
        }

        public void UpgradeToPremium(int userId)
        {
            var user = _context.Users.Find(userId);
            if (user == null) return;

            if (user.PremiumExpiredAt.HasValue &&
                user.PremiumExpiredAt > DateTime.Now)
            {
                user.PremiumExpiredAt = user.PremiumExpiredAt.Value.AddMonths(1);
            }
            else
            {
                user.PremiumExpiredAt = DateTime.Now.AddMonths(1);
            }

            user.IsPremium = true;
            _context.SaveChanges();
        }
    }
}
