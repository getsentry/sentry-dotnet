namespace Sentry.iOS.Extensions;

internal static class BreadcrumbExtensions
{
    public static Breadcrumb ToBreadcrumb(this SentryCocoa.SentryBreadcrumb breadcrumb)
    {
        var items = breadcrumb.Data as IEnumerable<KeyValuePair<NSString, NSObject?>>;
        var data = items?.ToDictionary(
            x => (string)x.Key,
            x => x.Value?.ToString() ?? "");

        return new Breadcrumb(
            breadcrumb.Timestamp?.ToDateTimeOffset(),
            breadcrumb.Message,
            breadcrumb.Type,
            data,
            breadcrumb.Category,
            breadcrumb.Level.ToBreadcrumbLevel());
    }

    public static SentryCocoa.SentryBreadcrumb ToCocoaBreadcrumb(this Breadcrumb breadcrumb)
    {
        NSDictionary<NSString, NSObject>? data = null;
        if (breadcrumb.Data is { } breadcrumbData)
        {
            data = new NSDictionary<NSString, NSObject>();
            foreach (var item in breadcrumbData)
            {
                data[item.Key] = NSObject.FromObject(item.Value);
            }
        }

        return new SentryCocoa.SentryBreadcrumb
        {
            Timestamp = breadcrumb.Timestamp.ToNSDate(),
            Message = breadcrumb.Message,
            Type = breadcrumb.Type,
            Data = data,
            Category = breadcrumb.Category ?? "",
            Level = breadcrumb.Level.ToCocoaSentryLevel()
        };
    }
}
