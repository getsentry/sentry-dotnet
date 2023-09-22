using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Baggage Header for dynamic sampling.
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/performance/dynamic-sampling-context"/>
/// <seealso href="https://www.w3.org/TR/baggage"/>
public class BaggageHeader
{
    /// <summary>
    /// The HTTP header name for baggage
    /// </summary>
    public const string HttpHeaderName = "baggage";

    internal const string SentryKeyPrefix = "sentry-";

    internal static IDiagnosticLogger? Logger { get; set; } = SentrySdk.CurrentOptions?.DiagnosticLogger;

    // https://www.w3.org/TR/baggage/#baggage-string
    // "Uniqueness of keys between multiple list-members in a baggage-string is not guaranteed."
    // "The order of duplicate entries SHOULD be preserved when mutating the list."

    internal IReadOnlyList<KeyValuePair<string, string>> Members { get; }

    private BaggageHeader(IEnumerable<KeyValuePair<string, string>> members) =>
        Members = members.ToList();

    // We can safely return a dictionary of Sentry members, as we are in control over the keys added.
    // Just to be safe though, we'll group by key and only take the first of each one.
    internal IReadOnlyDictionary<string, string> GetSentryMembers() =>
        Members
            .Where(kvp => kvp.Key.StartsWith(SentryKeyPrefix))
            .GroupBy(kvp => kvp.Key, kvp => kvp.Value)
            .ToDictionary(
                g => g.Key[SentryKeyPrefix.Length..],
                g => g.First());

    /// <summary>
    /// Creates the baggage header string based on the members of this instance.
    /// </summary>
    /// <returns>The baggage header string.</returns>
    public override string ToString()
    {
        // The keys do not require special encoding.  The values are percent-encoded.
        // The results should not be sorted, as the baggage spec says original ordering should be preserved.
        var members = Members.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}");

        // Whitespace after delimiter is optional by the spec, but typical by convention.
        return string.Join(", ", members);
    }

    /// <summary>
    /// Parses a baggage header string
    /// </summary>
    /// <param name="baggage">The string to parse.</param>
    /// <returns>
    /// An object representing the sentry baggage header, or <c>null</c> if there are no members parsed.
    /// </returns>
    public static BaggageHeader? TryParse(string baggage) => TryParse(baggage, true);

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
    internal static BaggageHeader? TryParse(string baggage, bool onlySentry)
    {
        // Example from W3C baggage spec:
        // "key1=value1;property1;property2, key2 = value2, key3=value3; propertyKey=propertyValue"

        var items = baggage.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var members = new List<KeyValuePair<string, string>>(items.Length);

        foreach (var item in items)
        {
            // Per baggage spec, the value may contain = characters, so limit the split to 2 parts.
            var parts = item.Split('=', 2);
            if (parts.Length != 2)
            {
                Logger?.LogWarning(
                    "The baggage header has an item without a '=' separator, and it will be discarded. " +
                    "The item is: \"{0}\"", item);
                continue;
            }

            var key = parts[0].Trim();
            if (key.Length == 0)
            {
                Logger?.LogWarning(
                    "The baggage header has an item with an empty key, and it will be discarded. " +
                    "The item is: \"{0}\"", item);
                continue;
            }

            var value = parts[1].Trim();
            if (value.Length == 0)
            {
                Logger?.LogWarning(
                    "The baggage header has an item with an empty value, and it will be discarded. " +
                    "The item is: \"{0}\"", item);
                continue;
            }

            if (!onlySentry || key.StartsWith(SentryKeyPrefix))
            {
                // Values are percent-encoded.  Decode them before storing.
                members.Add(key, Uri.UnescapeDataString(value));
            }
        }

        return members.Count == 0 ? null : new BaggageHeader(members);
    }

    internal static BaggageHeader Create(
        IEnumerable<KeyValuePair<string, string>> items,
        bool useSentryPrefix = false)
    {
        var members = items.Where(member => IsValidKey(member.Key));

        if (useSentryPrefix)
        {
            members = members.Select(kvp => new KeyValuePair<string, string>(SentryKeyPrefix + kvp.Key, kvp.Value));
        }

        return new BaggageHeader(members);
    }

    internal static BaggageHeader Merge(IEnumerable<BaggageHeader> baggageHeaders) =>
        new(baggageHeaders.SelectMany(x => x.Members));

    private static bool IsValidKey(string? key)
    {
        if (string.IsNullOrEmpty(key))
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
