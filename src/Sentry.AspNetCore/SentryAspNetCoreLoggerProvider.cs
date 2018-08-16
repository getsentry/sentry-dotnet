using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore
{
    public class SentryAspNetCoreLoggerProvider : SentryLoggerProvider
    {
        public SentryAspNetCoreLoggerProvider(IOptions<SentryAspNetCoreOptions> options)
            : base(options)
        {
        }
    }

    internal class SentryAspNetCoreOptionsSetup : ConfigureFromConfigurationOptions<SentryAspNetCoreOptions>
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public SentryAspNetCoreOptionsSetup(
            ILoggerProviderConfiguration<SentryAspNetCoreLoggerProvider> providerConfiguration,
            IHostingEnvironment hostingEnvironment)
            : base(providerConfiguration.Configuration)
            => _hostingEnvironment = hostingEnvironment;

        public override void Configure(SentryAspNetCoreOptions options)
        {
            base.Configure(options);

            options.Environment
                = options.Environment // Don't override user defined value
                  ?? _hostingEnvironment?.EnvironmentName;
        }
    }
}
