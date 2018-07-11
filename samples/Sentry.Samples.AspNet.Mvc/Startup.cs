using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Sentry.Samples.AspNet.Mvc.Startup))]
namespace Sentry.Samples.AspNet.Mvc
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
