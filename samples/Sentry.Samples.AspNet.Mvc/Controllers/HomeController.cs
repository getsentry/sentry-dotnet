using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Sentry.Samples.AspNet.Mvc.Models;

namespace Sentry.Samples.AspNet.Mvc.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }


        // Example: An exception that goes unhandled by the app will be captured by Sentry:
        [HttpPost]
        public ActionResult PostIndex(string @params)
        {
            try
            {
                if (@params == null)
                {
                    throw new ArgumentNullException(nameof(@params), "Param is null!");
                }

                throw new Exception();
            }
            catch (Exception e)
            {
                var ioe = new InvalidOperationException("Bad POST! See Inner exception for details.", e);

                ioe.Data.Add("inventory",
                    new Extra
                    {
                        SmallPotion = 3,
                        BigPotion = 0,
                        CheeseWheels = 512
                    });

                throw ioe;
            }
        }


        // Example: An entity validation exception that goes unhandled by the app will be captured by Sentry:
        [HttpPost]
        public ActionResult ThrowEntityFramework()
        {
            using (var db = new ApplicationDbContext())
            {
                var user = new ApplicationUser();
                db.Users.Add(user);

                // This will throw a DbEntityValidationException
                db.SaveChanges();
            }

            // This never gets called
            return View("Index");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [Serializable]
        public class Extra
        {
            public int SmallPotion { get; set; }
            public int BigPotion { get; set; }
            public int CheeseWheels { get; set; }
        }
    }
}
