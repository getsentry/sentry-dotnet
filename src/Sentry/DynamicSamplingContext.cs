using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Provides the Dynamic Sampling Context for a transaction.
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/performance/dynamic-sampling-context"/>
internal class DynamicSamplingContext
{
    public IReadOnlyDictionary<string, string> Items { get; }

    public bool IsEmpty => Items.Count == 0;

    private DynamicSamplingContext(IReadOnlyDictionary<string, string> items) => Items = items;

    /// <summary>
    /// Gets an empty <see cref="DynamicSamplingContext"/> that can be used to "freeze" the DSC on a transaction.
    /// </summary>
    public static readonly DynamicSamplingContext Empty = new(new Dictionary<string, string>().AsReadOnly());

    private DynamicSamplingContext(
        SentryId traceId,
        string publicKey,
        bool? sampled,
        double? sampleRate = null,
        string? release = null,
        string? environment = null,
        string? transactionName = null)
    {
        // Validate and set required values
        if (traceId == SentryId.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(traceId));
        }

        if (string.IsNullOrWhiteSpace(publicKey))
        {
            throw new ArgumentException(default, nameof(publicKey));
        }

        if (sampleRate is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        var items = new Dictionary<string, string>(capacity: 7)
        {
            ["trace_id"] = traceId.ToString(),
            ["public_key"] = publicKey,
        };

        // Set optional values
        if (sampled.HasValue)
        {
            items.Add("sampled", sampled.Value ? "true" : "false");
        }

        if (sampleRate is not null)
        {
            items.Add("sample_rate", sampleRate.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(release))
        {
            items.Add("release", release);
        }

        if (!string.IsNullOrWhiteSpace(environment))
        {
            items.Add("environment", environment);
        }

        if (!string.IsNullOrWhiteSpace(transactionName))
        {
            items.Add("transaction", transactionName);
        }

        Items = items;
    }

    public BaggageHeader ToBaggageHeader() => BaggageHeader.Create(Items, useSentryPrefix: true);

    public static DynamicSamplingContext? CreateFromBaggageHeader(BaggageHeader baggage)
    {
        var items = baggage.GetSentryMembers();

        // The required items must exist and be valid to create the DSC from baggage.
        // Return null if they are not, so we know we should create it from the transaction instead.

        if (!items.TryGetValue("trace_id", out var traceId) ||
            !Guid.TryParse(traceId, out var id) || id == Guid.Empty)
        {
            return null;
        }

        if (!items.TryGetValue("public_key", out var publicKey) || string.IsNullOrWhiteSpace(publicKey))
        {
            return null;
        }

        if (items.TryGetValue("sampled", out var sampledString) && !bool.TryParse(sampledString, out _))
        {
            return null;
        }

        if (!items.TryGetValue("sample_rate", out var sampleRate) ||
            !double.TryParse(sampleRate, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate) ||
            rate is < 0.0 or > 1.0)
        {
            return null;
        }

        // See https://develop.sentry.dev/sdk/telemetry/traces/#propagated-random-value
        if (items.TryGetValue("sample_rand", out var sampleRand))
        {
            if (!double.TryParse(sampleRand, NumberStyles.Float, CultureInfo.InvariantCulture, out var rand) ||
                 rand is < 0.0 or >= 1.0)
            {
                return null;
            }
        }
        else
        {
            var seed = FnvHash.ComputeHash(traceId);
            var rand = new Random(seed).NextDouble() * rate;
            items.Add("sample_rand", rand.ToString("N4", CultureInfo.InvariantCulture));
        }
        return new DynamicSamplingContext(items);
    }

    public static DynamicSamplingContext CreateFromTransaction(TransactionTracer transaction, SentryOptions options)
    {
        // These should already be set on the transaction.
        var publicKey = options.ParsedDsn.PublicKey;
        var traceId = transaction.TraceId;
        var sampled = transaction.IsSampled;
        var sampleRate = transaction.SampleRate!.Value;
        var transactionName = transaction.NameSource.IsHighQuality() ? transaction.Name : null;

        // These two may not have been set yet on the transaction, but we can get them directly.
        var release = options.SettingLocator.GetRelease();
        var environment = options.SettingLocator.GetEnvironment();

        return new DynamicSamplingContext(
            traceId,
            publicKey,
            sampled,
            sampleRate,
            release,
            environment,
            transactionName);
    }

    public static DynamicSamplingContext CreateFromPropagationContext(SentryPropagationContext propagationContext, SentryOptions options)
    {
        var traceId = propagationContext.TraceId;
        var publicKey = options.ParsedDsn.PublicKey;
        var release = options.SettingLocator.GetRelease();
        var environment = options.SettingLocator.GetEnvironment();

        return new DynamicSamplingContext(
            traceId,
            publicKey,
            null,
            release: release,
            environment: environment);
    }
}

internal static class DynamicSamplingContextExtensions
{
    public static DynamicSamplingContext? CreateDynamicSamplingContext(this BaggageHeader baggage)
        => DynamicSamplingContext.CreateFromBaggageHeader(baggage);

    public static DynamicSamplingContext CreateDynamicSamplingContext(this TransactionTracer transaction, SentryOptions options)
        => DynamicSamplingContext.CreateFromTransaction(transaction, options);

    public static DynamicSamplingContext CreateDynamicSamplingContext(this SentryPropagationContext propagationContext, SentryOptions options)
        => DynamicSamplingContext.CreateFromPropagationContext(propagationContext, options);
}
