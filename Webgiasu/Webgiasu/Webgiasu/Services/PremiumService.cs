using Webgiasu.Models;

namespace Webgiasu.Services
{
    public static class PremiumService
    {
        public static bool HasPremium(User user)
        {
            if (user == null) return false;

            if (!user.IsPremium)
                return false;

            if (user.PremiumExpiredAt.HasValue)
                return user.PremiumExpiredAt.Value > DateTime.Now;

            return true;
        }
    }

}
