using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sentry.Internal.Extensions;

namespace Sentry
{
    /// <summary>
    /// Baggage Header for dynamic sampling.
    /// </summary>
    /// <seealso href="https://develop.sentry.dev/sdk/performance/dynamic-sampling-context/#baggage"/>
    /// <seealso href="https://develop.sentry.dev/sdk/performance/dynamic-sampling-context/#baggage-header"/>
    /// <seealso href="https://www.w3.org/TR/baggage/"/>
    public class BaggageHeader
    {
        private readonly IDictionary<string, string> _members;
        internal const string HttpHeaderName = "baggage";
        private const string SentryKeyPrefix = "sentry-";

        private BaggageHeader(IDictionary<string, string> members)
        {
            _members = members;
        }

        internal IReadOnlyDictionary<string, string> GetRawMembers() =>
            _members.OrderBy(x => x.Key).ToDictionary();

        internal IReadOnlyDictionary<string, string> GetSentryMembers() =>
            _members
                .Where(x => x.Key.StartsWith(SentryKeyPrefix))
                .OrderBy(x => x.Key)
                .ToDictionary(
#if NETCOREAPP || NETSTANDARD2_1
                    _ => _.Key[SentryKeyPrefix.Length..],
#else
                    _ => _.Key.Substring(SentryKeyPrefix.Length),
#endif
                    _ => Uri.UnescapeDataString(_.Value));

        /// <summary>
        /// Gets a value from the list members of the baggage header, if it exists.
        /// </summary>
        /// <param name="key">The key of the list member.</param>
        /// <returns>The value of the list member if found, or <c>null</c> otherwise.</returns>
        public string? GetValue(string key) => _members.TryGetValue(key, out var value)
            ? Uri.UnescapeDataString(value)
            : null;

        /// <summary>
        /// Sets a value for the a list member of the baggage header.
        /// </summary>
        /// <param name="key">The key of the list member.</param>
        /// <param name="value">The value of the list member.</param>
        /// <remarks>
        /// Only non-null members will be added to the list.
        /// Attempting to set a <c>null</c> value will remove the member from the list if it exists.
        /// Attempting to set a value that is already present in the list will overwrite the existing value.
        /// </remarks>
        public void SetValue(string key, string? value)
        {
            if (IsValidKey(key))
            {
                throw new ArgumentException("The provided key is invalid.", nameof(key));
            }

            SetValueInternal(key, value);
        }

        private void SetValueInternal(string key, string? value)
        {
            if (value is null)
            {
                _members.Remove(key);
            }
            else
            {
                _members[key] = Uri.EscapeDataString(value);
            }
        }

        /// <summary>
        /// Gets or sets the Sentry trace ID in the list members of the baggage header.
        /// </summary>
        public SentryId? SentryTraceId
        {
            get => _members.TryGetValue(SentryKeyPrefix + "trace_id", out var value)
                ? Guid.TryParse(value, out var traceId)
                    ? new SentryId(traceId)
                    : null
                : null;
            set => SetValueInternal(SentryKeyPrefix + "trace_id", value?.ToString());
        }

        /// <summary>
        /// Gets or sets the Sentry public key in the list members of the baggage header.
        /// </summary>
        public string? SentryPublicKey
        {
            get => GetValue(SentryKeyPrefix + "public_key");
            set => SetValueInternal(SentryKeyPrefix + "public_key", value);
        }

        /// <summary>
        /// Gets or sets the Sentry sample rate in the list members of the baggage header.
        /// </summary>
        public double? SentrySampleRate
        {
            get => _members.TryGetValue(SentryKeyPrefix + "sample_rate", out var value)
                ? double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var sampleRate)
                    ? sampleRate
                    : null
                : null;
            set => SetValueInternal(SentryKeyPrefix + "sample_rate", value?.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Gets or sets the Sentry release in the list members of the baggage header.
        /// </summary>
        public string? SentryRelease
        {
            get => GetValue(SentryKeyPrefix + "release");
            set => SetValueInternal(SentryKeyPrefix + "release", value);
        }

        /// <summary>
        /// Gets or sets the Sentry environment in the list members of the baggage header.
        /// </summary>
        public string? SentryEnvironment
        {
            get => GetValue(SentryKeyPrefix + "environment");
            set => SetValueInternal(SentryKeyPrefix + "environment", value);
        }

        /// <summary>
        /// Gets or sets the Sentry user segment in the list members of the baggage header.
        /// </summary>
        public string? SentryUserSegment
        {
            get => GetValue(SentryKeyPrefix + "user_segment");
            set => SetValueInternal(SentryKeyPrefix + "user_segment", value);
        }

        /// <summary>
        /// Gets or sets the Sentry transaction name in the list members of the baggage header.
        /// </summary>
        public string? SentryTransactionName
        {
            get => GetValue(SentryKeyPrefix + "transaction");
            set => SetValueInternal(SentryKeyPrefix + "transaction", value);
        }

        /// <summary>
        /// Creates the baggage header string based on the members of this instance.
        /// </summary>
        /// <returns>The baggage header string.</returns>
        public override string ToString()
        {
            // the item keys do not require special encoding
            // the item value are already encoded correctly, so we can just return them
            var items = _members
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}");
            return string.Join(", ", items);
        }

        /// <summary>
        /// Parses a baggage header string.
        /// </summary>
        /// <param name="baggage">The string to parse.</param>
        /// <param name="onlySentry">
        /// When <c>false</c>, the resulting object includes all list members present in the baggage header string.
        /// When <c>true</c>, the resulting object includes only members prefixed with <c>"sentry-"</c>.
        /// </param>
        /// <returns>
        /// An object representing the members baggage header, or <c>null</c> if there are no members parsed.
        /// </returns>
        public static BaggageHeader? TryParse(string baggage, bool onlySentry = false)
        {
            // Example from W3C baggage spec:
            // "key1=value1;property1;property2, key2 = value2, key3=value3; propertyKey=propertyValue"

            var items = baggage.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var resultItems = new Dictionary<string, string>(items.Length, StringComparer.Ordinal);

            foreach (var item in items)
            {
                // Per baggage spec, the value may contain = characters, so limit the split to 2 parts.
                var parts = item.Split('=', 2);
                if (parts.Length != 2)
                {
                    // malformed, missing separator, key, or value
                    continue;
                }

                var key = parts[0].Trim();
                var value = parts[1].Trim();
                if (key.Length == 0 || value.Length == 0)
                {
                    // malformed, key or value found empty
                    continue;
                }

                if (!onlySentry || key.StartsWith(SentryKeyPrefix))
                {
                    resultItems.Add(key, value);
                }
            }

            return resultItems.Count == 0 ? null : new BaggageHeader(resultItems);
        }

        private static bool IsValidKey(string key)
        {
            if (key.Length == 0)
            {
                return false;
            }

            // The rules are the same as for HTTP headers.
            // TODO: Is this public somewhere in .NET we can just call?
            // https://www.w3.org/TR/baggage/#key
            // https://www.rfc-editor.org/rfc/rfc7230#section-3.2.6
            // https://source.dot.net/#System.Net.Http/System/Net/Http/HttpRuleParser.cs,21
            const string delimiters = @"""(),/:;<=>?@[\]{}";
            return key.All(c => c >= 33 && c != 127 && !delimiters.Contains(c));
        }
    }
}
