using Sentry.JavaSdk;
using Sentry.JavaSdk.Android.Replay;

namespace Sentry.Android;

internal class DotnetReplayBreadcrumbConverter(Sentry.JavaSdk.SentryOptions options)
    : DefaultReplayBreadcrumbConverter(options), IReplayBreadcrumbConverter
{
    private const string HttpCategory = "http";

    public override global::IO.Sentry.Rrweb.RRWebEvent? Convert(Sentry.JavaSdk.Breadcrumb breadcrumb)
    {
        // The Java converter expects httpStartTimestamp/httpEndTimestamp to be Double or Long.
        // .NET breadcrumb data is always stored as strings. We convert these to numeric here so that the base.Convert()
        // method doesn't throw an exception.
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
