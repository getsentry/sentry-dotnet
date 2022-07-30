namespace Sentry.iOS.Extensions;

internal static class CocoaExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this NSDate timestamp) => new((DateTime)timestamp);

    public static NSDate ToNSDate(this DateTimeOffset timestamp) => (NSDate)timestamp.UtcDateTime;
}
