using System;
using Google.Cloud.Functions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sentry.Google.Cloud.Functions
{
    public class Startup : FunctionsStartup
    {

        // Hook as we do in:
        // SentryWebHostBuilderExtensions

        public override void ConfigureLogging(WebHostBuilderContext context, ILoggingBuilder logging)
        {
            base.ConfigureLogging(context, logging);
        }


        public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            base.ConfigureServices(context, services);
        }
    }
}
