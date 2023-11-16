using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

#if NETSTANDARD2_0
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#endif

namespace Sentry.AspNetCore;

/// <summary>
/// Sets up ASP.NET Core option for Sentry.
/// </summary>
public class SentryAspNetCoreOptionsSetup : ConfigureFromConfigurationOptions<SentryAspNetCoreOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="SentryAspNetCoreOptionsSetup"/>.
    /// </summary>
    public SentryAspNetCoreOptionsSetup(
        ILoggerProviderConfiguration<SentryAspNetCoreLoggerProvider> providerConfiguration)
        : base(providerConfiguration.Configuration)
    {
    }

    /// <summary>
    /// Configures the <see cref="SentryAspNetCoreOptions"/>.
    /// </summary>
    public override void Configure(SentryAspNetCoreOptions options)
    {
        base.Configure(options);

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
