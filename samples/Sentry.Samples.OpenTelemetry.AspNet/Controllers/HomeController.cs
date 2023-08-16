using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Sentry;

namespace Sentry.Samples.OpenTelemetry.AspNet.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            SentrySdk.CaptureMessage("Hello Sentry!");
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Sentry ASP.NET Sample.";
            DoWork();
            return View();
        }

        private void DoWork()
        {             
            // Instrument this using OpenTelemetry for distributed tracing and performance.
            // Using Sentry's OpenTelemetry integration these traces will be sent to Sentry.
            using (var activity = Telemetry.ActivitySource.StartActivity("DoWork"))
            {
                activity.AddTag("work", "100ms");
                Thread.Sleep(100); // Simulate some work
            }
        }

        public ActionResult Breakup()
        {
            throw new NotImplementedException(); // Simulate a bug... Sentry captures these too :-)
        }
    }
}