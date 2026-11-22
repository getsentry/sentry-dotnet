using Sentry.Extensions.Logging;

// ReSharper disable once CheckNamespace
// Ensures 'AddSentry' can be found without: 'using Sentry;'
namespace Microsoft.Extensions.Logging;

/// <summary>
/// SentryLoggerFactory extensions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryLoggerFactoryExtensions
{
    internal const string ObsoleteLoggerFactoryOverload =
        "ILoggerFactory.AddSentry() is no longer supported. Add Sentry while building the logger factory instead, " +
        "with LoggerFactory.Create(builder => builder.AddSentry()) or ILoggingBuilder.AddSentry(), which also " +
        "sends structured logs.";

    /// <summary>
    /// Not supported. Use <see cref="LoggingBuilderExtensions.AddSentry(ILoggingBuilder, Action{SentryLoggingOptions}?)"/> instead.
    /// </summary>
    /// <param name="factory">The factory.</param>
    /// <param name="optionsConfiguration">No longer supported.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    [Obsolete(ObsoleteLoggerFactoryOverload, error: true)]
    public static ILoggerFactory AddSentry(
        this ILoggerFactory factory,
        Action<SentryLoggingOptions>? optionsConfiguration = null)
        => throw new NotSupportedException(ObsoleteLoggerFactoryOverload);
}
