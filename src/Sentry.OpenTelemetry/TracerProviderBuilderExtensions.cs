using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace Sentry.OpenTelemetry;

/// <summary>
/// Contains extension methods for the <see cref="TracerProviderBuilder"/> class.
/// </summary>
public static class TracerProviderBuilderExtensions
{
    private const string SentryNotInitializedMessage =
        "No IHub service registered and SentrySdk.CurrentHub is null. Sentry doesn't appear to have been initialized correctly.";

    /// <summary>
    /// Ensures OpenTelemetry trace information is sent to Sentry.
    /// </summary>
    /// <param name="tracerProviderBuilder"><see cref="TracerProviderBuilder"/>.</param>
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
    /// <returns>The supplied <see cref="TracerProviderBuilder"/> for chaining.</returns>
    public static TracerProviderBuilder AddSentry(this TracerProviderBuilder tracerProviderBuilder, TextMapPropagator? defaultTextMapPropagator = null)
    {
        defaultTextMapPropagator ??= new SentryPropagator();
        Sdk.SetDefaultTextMapPropagator(defaultTextMapPropagator);
        return tracerProviderBuilder.AddProcessor(services =>
        {
            List<IOpenTelemetryEnricher> enrichers = new();

            // AspNetCoreEnricher
            var userFactory = services.GetService<ISentryUserFactory>();
            if (userFactory is not null)
            {
                enrichers.Add(new AspNetCoreEnricher(userFactory));
            }

            var hub = services.GetService<IHub>() ?? SentrySdk.CurrentHub;
            if (hub.IsEnabled is not true)
            {
                var logger = services.GetService<ILogger<TracerProviderBuilder>>();
                logger?.LogWarning(SentryNotInitializedMessage);
            }
            return new SentrySpanProcessor(hub, enrichers);
        });
    }
}
