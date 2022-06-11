namespace Sentry.Android.Extensions;

internal static class JavaExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this JavaDate timestamp) => DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Time);

    public static JavaDate ToJavaDate(this DateTimeOffset timestamp) => new(timestamp.ToUnixTimeMilliseconds());

    public static Exception ToException(this Throwable throwable) => Throwable.ToException(throwable);

    public static Throwable ToThrowable(this Exception exception) => Throwable.FromException(exception);
}
