using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;
using Sentry;
using Sentry.OpenTelemetry;

// ReSharper disable once CheckNamespace -- Discoverability
namespace OpenTelemetry.Trace;

/// <summary>
/// Contains extension methods for the <see cref="TracerProviderBuilder"/> class.
/// </summary>
public static class SentryTracerProviderBuilderExtensions
{
    internal const string MissingDsnWarning = $"Invalid DSN passed to {nameof(AddSentryOtlpExporter)}.";

    /// <summary>
    /// <para>
    /// Ensures OpenTelemetry trace information is sent to the Sentry OTLP endpoint.
    /// </para>
    /// <para>
    /// Note that if you use this method to configure the trace builder, you will also need to call
    /// <see cref="SentryOptionsExtensions.UseOtlp(Sentry.SentryOptions)"/> when initialising Sentry, for Sentry to work
    /// properly with OpenTelemetry.
    /// </para>
    /// </summary>
    /// <param name="tracerProviderBuilder">The <see cref="TracerProviderBuilder"/>.</param>
    /// <param name="dsnString">The DSN for your Sentry project</param>
    /// <param name="collectorUrl">A custom endpoint to export OLTP trace information to. If no url is provided, the
    /// endpoint will be inferred automatically from the DSN.</param>
    /// <param name="defaultTextMapPropagator">
    ///     <para>The default TextMapPropagator to be used by OpenTelemetry.</para>
    ///     <para>
    ///         If this parameter is not supplied, a <see cref="CompositeTextMapPropagator"/> containing both
    ///         <see cref="TraceContextPropagator"/> (W3C <c>traceparent</c>/<c>tracestate</c>) and
    ///         <see cref="SentryPropagator"/> (<c>sentry-trace</c> and <c>baggage</c>) will be used.
    ///         This allows Sentry to interoperate with services that use W3C trace context headers.
    ///     </para>
    ///     <para>
    ///         The <see cref="SentryPropagator"/> is required for Sentry's OpenTelemetry integration to work. Supply
    ///         a custom propagator only if you need to replace the defaults entirely.
    ///     </para>
    /// </param>
    /// <returns>The supplied <see cref="TracerProviderBuilder"/> for chaining.</returns>
    public static TracerProviderBuilder AddSentryOtlpExporter(this TracerProviderBuilder tracerProviderBuilder,
        string dsnString, Uri? collectorUrl = null, TextMapPropagator? defaultTextMapPropagator = null)
    {
        if (Dsn.TryParse(dsnString) is not { } dsn)
        {
            throw new ArgumentException(MissingDsnWarning, nameof(dsnString));
        }

        defaultTextMapPropagator ??= new CompositeTextMapPropagator(new TextMapPropagator[]
        {
            new TraceContextPropagator(),
            new SentryPropagator(),
        });
        Sdk.SetDefaultTextMapPropagator(defaultTextMapPropagator);

        collectorUrl ??= dsn.GetOtlpTracesEndpointUri();
        tracerProviderBuilder.AddOtlpExporter(options => OtlpConfigurationCallback(options, collectorUrl, dsn.PublicKey));
        return tracerProviderBuilder;
    }

    // Internal helper method for testing purposes
    internal static void OtlpConfigurationCallback(OtlpExporterOptions options, Uri collectorUrl, string publicKey)
    {
        options.Endpoint = collectorUrl;
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.HttpClientFactory = () =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Sentry-Auth",
                $"Sentry sentry_version={SentryConstants.ProtocolVersion}," +
                $"sentry_client={SdkVersion.Instance.Name}/{SdkVersion.Instance.Version}," +
                $"sentry_key={publicKey}");
            return client;
        };
    }
}
