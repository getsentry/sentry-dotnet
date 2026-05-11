using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry.Exporter;

namespace Sentry;

/// <summary>
/// OpenTelemetry Extensions for <see cref="SentryOptions"/>.
/// </summary>
public static class SentryOptionsExtensions
{
    /// <summary>
    /// <para>Configures Sentry to use OpenTelemetry for distributed tracing. Sentry-instrumented traces will be
    /// disabled (so all tracing instrumentation must be done using the OpenTelemetry <see cref="Activity"/> classes).
    /// </para>
    /// <para>
    /// This is the recommended way to set up Sentry's OpenTelemetry integration.
    /// </para>
    /// </summary>
    /// <param name="options">The <see cref="SentryOptions"/> instance.</param>
    /// <param name="tracerProviderBuilder"><see cref="TracerProviderBuilder"/></param>
    /// <param name="collectorUrl">A custom endpoint to export OLTP trace information to. If no url is provided, the
    /// endpoint will be inferred automatically from the DSN.</param>
    /// <param name="defaultTextMapPropagator">
    ///     <para>The default TextMapPropagator to be used by OpenTelemetry.</para>
    ///     <para>
    ///         If this parameter is not supplied, a <see cref="CompositeTextMapPropagator"/> containing both
    ///         W3C <c>traceparent</c>/<c>tracestate</c> and
    ///         <see cref="OpenTelemetry.SentryPropagator"/> (<c>sentry-trace</c> and <c>baggage</c>) will be used.
    ///         This allows Sentry to interoperate with services that use W3C trace context headers.
    ///     </para>
    ///     <para>
    ///         The <see cref="OpenTelemetry.SentryPropagator"/> is required for Sentry's OpenTelemetry integration to work. Supply
    ///         a custom propagator only if you need to replace the defaults entirely.
    ///     </para>
    /// </param>
    public static void UseOtlp(this SentryOptions options, TracerProviderBuilder tracerProviderBuilder, Uri? collectorUrl = null, TextMapPropagator? defaultTextMapPropagator = null)
    {
        if (string.IsNullOrWhiteSpace(options.Dsn))
        {
            throw new ArgumentException("Sentry DSN must be set before calling `SentryOptions.UseOtlp`", nameof(options.Dsn));
        }
        tracerProviderBuilder.AddSentryOtlpExporter(options.Dsn, collectorUrl, defaultTextMapPropagator);
        options.UseOtlp();
    }

    /// <summary>
    /// <para>Configures Sentry to use OpenTelemetry for distributed tracing. Sentry-instrumented traces will be
    /// disabled (so all tracing instrumentation must be done using the OpenTelemetry <see cref="Activity"/> classes).
    /// </para>
    /// <para>
    /// This is the recommended way to set up Sentry's OpenTelemetry integration.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Note: if you are using this overload to configure Sentry to work with OpenTelemetry, you will also have to call
    /// <see cref="SentryTracerProviderBuilderExtensions.AddSentryOtlpExporter"/>, when building your <see cref="TracerProviderBuilder"/>
    /// to ensure OpenTelemetry sends trace information to Sentry.
    /// </remarks>
    /// <param name="options">The <see cref="SentryOptions"/> instance.</param>
    public static void UseOtlp(this SentryOptions options)
    {
        options.Instrumenter = Instrumenter.OpenTelemetry;
        options.DisableSentryTracing = true;
        options.ExternalPropagationContext = new OtelPropagationContext();
    }
}
