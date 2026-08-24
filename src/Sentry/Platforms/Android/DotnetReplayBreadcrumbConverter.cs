using Sentry.JavaSdk;
using Sentry.JavaSdk.Android.Replay;

namespace Sentry.Android;

internal class DotnetReplayBreadcrumbConverter(Sentry.JavaSdk.SentryOptions options)
    : DefaultReplayBreadcrumbConverter(options), IReplayBreadcrumbConverter
{
    private const string HttpCategory = "http";

    public override global::IO.Sentry.Rrweb.RRWebEvent? Convert(Sentry.JavaSdk.Breadcrumb breadcrumb)
    {
        // The Java SDK automatically converts breadcrumbs for outgoing http requests into performance spans
        // that show in the Network tab of session replays... however, it expects certain data to be stored in a
        // specific format in the breadcrumb.data. It needs values for httpStartTimestamp and httpEndTimestamp
        // stored as Double or Long representations of timestamps (milliseconds since epoch).
        // .NET breadcrumb data is always stored as strings, so we have to convert these to numeric values here.
        try
        {
            if (breadcrumb is { Category: HttpCategory, Data: { } data })
            {
                NormalizeTimestampField(data, SentryHttpMessageHandler.HttpStartTimestampKey);
                NormalizeTimestampField(data, SentryHttpMessageHandler.HttpEndTimestampKey);
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
        if (!data.TryGetValue(key, out var value) || value is Java.Lang.Number)
        {
            return;
        }

        var str = (value as Java.Lang.String)?.ToString() ?? value.ToString();
        if (string.IsNullOrWhiteSpace(str))
        {
            return;
        }

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
