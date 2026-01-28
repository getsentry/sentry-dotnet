using Sentry.JavaSdk;
using Sentry.JavaSdk.Android.Replay;
using System.Globalization;

// ReSharper disable once CheckNamespace - match generated code namespace
namespace Sentry;

// Add IReplayBreadcrumbConverter interface to DefaultReplayBreadcrumbConverter, to work source generator issue
internal class DotnetReplayBreadcrumbConverter : DefaultReplayBreadcrumbConverter, IReplayBreadcrumbConverter
{
    private const string HttpCategory = "http";

    // Kotlin expects these keys to be Double or Long (ms)
    private const string HttpStartTimestampKey = "http.start_timestamp";
    private const string HttpEndTimestampKey = "http.end_timestamp";

    public DotnetReplayBreadcrumbConverter(Sentry.JavaSdk.SentryOptions options) : base(options)
    {
    }

    public override global::IO.Sentry.Rrweb.RRWebEvent? Convert(Sentry.JavaSdk.Breadcrumb breadcrumb)
    {
        if (breadcrumb is null)
        {
            return null;
        }

        // The Java converter expects httpStartTimestamp/httpEndTimestamp to be Double or Long.
        // .NET breadcrumb data is always stored as strings. We convert these to numeric here so that the base.Convert()
        // method doesn't throw an exception.
        try
        {
            if (breadcrumb.Category == HttpCategory && breadcrumb.Data is { } data)
            {
                NormalizeTimestampField(data, HttpStartTimestampKey);
                NormalizeTimestampField(data, HttpEndTimestampKey);
            }
        }
        catch
        {
            // Best-effort: never fail conversion because of parsing issues... we may be parsing breadcrumbs that don't
            // originate from the .NET SDK.
        }

        return base.Convert(breadcrumb);
    }

    private static void NormalizeTimestampField(IDictionary<string, Java.Lang.Object> data, string key)
    {
        data.TryGetValue(key, out var value);
        if (value is null or Java.Lang.Long or Java.Lang.Double or Java.Lang.Integer or Java.Lang.Float)
        {
            return;
        }

        // Note: `data.Get` returns `Java.Lang.Object`, not a .NET `string`.
        var str = (value as Java.Lang.String)?.ToString() ?? value.ToString();
        if (string.IsNullOrWhiteSpace(str))
        {
            return;
        }

        // Prefer integer milliseconds, but accept floating point too.
        // Use invariant culture to avoid commas, etc.
        if (long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var asLong))
        {
            data[key] = Java.Lang.Long.ValueOf(asLong);
            return;
        }

        if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var asDouble))
        {
            // Preserve type as Double; Java converter divides by 1000.0.
            data[key] = Java.Lang.Double.ValueOf(asDouble);
        }
    }
}
