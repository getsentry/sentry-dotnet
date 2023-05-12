namespace Sentry.Internal;

/// <summary>
/// Sanitizes data that potentially contains Personally Identifiable Information (PII) before sending it to Sentry.
/// </summary>
internal static class BreadcrumbPiiSanitizer
{
    /// <summary>
    /// Redacts PII from the breadcrumb message or data
    /// </summary>
    /// <param name="breadcrumb">The breadcrumb to be sanitized</param>
    /// <returns>A new Breadcrumb with redacted copies of the message and data in the original</returns>
    public static Breadcrumb Sanitize(Breadcrumb breadcrumb)
    {
        var sanitizedData = breadcrumb.Data?.ToDictionary(
            x => x.Key,
            x => UrlPiiSanitizer.Sanitize(x.Value) ?? string.Empty
        );

        return  new Breadcrumb(
            timestamp : breadcrumb.Timestamp,
            message : UrlPiiSanitizer.Sanitize(breadcrumb.Message),
            type : breadcrumb.Type,
            data : sanitizedData,
            category : breadcrumb.Category,
            level : breadcrumb.Level
        );
    }
}
