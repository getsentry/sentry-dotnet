using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;
using Sentry.Internal;
#if NETSTANDARD2_0
using IHostingEnvironment = Microsoft.Extensions.Hosting.IHostingEnvironment;
#else
using IHostingEnvironment = Microsoft.Extensions.Hosting.IHostEnvironment;
#endif

namespace Sentry.AspNetCore
{
    /// <summary>
    /// Sets up ASP.NET Core option for Sentry.
    /// </summary>
    public class SentryAspNetCoreOptionsSetup : ConfigureFromConfigurationOptions<SentryAspNetCoreOptions>
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        /// <summary>
        /// Creates a new instance of <see cref="SentryAspNetCoreOptionsSetup"/>.
        /// </summary>
        public SentryAspNetCoreOptionsSetup(
            ILoggerProviderConfiguration<SentryAspNetCoreLoggerProvider> providerConfiguration,
            IHostingEnvironment hostingEnvironment)
            : base(providerConfiguration.Configuration)
            => _hostingEnvironment = hostingEnvironment;

        /// <summary>
        /// Configures the <see cref="SentryAspNetCoreOptions"/>.
        /// </summary>
        public override void Configure(SentryAspNetCoreOptions options)
        {
            base.Configure(options);

            // Don't override user defined value.
            if (string.IsNullOrWhiteSpace(options.Environment))
            {
                var locatedEnvironment = EnvironmentLocator.LocateFromEnvironmentVariable();
                if (!string.IsNullOrWhiteSpace(locatedEnvironment))
                {
                    // Sentry specific environment takes precedence #92.
                    options.Environment = locatedEnvironment;
                }
                else if (options.AdjustStandardEnvironmentNameCasing)
                {
                    // NOTE: Sentry prefers to have its environment setting to be all lower case.
                    //       .NET Core sets the ENV variable to 'Production' (upper case P),
                    //       'Development' (upper case D) or 'Staging' (upper case S) which conflicts with
                    //       the Sentry recommendation. As such, we'll be kind and override those values,
                    //       here ... if applicable.
                    // Assumption: The Hosting Environment is always set.
                    //             If not set by a developer, then the framework will auto set it.
                    //             Alternatively, developers might set this to a CUSTOM value, which we
                    //             need to respect (especially the case-sensitivity).
                    //             REF: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments

                    if (_hostingEnvironment.IsProduction())
                    {
                        options.Environment = Internal.Constants.ProductionEnvironmentSetting;
                    }
                    else if (_hostingEnvironment.IsStaging())
                    {
                        options.Environment = Internal.Constants.StagingEnvironmentSetting;
                    }
                    else if (_hostingEnvironment.IsDevelopment())
                    {
                        options.Environment = Internal.Constants.DevelopmentEnvironmentSetting;
                    }
                    else
                    {
                        // Use the value set by the developer.
                        options.Environment = _hostingEnvironment.EnvironmentName;
                    }
                }
                else
                {
                    options.Environment = _hostingEnvironment.EnvironmentName;
                }
            }

            options.AddLogEntryFilter((category, _, eventId, _)
                // https://github.com/aspnet/KestrelHttpServer/blob/0aff4a0440c2f393c0b98e9046a8e66e30a56cb0/src/Kestrel.Core/Internal/Infrastructure/KestrelTrace.cs#L33
                // 13 = Application unhandled exception, which is captured by the middleware so the LogError of kestrel ends up as a duplicate with less info
                => eventId.Id == 13
                   && string.Equals(
                       category,
                       "Microsoft.AspNetCore.Server.Kestrel",
                       StringComparison.Ordinal));

#if NETSTANDARD2_0
            options.AddDiagnosticSourceIntegration();
#endif
        }
    }
}
