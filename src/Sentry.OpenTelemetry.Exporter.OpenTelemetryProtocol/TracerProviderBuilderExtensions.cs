using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;
using Sentry;
using Sentry.OpenTelemetry;

// ReSharper disable once CheckNamespace -- Discoverability
namespace OpenTelemetry.Trace;

/// <summary>
/// Contains extension methods for the <see cref="TracerProviderBuilder"/> class.
/// </summary>
public static class TracerProviderBuilderExtensions
{
    internal const string MissingDsnWarning = "Invalid DSN passed to AddSentryOtlp";

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
    /// <param name="defaultTextMapPropagator">
    ///     <para>The default TextMapPropagator to be used by OpenTelemetry.</para>
    ///     <para>
    ///         If this parameter is not supplied, the <see cref="SentryPropagator"/> will be used, which propagates the
    ///         baggage header as well as Sentry trace headers.
    ///     </para>
    ///     <para>
    ///         The <see cref="SentryPropagator"/> is required for Sentry's OpenTelemetry integration to work, but you
    ///         could wrap this in a <see cref="CompositeTextMapPropagator"/> if you needed other propagators as well.
    ///     </para>
    /// </param>
    /// <returns>The supplied <see cref="TracerProviderBuilder"/> for chaining.</returns>
    public static TracerProviderBuilder AddSentryOtlpExporter(this TracerProviderBuilder tracerProviderBuilder, string dsnString,
        TextMapPropagator? defaultTextMapPropagator = null)
    {
        if (Dsn.TryParse(dsnString) is not { } dsn)
        {
            throw new ArgumentException(MissingDsnWarning, nameof(dsnString));
        }

        defaultTextMapPropagator ??= new SentryPropagator();
        Sdk.SetDefaultTextMapPropagator(defaultTextMapPropagator);

        tracerProviderBuilder.AddOtlpExporter(options => OtlpConfigurationCallback(options, dsn));
        return tracerProviderBuilder;
    }

    // Internal helper method for testing purposes
    internal static void OtlpConfigurationCallback(OtlpExporterOptions options, Dsn dsn)
    {
        options.Endpoint = dsn.GetOtlpTracesEndpointUri();
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.HttpClientFactory = () =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Sentry-Auth", $"sentry sentry_key={dsn.PublicKey}");
            return client;
        };
    }
}
