using System;
using System.Collections.Generic;
using System.Globalization;
using Sentry.Internal.Extensions;

namespace Sentry
{
    /// <summary>
    /// Provides the Dynamic Sampling Context for a transaction.
    /// </summary>
    /// <seealso href="https://develop.sentry.dev/sdk/performance/dynamic-sampling-context"/>
    public class DynamicSamplingContext
    {
        // All values are stored in a dictionary, because it is possible to have other than those explicitly defined
        // in this class.  For example, the context can come from a baggage header with other Sentry- prefixed keys.
        private readonly IReadOnlyDictionary<string, string> _items;

        private DynamicSamplingContext(IReadOnlyDictionary<string, string> items) => _items = items;

        /// <summary>
        /// Gets an empty <see cref="DynamicSamplingContext"/> that can be used to "freeze" the DSC on a transaction.
        /// </summary>
        internal static readonly DynamicSamplingContext Empty = new(new Dictionary<string, string>().AsReadOnly());

        internal bool IsEmpty => _items.Count == 0;

        private DynamicSamplingContext(
            SentryId traceId,
            string publicKey,
            double sampleRate,
            string? release = null,
            string? environment = null,
            string? userSegment = null,
            string? transactionName = null)
        {
            // Validate and set required fields
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

            // Set optional fields
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

            _items = items;
        }

        /// <summary>
        /// Gets the trace ID of the Dynamic Sampling Context.
        /// </summary>
        public SentryId TraceId => SentryId.Parse(_items["trace_id"]);

        /// <summary>
        /// Gets the public key of the Dynamic Sampling Context.
        /// </summary>
        public string PublicKey => _items["public_key"];

        /// <summary>
        /// Gets the sample rate of the Dynamic Sampling Context.
        /// </summary>
        public double SampleRate => double.Parse(_items["sample_rate"], CultureInfo.InvariantCulture);

        /// <summary>
        /// Gets the release of the Dynamic Sampling Context, or <c>null</c> if none was set.
        /// </summary>
        public string? Release => _items.TryGetValue("release", out var value) ? value : null;

        /// <summary>
        /// Gets the environment of the Dynamic Sampling Context, or <c>null</c> if none was set.
        /// </summary>
        public string? Environment => _items.TryGetValue("environment", out var value) ? value : null;

        /// <summary>
        /// Gets the user segment of the Dynamic Sampling Context, or <c>null</c> if none was set.
        /// </summary>
        public string? UserSegment => _items.TryGetValue("user_segment", out var value) ? value : null;

        /// <summary>
        /// Gets the transaction name of the Dynamic Sampling Context, or <c>null</c> if none was set.
        /// </summary>
        public string? TransactionName => _items.TryGetValue("transaction", out var value) ? value : null;

        internal IReadOnlyDictionary<string, string> GetItems() => _items;

        internal BaggageHeader ToBaggageHeader() => BaggageHeader.Create(_items);

        internal static DynamicSamplingContext? CreateFromBaggageHeader(BaggageHeader baggage)
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

        internal static DynamicSamplingContext CreateFromTransaction(TransactionTracer transaction, SentryOptions options)
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
        internal static DynamicSamplingContext? CreateDynamicSamplingContext(this BaggageHeader baggage)
            => DynamicSamplingContext.CreateFromBaggageHeader(baggage);

        internal static DynamicSamplingContext CreateDynamicSamplingContext(this TransactionTracer transaction, SentryOptions options)
            => DynamicSamplingContext.CreateFromTransaction(transaction, options);
    }
}
