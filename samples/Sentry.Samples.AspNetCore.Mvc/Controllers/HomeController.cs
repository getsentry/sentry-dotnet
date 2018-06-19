using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Sentry.Samples.AspNetCore.Mvc.Models;

namespace Sentry.Samples.AspNetCore.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly IGameService _gameService;

        public HomeController(IGameService gameService) => _gameService = gameService;

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task PostIndex(string @params)
        {
            try
            {
                await _gameService.FetchNextPhaseDataAsync();
            }
            catch (Exception e)
            {
                var ioe = new InvalidOperationException("Bad POST! See Inner exception for details.", e);

                ioe.Data.Add("inventory",
                    // The following anonymous object gets serialized:
                    new
                    {
                        SmallPotion = 3,
                        BigPotion = 0,
                        CheeseWheels = 512
                    });

                throw ioe;
            }
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
