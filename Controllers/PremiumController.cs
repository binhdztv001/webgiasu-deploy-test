using Microsoft.AspNetCore.Mvc;
using Webgiasu.Services;

namespace Webgiasu.Controllers
{
    public class PremiumController : Controller
    {
        private readonly IPremiumService _premiumService;

        public PremiumController(IPremiumService premiumService)
        {
            _premiumService = premiumService;
        }

        [HttpPost]
        public IActionResult Upgrade()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                // Check premium đúng domain
                if (_premiumService.IsPremium(userId.Value))
                    return BadRequest("Already premium");

                _premiumService.UpgradeToPremium(userId.Value);
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Premium Upgrade Error: {ex}");
                return StatusCode(500);
            }
        }
    }
}
