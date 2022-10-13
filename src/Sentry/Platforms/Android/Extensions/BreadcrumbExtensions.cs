namespace Sentry.Android.Extensions;

internal static class BreadcrumbExtensions
{
    public static Breadcrumb ToBreadcrumb(this JavaSdk.Breadcrumb breadcrumb)
    {
        var data = breadcrumb.Data
            .WorkaroundKeyIteratorBug()
            .ToDictionary(x => x.Key, x => x.Value?.ToString() ?? "");

        return new(breadcrumb.Timestamp.ToDateTimeOffset(),
            breadcrumb.Message,
            breadcrumb.Type,
            data,
            breadcrumb.Category,
            breadcrumb.Level?.ToBreadcrumbLevel() ?? default);
    }

    public static JavaSdk.Breadcrumb ToJavaBreadcrumb(this Breadcrumb breadcrumb)
    {
        var javaBreadcrumb = new JavaSdk.Breadcrumb(breadcrumb.Timestamp.ToJavaDate())
        {
            Message = breadcrumb.Message,
            Type = breadcrumb.Type,
            Category = breadcrumb.Category,
            Level = breadcrumb.Level.ToJavaSentryLevel()
        };

        if (breadcrumb.Data is { } data)
        {
            var javaData = javaBreadcrumb.Data;
            foreach (var item in data)
            {
                var value = item.Value ?? "";
                javaData.Add(item.Key, value!);
            }
        }

        return javaBreadcrumb;
    }
}
