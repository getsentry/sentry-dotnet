using System;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;
using Sentry.Internal;
#if NETSTANDARD2_0
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#endif

namespace Sentry.AspNetCore
{
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

            // Don't override user defined value.
            if (string.IsNullOrWhiteSpace(options.Environment))
            {
                var locatedEnvironment = EnvironmentLocator.Locate();
                if (!string.IsNullOrWhiteSpace(locatedEnvironment))
                {
                    // Sentry specific environment takes precedence #92.
                    options.Environment = locatedEnvironment;
                }
                else
                {
                    // NOTE: Sentry prefers to have it's environment setting to be all lower case.
                    //       .NET Core sets the ENV variable to 'Production' (upper case P) or
                    //       'Development' (upper case D) which conflicts with the Sentry recommendation.
                    //       As such, we'll be kind and override those values, here ... if applicable.
                    // Assumption: The Hosting Environment is always set.
                    //             If not set by a developer, then the framework will auto set it.
                    //             Alternatively, developers might set this to a CUSTOM value, which we
                    //             need to respect (especially the case-sensitivity).
                    //             REF: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments
#if NETSTANDARD2_0
                    if (_hostingEnvironment.EnvironmentName.Equals("Production"))
                    {
                        options.Environment = Internal.Constants.DefaultEnvironmentSetting;
                    }
                    else if (_hostingEnvironment.EnvironmentName.Equals("Development"))
                    {
                        options.Environment = "development";
                    }
#else
                    if (_hostingEnvironment.EnvironmentName.Equals(Microsoft.Extensions.Hosting.Environments.Production))
                    {
                        options.Environment = Internal.Constants.DefaultEnvironmentSetting;
                    }
                    else if (_hostingEnvironment.EnvironmentName.Equals(Microsoft.Extensions.Hosting.Environments.Development))
                    {
                        options.Environment = Microsoft.Extensions.Hosting.Environments.Development.ToLowerInvariant();
                    }
#endif
                    else
                    {
                        // Use the value set by the developer.
                        options.Environment = _hostingEnvironment.EnvironmentName;
                    }
                }
            }

            options.AddLogEntryFilter((category, level, eventId, exception)
                // https://github.com/aspnet/KestrelHttpServer/blob/0aff4a0440c2f393c0b98e9046a8e66e30a56cb0/src/Kestrel.Core/Internal/Infrastructure/KestrelTrace.cs#L33
                // 13 = Application unhandled exception, which is captured by the middleware so the LogError of kestrel ends up as a duplicate with less info
                => eventId.Id == 13
                   && string.Equals(
                       category,
                       "Microsoft.AspNetCore.Server.Kestrel",
                       StringComparison.Ordinal));
        }
    }
}
