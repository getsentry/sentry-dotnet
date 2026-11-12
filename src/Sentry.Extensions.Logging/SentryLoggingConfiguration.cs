using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging;

internal static class SentryLoggingConfiguration
{
    private static readonly string[] LoggingKeys =
    [
        "LogLevel",
        nameof(SentryLoggingOptions.MinimumEventLevel),
        nameof(SentryLoggingOptions.MinimumBreadcrumbLevel),
    ];

    private static readonly string[] SdkInitializationKeys = ["Dsn", "InitializeSdk"];

    internal static void ApplyTo(IConfiguration section, SentryLoggingOptions options)
    {
        RejectSdkSettings(section);

        if (section[nameof(options.MinimumEventLevel)] is { } eventLevel
            && Enum.TryParse<LogLevel>(eventLevel, ignoreCase: true, out var minimumEventLevel))
        {
            options.MinimumEventLevel = minimumEventLevel;
        }

        if (section[nameof(options.MinimumBreadcrumbLevel)] is { } breadcrumbLevel
            && Enum.TryParse<LogLevel>(breadcrumbLevel, ignoreCase: true, out var minimumBreadcrumbLevel))
        {
            options.MinimumBreadcrumbLevel = minimumBreadcrumbLevel;
        }
    }

    private static void RejectSdkSettings(IConfiguration section)
    {
        if (section["Dsn"] is not null
            || (bool.TryParse(section["InitializeSdk"], out var initializeSdk) && initializeSdk))
        {
            throw new NotSupportedException(SentryLoggingOptions.ObsoleteSdkInitialization);
        }

        var sdkSettings = section.GetChildren()
            .Select(child => child.Key)
            .Where(key => !LoggingKeys.Contains(key, StringComparer.OrdinalIgnoreCase)
                          && !SdkInitializationKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (sdkSettings.Length > 0)
        {
            throw new NotSupportedException(
                "Sentry's logging configuration section no longer configures the SDK, so these settings have no " +
                $"effect there: {string.Join(", ", sdkSettings)}. Configure them where Sentry is initialized " +
                "instead: the 'Sentry' configuration section when using an integration such as UseSentry, or the " +
                "options passed to SentrySdk.Init.");
        }
    }
}
