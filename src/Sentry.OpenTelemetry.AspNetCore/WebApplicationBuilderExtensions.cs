using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Sentry.AspNetCore;

namespace Sentry.OpenTelemetry.AspNetCore;

/// <summary>
/// Extensions fo using Sentry for use with OpenTelemetry in AspNetCore applications
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Configures OpenTelemetry so that ASP .NET Core trace information is sent to Sentry.
    /// </summary>
    /// <param name="builder"><see cref="WebApplicationBuilder"/> instance</param>
    /// <param name="tracerProviderBuilder">Function to configure the <see cref="TracerProviderBuilder"/></param>
    /// <param name="sentryOptions">Function to configure the <see cref="SentryOptions"/></param>
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
    /// <returns>An <see cref="OpenTelemetryBuilder"/> for chaining to configure other aspects of OpenTelemetry</returns>
    public static OpenTelemetryBuilder? AddOpenTelemetryWithSentry(
        this  WebApplicationBuilder builder,
        Action<TracerProviderBuilder> tracerProviderBuilder,
        Action<SentryAspNetCoreOptions>? sentryOptions,
        TextMapPropagator? defaultTextMapPropagator = null
    )
    {
        // Ensure OpenTelemetry is added whatever SentryOptions action the SDK user has configured
        void SentryOptionsWithOpenTelemetryInstrumentation(SentryAspNetCoreOptions options)
        {
            sentryOptions?.Invoke(options);
            options.UseOpenTelemetry();
        }
        builder.WebHost.UseSentry(SentryOptionsWithOpenTelemetryInstrumentation);

        // Ensure Sentry is added whatever TracerProviderBuilder action the SDK user has configured
        void TracerProviderBuilderWithSentrySpanProcessor(TracerProviderBuilder b)
        {
            tracerProviderBuilder.Invoke(b);
            b.AddSentry(defaultTextMapPropagator);
        }
        return builder.Services.AddOpenTelemetry().WithTracing(
            TracerProviderBuilderWithSentrySpanProcessor
        );
    }
}
