using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore;

/// <summary>
/// Sets up ASP.NET Core option for Sentry.
/// </summary>
#if NETSTANDARD2_0
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
        options.AddDiagnosticSourceIntegration();
        options.DeduplicateUnhandledException();
    }
}

#else
public class SentryAspNetCoreOptionsSetup : IConfigureOptions<SentryAspNetCoreOptions>
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Creates a new instance of <see cref="SentryAspNetCoreOptionsSetup"/>.
    /// </summary>
    public SentryAspNetCoreOptionsSetup(ILoggerProviderConfiguration<SentryAspNetCoreLoggerProvider> providerConfiguration)
        : this(providerConfiguration.Configuration)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="SentryAspNetCoreOptionsSetup"/>.
    /// </summary>
    internal SentryAspNetCoreOptionsSetup(IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    }

    /// <summary>
    /// Configures the <see cref="SentryAspNetCoreOptions"/>.
    /// </summary>
    [RequiresDynamicCode()]
    [RequiresUnreferencedCode()]
    public void Configure(SentryAspNetCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var bindable = new BindableSentryAspNetCoreOptions();
        _config.Bind(bindable);
        bindable.ApplyTo(options);

        options.DeduplicateUnhandledException();
    }
}
#endif

internal static class SentryAspNetCoreOptionsExtensions
{
    internal static void DeduplicateUnhandledException(this SentryAspNetCoreOptions options)
    {
        options.AddLogEntryFilter((category, _, eventId, _)
            // https://github.com/aspnet/KestrelHttpServer/blob/0aff4a0440c2f393c0b98e9046a8e66e30a56cb0/src/Kestrel.Core/Internal/Infrastructure/KestrelTrace.cs#L33
            // 13 = Application unhandled exception, which is captured by the middleware so the LogError of kestrel ends up as a duplicate with less info
            => eventId.Id == 13
               && string.Equals(
                   category,
                   "Microsoft.AspNetCore.Server.Kestrel",
                   StringComparison.Ordinal));
    }
}
