using Sentry.Internal;
using Sentry.Protocol.Metrics;

namespace Sentry;

internal static partial class MetricHelper
{
    private static readonly RandomValuesFactory Random = new SynchronizedRandomValuesFactory();
    private const int RollupInSeconds = 10;
    private const string InvalidMetricKeyOrNameCharactersPattern = @"[^\w\-.]+";
    private const string InvalidTagKeyCharactersPattern = @"[^\w\-.\/]+";
    // See https://docs.sysdig.com/en/docs/sysdig-monitor/integrations/working-with-integrations/custom-integrations/integrate-statsd-metrics/#characters-allowed-for-statsd-metric-names
    private const string InvalidMetricUnitCharactersPattern = @"[^\w]+";

#if NET6_0_OR_GREATER
    private static readonly DateTimeOffset UnixEpoch = DateTimeOffset.UnixEpoch;
#else
    private static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
#endif

#if NET9_0_OR_GREATER
    [GeneratedRegex(InvalidMetricKeyOrNameCharactersPattern)]
    private static partial Regex InvalidMetricKeyOrNameCharacters { get; }

    [GeneratedRegex(InvalidTagKeyCharactersPattern)]
    private static partial Regex InvalidTagKeyCharacters { get; }

    [GeneratedRegex(InvalidMetricUnitCharactersPattern)]
    private static partial Regex InvalidMetricUnitCharacters { get; }
#elif NET8_0
    [GeneratedRegex(InvalidMetricKeyOrNameCharactersPattern)]
    private static partial Regex InvalidMetricKeyOrNameCharacters();

    [GeneratedRegex(InvalidTagKeyCharactersPattern)]
    private static partial Regex InvalidTagKeyCharacters();

    [GeneratedRegex(InvalidMetricUnitCharactersPattern)]
    private static partial Regex InvalidMetricUnitCharacters();
#else
    private static readonly Regex InvalidMetricKeyOrNameCharacters = new(InvalidMetricKeyOrNameCharactersPattern, RegexOptions.Compiled);
    private static readonly Regex InvalidTagKeyCharacters = new(InvalidTagKeyCharactersPattern, RegexOptions.Compiled);
    private static readonly Regex InvalidMetricUnitCharacters = new(InvalidMetricUnitCharactersPattern, RegexOptions.Compiled);
#endif

#if NET8_0
    internal static string SanitizeMetricKeyOrName(string input) => InvalidMetricKeyOrNameCharacters().Replace(input, "_");
    internal static string SanitizeTagKey(string input) => InvalidTagKeyCharacters().Replace(input, "");
    internal static string SanitizeMetricUnit(string input) => InvalidMetricUnitCharacters().Replace(input, "");
#else
    internal static string SanitizeMetricKeyOrName(string input) => InvalidMetricKeyOrNameCharacters.Replace(input, "_");
    internal static string SanitizeTagKey(string input) => InvalidTagKeyCharacters.Replace(input, "");
    internal static string SanitizeMetricUnit(string input) => InvalidMetricUnitCharacters.Replace(input, "");
#endif

    internal static long GetDayBucketKey(this DateTimeOffset timestamp)
    {
        var utc = timestamp.ToUniversalTime();
        var dayOnly = new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, 0, TimeSpan.Zero);
        return (long)(dayOnly - UnixEpoch).TotalSeconds;
    }

    internal static long GetTimeBucketKey(this DateTimeOffset timestamp)
    {
        var seconds = (long)(timestamp.ToUniversalTime() - UnixEpoch).TotalSeconds;
        return (seconds / RollupInSeconds) * RollupInSeconds;
    }

    /// <summary>
    /// The aggregator shifts it's flushing by up to an entire rollup window to avoid multiple clients trampling on end
    /// of a 10 second window as all the buckets are anchored to multiples of ROLLUP seconds.  We randomize this number
    /// once per aggregator boot to achieve some level of offsetting across a fleet of deployed SDKs.  Relay itself will
    /// also apply independent jittering.
    /// </summary>
    /// <remarks>Internal for testing</remarks>
    internal static double FlushShift = Random.NextInt(0, 1000) * RollupInSeconds;
    internal static DateTimeOffset GetCutoff() => DateTimeOffset.UtcNow
        .Subtract(TimeSpan.FromSeconds(RollupInSeconds))
        .Subtract(TimeSpan.FromMilliseconds(FlushShift));

    private static readonly Lazy<KeyValuePair<string, string>[]> LazyTagValueReplacements = new(() =>
    [
        new KeyValuePair<string, string>("\n", @"\n"),
        new KeyValuePair<string, string>("\r", @"\r"),
        new KeyValuePair<string, string>("\t", @"\t"),
        new KeyValuePair<string, string>("|", "\u007c"),
        new KeyValuePair<string, string>(",", "\u002c")
    ]);
    private static KeyValuePair<string, string>[] TagValueReplacements => LazyTagValueReplacements.Value;
    internal static string SanitizeTagValue(string input)
    {
        // Replace back slashes before we add any of these ourselves when substituting "\n" for other control characters
        input = input.Replace(@"\", @"\\");
        foreach (var (reservedCharacter, replacementValue) in TagValueReplacements)
        {
            input = input.Replace(reservedCharacter, replacementValue);
        }
        return input;
    }

    public static string GetMetricBucketKey(MetricType type, string metricKey, MeasurementUnit unit,
        IDictionary<string, string>? tags)
    {
        var typePrefix = type.ToStatsdType();
        var serializedTags = GetTagsKey(tags);

        return $"{typePrefix}_{metricKey}_{unit}_{serializedTags}";
    }

    internal static string GetTagsKey(IDictionary<string, string>? tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return string.Empty;
        }

        const char pairDelimiter = ',';  // Delimiter between key-value pairs
        const char keyValueDelimiter = '=';  // Delimiter between key and value
        const char escapeChar = '\\';

        var builder = new StringBuilder();

        foreach (var tag in tags)
        {
            // Escape delimiters in key and value
            var key = EscapeString(tag.Key, pairDelimiter, keyValueDelimiter, escapeChar);
            var value = EscapeString(tag.Value, pairDelimiter, keyValueDelimiter, escapeChar);

            if (builder.Length > 0)
            {
                builder.Append(pairDelimiter);
            }

            builder.Append(key).Append(keyValueDelimiter).Append(value);
        }

        return builder.ToString();

        static string EscapeString(string input, params char[] charsToEscape)
        {
            var escapedString = new StringBuilder(input.Length);

            foreach (var ch in input)
            {
                if (charsToEscape.Contains(ch))
                {
                    escapedString.Append(escapeChar);  // Prefix with escape character
                }
                escapedString.Append(ch);
            }

            return escapedString.ToString();
        }
    }
}
