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
    /// <param name="options">The <see cref="SentryOptions"/> instance.</param>
    /// <param name="builder"><see cref="TracerProviderBuilder"/></param>
    /// <param name="textMapPropagator">
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
    public static void UseOpenTelemetry(this SentryOptions options, TracerProviderBuilder builder, TextMapPropagator? textMapPropagator = null)
    {
        options.UseOpenTelemetry();
        builder.AddSentry(textMapPropagator);
    }

    /// <summary>
    /// <para>Configures Sentry to use OpenTelemetry for distributed tracing.</para>
    /// <para>
    /// Note: if you are using this method to configure Sentry to work with OpenTelemetry, you will also have to call
    /// <see cref="O:TracerProviderBuilderExtensions.AddSentry"/>  when building your <see cref="TracerProviderBuilder"/>
    /// to ensure OpenTelemetry sends trace information to Sentry.
    /// </para>
    /// </summary>
    /// <param name="options">The <see cref="SentryOptions"/> instance.</param>
    public static void UseOpenTelemetry(this SentryOptions options)
    {
        options.Instrumenter = Instrumenter.OpenTelemetry;
        options.PropagationContextFactory = _ => new OtelPropagationContext();
        options.AddTransactionProcessor(
            new OpenTelemetryTransactionProcessor()
        );
    }
}
