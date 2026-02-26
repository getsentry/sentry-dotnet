using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace Sentry.OpenTelemetry.Exporter.OpenTelemetryProtocol;

/// <summary>
/// OpenTelemetry Extensions for <see cref="SentryOptions"/>.
/// </summary>
public static class SentryOptionsExtensions
{
    /// <summary>
    /// <para>Configures Sentry to use OpenTelemetry for distributed tracing. Sentry instrumented traces will be
    /// disabled (so all tracing instrumentation must be done using the OpenTelemetry <see cref="Activity"/> classes).
    /// </para>
    /// <para>
    /// This is the recommended way to set up Sentry's OpenTelemetry integration.
    /// </para>
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
    public static void UseOtlp(this SentryOptions options, TracerProviderBuilder builder, TextMapPropagator? textMapPropagator = null)
    {
        if (string.IsNullOrWhiteSpace(options.Dsn))
        {
            throw new ArgumentException("Sentry DSN must be set before calling `SentryOptions.UseOTLP`", nameof(options.Dsn));
        }
        builder.AddSentryOtlp(options.Dsn, textMapPropagator);
        options.UseOtlp();
    }

    /// <summary>
    /// <para>Configures Sentry to use OpenTelemetry for distributed tracing. Sentry instrumented traces will be
    /// disabled (so all tracing instrumentation must be done using the OpenTelemetry <see cref="Activity"/> classes).
    /// </para>
    /// <para>
    /// This is the recommended way to set up Sentry's OpenTelemetry integration.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Note: if you are using this overload to configure Sentry to work with OpenTelemetry, you will also have to call
    /// <see cref="TracerProviderBuilderExtensions.AddSentryOtlp"/>, when building your <see cref="TracerProviderBuilder"/>
    /// to ensure OpenTelemetry sends trace information to Sentry.
    /// </remarks>
    /// <param name="options">The <see cref="SentryOptions"/> instance.</param>
    public static void UseOtlp(this SentryOptions options)
    {
        options.Instrumenter = Instrumenter.OpenTelemetry;
        options.DisableSentryTracing = true;
        options.PropagationContextFactory = _ => new OtelPropagationContext();
    }
}
