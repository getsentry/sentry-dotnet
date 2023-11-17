// ReSharper disable once CheckNamespace - Make it visible without: using Sentry.EntityFramework
namespace Sentry;

/// <summary>
/// Extension methods to <see cref="SentryOptions"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryOptionsExtensions
{
    private static DbInterceptionIntegration? dbIntegration;

    /// <summary>
    /// Adds the entity framework integration.
    /// </summary>
    /// <param name="sentryOptions">The sentry options.</param>
    public static SentryOptions AddEntityFramework(this SentryOptions sentryOptions)
    {
        if (sentryOptions.ExceptionProcessors.Any(processor => processor.Type == typeof(DbEntityValidationExceptionProcessor)))
        {
            sentryOptions.LogWarning($"{nameof(AddEntityFramework)} has already been called. Subsequent call will be ignored.");
            return sentryOptions;
        }

        try
        {
            _ = SentryDatabaseLogging.UseBreadcrumbs(diagnosticLogger: sentryOptions.DiagnosticLogger);
        }
        catch (Exception e)
        {
            sentryOptions.DiagnosticLogger?
                .LogError(e, "Failed to configure EF breadcrumbs. Make sure to init Sentry before EF.");
        }

        dbIntegration = new DbInterceptionIntegration();
        sentryOptions.AddIntegration(dbIntegration);

        sentryOptions.AddExceptionProcessor(new DbEntityValidationExceptionProcessor());
        // DbConcurrencyExceptionProcessor is untested due to problems with testing it, so it might not be production ready
        //sentryOptions.AddExceptionProcessor(new DbConcurrencyExceptionProcessor());
        return sentryOptions;
    }

    /// <summary>
    /// Disables the integrations with DbInterception.
    /// </summary>
    /// <param name="options">The SentryOptions to remove the integration from.</param>
    public static void DisableDbInterceptionIntegration(this SentryOptions options)
        => dbIntegration?.Unregister();
}
