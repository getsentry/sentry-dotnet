using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace Sentry.OpenTelemetry;

/// <summary>
/// OpenTelemetry Extensions for <see cref="SentryOptions"/>.
/// </summary>
public static class SentryOptionsExtensions
{
    /// <summary>
    /// Configures Sentry to use OpenTelemetry for distributed tracing.
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
    /// <param name="disableSentryTracing">Whether to disable traces created using Sentry's tracing instrumentation.
    /// It's recommended that you set this to <c>true</c> since mixing OpenTelemetry and Sentry traces may yield
    /// unexpected results. It is <c>false</c> by default for backward compatibility only.
    /// </param>
    /// <remarks>
    /// This method of initialising the Sentry OpenTelemetry integration will be depricated in a future major release.
    /// We recommend you use <see cref="UseOTLP(SentryOptions, TracerProviderBuilder, TextMapPropagator?)"/> instead.
    /// </remarks>
    public static void UseOpenTelemetry(this SentryOptions options, TracerProviderBuilder builder,
        TextMapPropagator? textMapPropagator = null, bool disableSentryTracing = false)
    {
        options.UseOpenTelemetry(disableSentryTracing);
        builder.AddSentry(textMapPropagator);
    }

    /// <summary>
    /// <para>Configures Sentry to use OpenTelemetry for distributed tracing.
    /// </para>
    /// <para>
    /// Note: if you are using this overload to configure Sentry to work with OpenTelemetry, you will also have to call
    /// <see cref="O:TracerProviderBuilderExtensions.AddSentry"/>  when building your <see cref="TracerProviderBuilder"/>
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
    /// We recommend you use <see cref="UseOTLP(SentryOptions, TracerProviderBuilder, TextMapPropagator?)"/> instead.
    /// </remarks>
    public static void UseOpenTelemetry(this SentryOptions options, bool disableSentryTracing = false)
    {
        options.Instrumenter = Instrumenter.OpenTelemetry;
        options.DisableSentryTracing = disableSentryTracing;
        options.PropagationContextFactory = _ => new OtelPropagationContext();
        options.AddTransactionProcessor(
            new OpenTelemetryTransactionProcessor()
        );
    }

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
    public static void UseOTLP(this SentryOptions options, TracerProviderBuilder builder, TextMapPropagator? textMapPropagator = null)
    {
        if (string.IsNullOrWhiteSpace(options.Dsn))
        {
            throw new ArgumentException("Sentry DSN must be set before calling `SentryOptions.UseOTLP`", nameof(options.Dsn));
        }
        builder.AddSentryOTLP(options.Dsn, textMapPropagator);
        options.UseOTLP();
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
    /// <see cref="TracerProviderBuilderExtensions.AddSentryOTLP"/>, when building your <see cref="TracerProviderBuilder"/>
    /// to ensure OpenTelemetry sends trace information to Sentry.
    /// </remarks>
    /// <param name="options">The <see cref="SentryOptions"/> instance.</param>
    public static void UseOTLP(this SentryOptions options)
    {
        options.Instrumenter = Instrumenter.OpenTelemetry;
        options.DisableSentryTracing = true;
        options.PropagationContextFactory = _ => new OtelPropagationContext();
    }
}
