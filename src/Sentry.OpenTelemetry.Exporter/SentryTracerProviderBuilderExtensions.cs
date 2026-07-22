using OpenTelemetry.Exporter;
using Sentry;

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
    /// <returns>The supplied <see cref="TracerProviderBuilder"/> for chaining.</returns>
    /// <remarks>
    /// This method does not configure an OpenTelemetry propagator.
    /// Cross-service trace propagation should be enabled via the OpenTelemetry SDK (e.g. by calling
    /// <c>Sdk.SetDefaultTextMapPropagator</c>).
    /// </remarks>
    public static TracerProviderBuilder AddSentryOtlpExporter(this TracerProviderBuilder tracerProviderBuilder,
        string dsnString, Uri? collectorUrl = null)
    {
        if (Dsn.IsDisabled(dsnString))
        {
            return tracerProviderBuilder;
        }

        if (Dsn.TryParse(dsnString) is not { } dsn)
        {
            throw new ArgumentException(MissingDsnWarning, nameof(dsnString));
        }

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
