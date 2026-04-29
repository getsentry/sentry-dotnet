using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.OpenTelemetry.Exporter;

/// <summary>
/// When using OpenTelemetry the TraceId etc come from the current Activity (which can be null).
/// </summary>
internal class OtelPropagationContext : IExternalPropagationContext
{
    /// <summary>
    /// We cache the DSC on the current activity so we don't have to regenerate this every time we use it
    /// </summary>
    public DynamicSamplingContext? DynamicSamplingContext
    {
        get => Activity.Current?.GetFused<DynamicSamplingContext>();
        private set => Activity.Current?.SetFused(value);
    }
    public SentryId? TraceId => Activity.Current?.TraceId.AsSentryId();
    public SpanId? SpanId => Activity.Current?.SpanId.AsSentrySpanId();
    public SpanId? ParentSpanId
    {
        get
        {
            var activity = Activity.Current;
            if (activity is null)
            {
                return null;
            }
            var parentSpanId = activity.ParentSpanId;
            return parentSpanId == default ? null : parentSpanId.AsSentrySpanId();
        }
    }

    public bool IsSampled => Activity.Current?.Recorded ?? false;

    /// <summary>
    /// th is a rejection threshold: T = (1 - sampling_probability) * 2^56, so we invert to get the sample rate.
    /// </summary>
    public double? SampleRate => GetOtelTraceStateValue("th") is { } th && ParseOtelHexFraction(th) is { } v ? 1.0 - v : null;

    /// <summary>
    /// Parses the SampleRand from the rv (random value) OTEL equivalent from the TraceStateString
    /// </summary>
    public double? SampleRand =>
        // OTel keeps a trace when rv ≥ th; Sentry keeps it when sample_rand < sample_rate.
        // Mapping sample_rand = 1 − rv makes the decisions equivalent: 1 − rv < 1 − th ↔ rv > th
        // Guard: rv=0 would produce 1.0, which is out of range for sample_rand — return null instead.
        GetOtelTraceStateValue("rv") is { } rv && ParseOtelHexFraction(rv) is { } v && v > 0.0 ? 1.0 - v : null;

    /// <summary>
    /// <para>
    /// Parses a sub-key value from the OTel vendor entry in the W3C tracestate string.
    /// The tracestate format is comma-separated vendor entries (e.g. "ot=th:8;rv:a0b1c2d3e4f5a0,other=x").
    /// The OTel entry uses semicolon-separated sub-keys with colon-delimited values (e.g. "th:8;rv:...").
    /// </para>
    /// <para>
    /// See https://opentelemetry.io/docs/specs/otel/trace/tracestate-handling/
    /// </para>
    /// </summary>
    private static string? GetOtelTraceStateValue(string subKey)
    {
        var traceState = Activity.Current?.TraceStateString;
        if (string.IsNullOrEmpty(traceState))
            return null;

        foreach (var entry in traceState.Split(','))
        {
            var trimmed = entry.Trim();
            if (!trimmed.StartsWith("ot=", StringComparison.Ordinal))
                continue;

            foreach (var subEntry in trimmed.Substring(3).Split(';'))
            {
                var colonIdx = subEntry.IndexOf(':');
                if (colonIdx < 0)
                    continue;
                if (subEntry.Substring(0, colonIdx) == subKey)
                    return subEntry.Substring(colonIdx + 1);
            }
            break; // found "ot" entry but sub-key was absent
        }

        return null;
    }

    /// <summary>
    /// Converts an OTel 56-bit hex fraction to a double in [0, 1).
    /// The value is encoded as up to 14 lowercase hex digits with trailing zeros omitted, so "8" means 0.5.
    /// </summary>
    private static double? ParseOtelHexFraction(string hexValue)
    {
        if (hexValue.Length == 0 || hexValue.Length > 14)
            return null;

        // Restore trailing zeros so we always have a 56-bit (14 hex digit) number, then divide by 2^56
        if (!ulong.TryParse(hexValue.PadRight(14, '0'), NumberStyles.HexNumber, null, out var raw))
            return null;

        return raw / (double)(1UL << 56);
    }

    public BaggageHeader GetBaggageHeader()
    {
        var items = new Dictionary<string, string>();
        if (Activity.Current?.Baggage is { } baggage)
        {
            foreach (var item in baggage)
            {
                items[item.Key] = item.Value ?? string.Empty;
            }
        }
        return BaggageHeader.Create(items);
    }

    public DynamicSamplingContext? GetDynamicSamplingContext(SentryOptions options, IReplaySession replaySession)
    {
        if (DynamicSamplingContext is null)
        {
            options.LogDebug("Creating Dynamic Sampling Context from the External Propagation Context.");
            DynamicSamplingContext = this.CreateDynamicSamplingContext(options, replaySession);
        }

        return DynamicSamplingContext;
    }
}
