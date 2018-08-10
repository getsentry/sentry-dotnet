using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.DependencyInjection;

[assembly: TestFramework("Sentry.Extensions.Logging.Tests.Startup", "Sentry.Extensions.Logging.Tests")]
namespace Sentry.Extensions.Logging.Tests
{
    public class Startup : DependencyInjectionTestFramework
    {
        public Startup(IMessageSink messageSink) : base(messageSink) { }

        protected override IServiceProvider ConfigureServices(IServiceCollection services)
        {
            IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
            services.AddSingleton<IConfiguration>(configuration);

            services.AddLogging(builder => builder.AddConfiguration(configuration.GetSection("Logging")).AddSentry());

            return services.BuildServiceProvider();
        }
    }
}
