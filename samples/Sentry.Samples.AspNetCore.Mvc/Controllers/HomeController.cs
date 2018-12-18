using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Samples.AspNetCore.Mvc.Models;
using Sentry;

namespace Samples.AspNetCore.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly IGameService _gameService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IGameService gameService, ILogger<HomeController> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Example: An exception that goes unhandled by the app will be captured by Sentry:
        [HttpPost]
        public async Task PostIndex(string @params)
        {
            try
            {
                if (@params == null)
                {
                    _logger.LogWarning("Param is null!", @params);
                }

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

        // Example: The view rendering throws: see about.cshtml
        public IActionResult About(string who = null)
        {
            if (who == null)
            {
                // Exemplifies using the logger to raise a warning which will be sent as an event because MinimumEventLevel was configured to Warning
                // ALso, the stack trace of this location will be sent (even though there was no exception) because of the configuration AttachStackTrace
                _logger.LogWarning("A {route} '{value}' was requested.",
                    // example structured logging where keys (in the template above) go as tag keys and values below:
                    "/about",
                    "null");
            }

            return View();
        }

        // Example: To take the Sentry Hub and submit errors directly:
        public IActionResult Contact(
            // Hub holds a Client and Scope management
            // Errors sent with the hub will include all context collected in the current scope
            [FromServices] IHub sentry)
        {
            try
            {
                // Some code block that could throw
                throw null;
            }
            catch (Exception e)
            {
                e.Data.Add("detail",
                    new
                    {
                        Reason = "There's a 'throw null' hard-coded here!",
                        IsCrazy = true
                    });

                var id = sentry.CaptureException(e);

                ViewData["Message"] = "An exception was caught and sent to Sentry! Event id: " + id;
            }
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public Task<int> MethodAsync() => new AsyncMethodClass().MethodAsync(1, 2, 3, 4);

        private class AsyncMethodClass
        {
            private Dictionary<int, int> _dict = new Dictionary<int, int>();
            public async Task<int> MethodAsync(int v0, int v1, int v2, int v3)
                => await MethodAsync(v0, v1, v2);
            private async Task<int> MethodAsync(int v0, int v1, int v2)
                => await MethodAsync(v0, v1);
            private async Task<int> MethodAsync(int v0, int v1)
                => await MethodAsync(v0);
            private async Task<int> MethodAsync(int v0)
                => await MethodAsync();
            private async Task<int> MethodAsync()
            {
                await Task.Delay(1000);
                int value = 0;
                foreach (var i in Sequence(0, 5))
                {
                    value += i;
                }
                return value;
            }
            private IEnumerable<int> Sequence(int start, int end)
            {
                for (var i = start; i <= end; i++)
                {
                    foreach (var item in Sequence(i))
                    {
                        yield return item;
                    }
                }
            }
            private IEnumerable<int> Sequence(int start)
            {
                var end = start + 10;
                for (var i = start; i <= end; i++)
                {
                    _dict[i] = _dict[i] + 1; // Throws exception
                    yield return i;
                }
            }
        }
    }
}
