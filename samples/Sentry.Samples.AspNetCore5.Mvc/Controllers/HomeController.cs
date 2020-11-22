using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sentry.Samples.AspNetCore3.Mvc.Models;

namespace Samples.AspNetCore5.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger) => _logger = logger;

        public IActionResult Index()
        {
            // Raises an error event:
            _logger.LogError("Index was called.");
            return View();
        }

        public IActionResult Privacy()
        {
            // Raises an event only when looking for the view (after returning).
            // ReSharper disable once Mvc.ViewNotResolved
            return View("DoesNotExist");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}
