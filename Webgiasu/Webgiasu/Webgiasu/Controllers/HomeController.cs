using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Webgiasu.Models;

namespace Webgiasu.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Check if user is already logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (userId.HasValue && !string.IsNullOrEmpty(userRole))
            {
                return userRole switch
                {
                    "Student" => RedirectToAction("Dashboard", "Student"),
                    "Tutor" => RedirectToAction("Dashboard", "Tutor"),
                    "Admin" => RedirectToAction("Dashboard", "Admin"),
                    _ => View()
                };
            }
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
