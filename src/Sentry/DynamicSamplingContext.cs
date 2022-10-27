using System.Globalization;
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
        double sampleRate,
        string? release = null,
        string? environment = null,
        string? userSegment = null,
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
            ["sample_rate"] = sampleRate.ToString(CultureInfo.InvariantCulture)
        };

        // Set optional values
        if (!string.IsNullOrWhiteSpace(release))
        {
            items.Add("release", release);
        }

        if (!string.IsNullOrWhiteSpace(environment))
        {
            items.Add("environment", environment);
        }

        if (!string.IsNullOrWhiteSpace(userSegment))
        {
            items.Add("user_segment", userSegment);
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

        if (!items.TryGetValue("sample_rate", out var sampleRate) ||
            !double.TryParse(sampleRate, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate) ||
            rate is < 0.0 or > 1.0)
        {
            return null;
        }

        return new DynamicSamplingContext(items);
    }

    public static DynamicSamplingContext CreateFromTransaction(TransactionTracer transaction, SentryOptions options)
    {
        // These should already be set on the transaction.
        var publicKey = Dsn.Parse(options.Dsn!).PublicKey;
        var traceId = transaction.TraceId;
        var sampleRate = transaction.SampleRate!.Value;
        var userSegment = transaction.User.Segment;
        var transactionName = transaction.NameSource.IsHighQuality() ? transaction.Name : null;

        // These two may not have been set yet on the transaction, but we can get them directly.
        var release = options.SettingLocator.GetRelease();
        var environment = options.SettingLocator.GetEnvironment();

        return new DynamicSamplingContext(
            traceId,
            publicKey,
            sampleRate,
            release,
            environment,
            userSegment,
            transactionName);
    }
}

internal static class DynamicSamplingContextExtensions
{
    public static DynamicSamplingContext? CreateDynamicSamplingContext(this BaggageHeader baggage)
        => DynamicSamplingContext.CreateFromBaggageHeader(baggage);

    public static DynamicSamplingContext CreateDynamicSamplingContext(this TransactionTracer transaction, SentryOptions options)
        => DynamicSamplingContext.CreateFromTransaction(transaction, options);
}
