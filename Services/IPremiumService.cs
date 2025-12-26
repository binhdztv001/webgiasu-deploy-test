using Webgiasu.Models;

namespace Webgiasu.Services
{
    public interface IPremiumService
    {
        bool IsPremium(int userId);
        User? GetPremiumUser(int userId);
        void UpgradeToPremium(int userId);
    }
}
