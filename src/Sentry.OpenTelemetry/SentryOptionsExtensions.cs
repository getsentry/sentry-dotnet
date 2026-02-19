using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace Sentry.OpenTelemetry;

/// <summary>
/// OpenTelemetry Extensions for <see cref="SentryOptions"/>.
/// </summary>
public static class SentryOptionsExtensions
{
    /// <summary>
    /// Enables OpenTelemetry instrumentation with Sentry
    /// </summary>
    /// <param name="options"><see cref="SentryOptions"/> instance</param>
    /// <param name="traceProviderBuilder"><see cref="TracerProviderBuilder"/></param>
    /// <param name="defaultTextMapPropagator">
    ///     <para>The default TextMapPropagator to be used by OpenTelemetry.</para>
    ///     <para>
    ///         If this parameter is not supplied, the <see cref="SentryPropagator"/> will be used, which propagates the
    ///         baggage header as well as Sentry trace headers.
    ///     </para>
    ///     <para>
    ///         The <see cref="SentryPropagator"/> is required for Sentry's OpenTelemetry integration to work but you
    ///         could wrap this in a <see cref="CompositeTextMapPropagator"/> if you needed other propagators as well.
    ///     </para>
    /// </param>
    /// <param name="disableSentryTracing">Whether to disable traces created using Sentry's tracing instrumentation.
    /// It's recommended that you set this to <c>true</c> since mixing OpenTelemetry and Sentry traces may yield
    /// unexpected results. It is <c>false</c> by default for backward compatibility only.
    /// </param>
    /// <remarks>
    /// This method of initialising the Sentry OpenTelemetry integration will be depricated in a future major release.
    /// We recommend you use the Sentry.OpenTelemetry.Exporter.OpenTelemetryProtocol integration instead.
    /// </remarks>
    public static void UseOpenTelemetry(this SentryOptions options, TracerProviderBuilder traceProviderBuilder,
        TextMapPropagator? defaultTextMapPropagator = null, bool disableSentryTracing = false)
    {
        options.UseOpenTelemetry(disableSentryTracing);
        traceProviderBuilder.AddSentry(defaultTextMapPropagator);
    }

    /// <summary>
    /// <para>Configures Sentry to use OpenTelemetry for distributed tracing.</para>
    /// <para>
    /// Note: if you are using this method to configure Sentry to work with OpenTelemetry you will also have to call
    /// <see cref="TracerProviderBuilderExtensions.AddSentry"/> when building your <see cref="TracerProviderBuilder"/>
    /// to ensure OpenTelemetry sends trace information to Sentry.
    /// </para>
    /// </summary>
    /// <param name="options">The <see cref="SentryOptions"/> instance.</param>
    /// <param name="disableSentryTracing">Whether to disable traces created using Sentry's tracing instrumentation.
    /// It's recommended that you set this to <c>true</c> since mixing OpenTelemetry and Sentry traces may yield
    /// unexpected results. It is <c>false</c> by default for backward compatibility only.
    /// </param>
    /// <remarks>
    /// This method of initialising the Sentry OpenTelemetry integration will be depricated in a future major release.
    /// We recommend you use the Sentry.OpenTelemetry.Exporter.OpenTelemetryProtocol integration instead.
    /// </remarks>
    public static void UseOpenTelemetry(this SentryOptions options, bool disableSentryTracing = false)
    {
        options.Instrumenter = Instrumenter.OpenTelemetry;
        options.DisableSentryTracing = disableSentryTracing;
        options.AddTransactionProcessor(
            new OpenTelemetryTransactionProcessor()
        );
    }
}
