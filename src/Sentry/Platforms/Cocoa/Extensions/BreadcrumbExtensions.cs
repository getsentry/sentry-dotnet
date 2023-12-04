using Sentry.Extensibility;

namespace Sentry.Cocoa.Extensions;

internal static class BreadcrumbExtensions
{
    public static Breadcrumb ToBreadcrumb(this CocoaSdk.SentryBreadcrumb breadcrumb, IDiagnosticLogger? logger) =>
        new(
            breadcrumb.Timestamp?.ToDateTimeOffset(),
            breadcrumb.Message,
            breadcrumb.Type,
            breadcrumb.Data.ToNullableStringDictionary(logger),
            breadcrumb.Category,
            breadcrumb.Level.ToBreadcrumbLevel());

    public static CocoaSdk.SentryBreadcrumb ToCocoaBreadcrumb(this Breadcrumb breadcrumb) =>
        new()
        {
            Timestamp = breadcrumb.Timestamp.ToNSDate(),
            Message = breadcrumb.Message,
            Type = breadcrumb.Type,
            Data = breadcrumb.Data?.ToNullableNSDictionary(),
            Category = breadcrumb.Category ?? "",
            Level = breadcrumb.Level.ToCocoaSentryLevel()
        };
}
