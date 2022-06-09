namespace Sentry.Android.Extensions;

internal static class BreadcrumbExtensions
{
    public static Breadcrumb ToBreadcrumb(this Java.Breadcrumb breadcrumb)
    {
        var data = breadcrumb.Data.ToDictionary(x => x.Key, x => x.Value.ToString());

        return new(breadcrumb.Timestamp.ToDateTimeOffset(),
            breadcrumb.Message,
            breadcrumb.Type,
            data,
            breadcrumb.Category,
            breadcrumb.Level?.ToBreadcrumbLevel() ?? default);
    }

    public static Java.Breadcrumb ToJavaBreadcrumb(this Breadcrumb breadcrumb)
    {
        var javaBreadcrumb = new Java.Breadcrumb(breadcrumb.Timestamp.ToJavaDate())
        {
            Message = breadcrumb.Message,
            Type = breadcrumb.Type,
            Category = breadcrumb.Category,
            Level = breadcrumb.Level.ToJavaSentryLevel()
        };

        if (breadcrumb.Data != null)
        {
            foreach (var item in breadcrumb.Data)
            {
                javaBreadcrumb.SetData(item.Key, item.Value!);
            }
        }

        return javaBreadcrumb;
    }
}
