using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sentry.Samples.AspNetCore.Mvc.Models;

namespace Sentry.Samples.AspNetCore.Mvc.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public void PostIndex(string @params)
        {
            try
            {
                Thrower();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Invalid POST with {@params}! See Inner exception for details.", e);
            }
        }

        public void Thrower()
        {
            var ex1 = new Exception("Exception #1");
            var ex2 = new Exception("Exception #2");
            var ae = new AggregateException(ex1, ex2);

            ae.Data.Add("Extra", new
            {
                ErrorDetail = "I always throw!"
            });

            throw ae;
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
