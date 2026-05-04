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
    public double? SampleRate
    {
        get
        {
            var th = GetOtelTraceStateValue("th");
            return !th.IsEmpty && ParseOtelHexFraction(th) is { } v ? 1.0 - v : null;
        }
    }

    /// <summary>
    /// Parses the SampleRand from the rv (random value) OTEL equivalent from the TraceStateString
    /// </summary>
    public double? SampleRand
    {
        get
        {
            // OTel keeps a trace when rv ≥ th; Sentry keeps it when sample_rand < sample_rate.
            // Mapping sample_rand = 1 − rv makes the decisions equivalent: 1 − rv < 1 − th ↔ rv > th
            // Guard: rv=0 would produce 1.0, which is out of range for sample_rand — return null instead.
            var rv = GetOtelTraceStateValue("rv");
            return !rv.IsEmpty && ParseOtelHexFraction(rv) is { } v && v > 0.0 ? 1.0 - v : null;
        }
    }

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
    private static ReadOnlySpan<char> GetOtelTraceStateValue(ReadOnlySpan<char> subKey)
    {
        var traceState = Activity.Current?.TraceStateString;
        if (string.IsNullOrEmpty(traceState))
            return default;

        var remaining = traceState.AsSpan();
        while (!remaining.IsEmpty)
        {
            int commaIdx = remaining.IndexOf(',');
            var entry = (commaIdx >= 0 ? remaining[..commaIdx] : remaining).Trim();
            remaining = commaIdx >= 0 ? remaining[(commaIdx + 1)..] : default;

            if (!entry.StartsWith("ot=", StringComparison.Ordinal))
                continue;

            var otValue = entry[3..]; // skip "ot="
            while (!otValue.IsEmpty)
            {
                int semiIdx = otValue.IndexOf(';');
                var subEntry = semiIdx >= 0 ? otValue[..semiIdx] : otValue;
                otValue = semiIdx >= 0 ? otValue[(semiIdx + 1)..] : default;

                int colonIdx = subEntry.IndexOf(':');
                if (colonIdx < 0)
                    continue;
                if (subEntry[..colonIdx].Equals(subKey, StringComparison.Ordinal))
                    return subEntry[(colonIdx + 1)..];
            }
            break; // found "ot" entry but sub-key was absent
        }

        return default;
    }

    /// <summary>
    /// Converts an OTel 56-bit hex fraction to a double in [0, 1).
    /// The value is encoded as up to 14 lowercase hex digits with trailing zeros omitted, so "8" means 0.5.
    /// </summary>
    private static double? ParseOtelHexFraction(ReadOnlySpan<char> hexValue)
    {
        if (hexValue.IsEmpty || hexValue.Length > 14)
            return null;

#if NETSTANDARD2_0 || NET462
        if (!ulong.TryParse(hexValue.ToString(), NumberStyles.HexNumber, null, out var raw))
#else
        if (!ulong.TryParse(hexValue, NumberStyles.HexNumber, null, out var raw))
#endif
            return null;

        // Shift left to fill the full 56 bits (trailing zeros omitted in the encoding), then divide by 2^56
        raw <<= (14 - hexValue.Length) * 4;
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
