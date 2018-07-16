using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Sentry.EntityFramework;

namespace Sentry.Samples.AspNet.Mvc
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // We add the query logging here so multiple DbContexts in the same project are supported
            SentryDatabaseLogging.UseBreadcrumbs();

            // Set up the sentry SDK
            SentrySdk.Init(new SentryOptions
            {
                // We store the DSN inside Web.config; make sure to use your own DSN!
                Dsn = new Dsn(ConfigurationManager.AppSettings["SentryDsn"])
            }.AddEntityFramework());

        }

        // Global error catcher
        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            SentrySdk.CaptureException(exception);
        }
    }
}
