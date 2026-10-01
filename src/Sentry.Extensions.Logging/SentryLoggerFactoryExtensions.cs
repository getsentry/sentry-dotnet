using Sentry;
using Sentry.Extensibility;
using Sentry.Extensions.Logging;
using Sentry.Infrastructure;

// ReSharper disable once CheckNamespace
// Ensures 'AddSentry' can be found without: 'using Sentry;'
namespace Microsoft.Extensions.Logging;

/// <summary>
/// SentryLoggerFactory extensions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryLoggerFactoryExtensions
{
    /// <summary>
    /// Adds the Sentry logging integration.
    /// </summary>
    /// <remarks>
    /// This method does not initialize Sentry. Initialize it separately, with <see cref="SentrySdk.Init(Action{SentryOptions})"/>
    /// or a framework integration such as <c>UseSentry</c>.
    /// </remarks>
    /// <param name="factory">The factory.</param>
    /// <param name="optionsConfiguration">The options configuration.</param>
    public static ILoggerFactory AddSentry(
        this ILoggerFactory factory,
        Action<SentryLoggingOptions>? optionsConfiguration = null)
    {
        var options = new SentryLoggingOptions();

        optionsConfiguration?.Invoke(options);

        factory.AddProvider(new SentryLoggerProvider(HubAdapter.Instance, SystemClock.Clock, options));
        return factory;
    }
}
