using OpenTelemetry;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Internal.Tracing;

namespace Sentry.OpenTelemetry;

/// <summary>
/// Sentry span processor for Open Telemetry.
/// </summary>
public class SentrySpanProcessor : BaseProcessor<Activity>
{
    internal readonly IEnumerable<IOpenTelemetryEnricher> _enrichers;

    private readonly ActivitySpanProcessor _activitySpanProcessor;
    private readonly IHub _hub;
    private readonly SentryOptions? _options;

    /// <summary>
    /// Constructs a <see cref="SentrySpanProcessor"/>.
    /// </summary>
    public SentrySpanProcessor() : this(SentrySdk.CurrentHub)
    {
    }

    /// <summary>
    /// Constructs a <see cref="SentrySpanProcessor"/>.
    /// </summary>
    public SentrySpanProcessor(IHub hub) : this(hub, null)
    {
    }

    internal SentrySpanProcessor(IHub hub, IEnumerable<IOpenTelemetryEnricher>? enrichers)
    {
        _hub = hub;
        if (_hub is DisabledHub)
        {
            // This would only happen if someone tried to create a SentrySpanProcessor manually
            throw new InvalidOperationException(
                "Attempted to creates a SentrySpanProcessor for a Disabled hub. " +
                "You should use the TracerProviderBuilderExtensions to configure Sentry with OpenTelemetry");
        }

        _options = hub.GetSentryOptions();
        if (_options is null)
        {
            throw new InvalidOperationException(
                "The Sentry SDK has not been initialised. To use Sentry with OpenTelemetry " +
                "tracing you need to initialize the Sentry SDK.");
        }

        if (_options.Instrumenter != Instrumenter.OpenTelemetry)
        {
            throw new InvalidOperationException(
                "OpenTelemetry has not been configured on the Sentry SDK. To use OpenTelemetry tracing you need " +
                "to initialize the Sentry SDK with options.UseOpenTelemetry()");
        }

        _enrichers = enrichers ?? Enumerable.Empty<IOpenTelemetryEnricher>();
        _activitySpanProcessor = new ActivitySpanProcessor(hub, ApplyEnrichers, GetResourceAttributes);
    }

    internal ISpan? GetMappedSpan(ActivitySpanId spanId) => _activitySpanProcessor.GetMappedSpan(spanId);

    /// <inheritdoc />
    public override void OnStart(Activity data)
    {
        _activitySpanProcessor.OnStart(data);
    }

    /// <inheritdoc />
    public override void OnEnd(Activity data)
    {
        _activitySpanProcessor.OnEnd(data);
    }

    private void ApplyEnrichers(ISpan span, Activity data)
    {
        foreach (var enricher in _enrichers)
        {
            enricher.Enrich(span, data, _hub, _options);
        }
    }

    private Dictionary<string, object> GetResourceAttributes()
    {
        return ParentProvider?.GetResource().Attributes.ToDict() ?? new Dictionary<string, object>(0);
    }
}
