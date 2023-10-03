using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Sentry.Internal;
using Sentry.Internal.ScopeStack;

namespace Sentry.OpenTelemetry;

/// <summary>
/// Contains extension methods for the <see cref="TracerProviderBuilder"/> class.
/// </summary>
public static class TracerProviderBuilderExtensions
{
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

            return new SentrySpanProcessor(SentrySdk.CurrentHub, enrichers);
        });
    }

    /// <summary>
    /// Ensures Sentry <see cref="Scope"/> data is applied to any OpenTelemetry events that get sent to Sentry. This
    /// should be called inside the `AddAspNetCoreInstrumentation` callback on the <see cref="TracerProviderBuilder"/>
    /// when initializing OpenTelemetry.
    /// </summary>
    /// <param name="activity">The activity</param>
    /// <param name="request"></param>
    /// <typeparam name="T"></typeparam>
    public static void ApplySentryScope<T>(this Activity activity, T request)
        where T: class
    {
        activity.SetScopeStackKey(request);
    }
}
