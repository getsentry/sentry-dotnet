using System.ComponentModel;
using Sentry.EntityFramework;
using Sentry.EntityFramework.ErrorProcessors;
using Sentry.Extensibility;

// ReSharper disable once CheckNamespace - Make it visible without: using Sentry.EntityFramework
namespace Sentry;

/// <summary>
/// Extension methods to <see cref="SentryOptions"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryOptionsExtensions
{
    private static DbInterceptionIntegration? DbIntegration { get; set; }

    /// <summary>
    /// Adds the entity framework integration.
    /// </summary>
    /// <param name="sentryOptions">The sentry options.</param>
    /// <returns></returns>
    public static SentryOptions AddEntityFramework(this SentryOptions sentryOptions)
    {
        try
        {
#pragma warning disable 618 // TODO: We can make the method internal on a new major release.
            _ = SentryDatabaseLogging.UseBreadcrumbs(diagnosticLogger: sentryOptions.DiagnosticLogger);
#pragma warning restore 618
        }
        catch (Exception e)
        {
            sentryOptions.DiagnosticLogger?
                .LogError("Failed to configure EF breadcrumbs. Make sure to init Sentry before EF.", e);
        }

        DbIntegration = new DbInterceptionIntegration();
        sentryOptions.AddIntegration(DbIntegration);

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
        => DbIntegration?.Unregister();
}
