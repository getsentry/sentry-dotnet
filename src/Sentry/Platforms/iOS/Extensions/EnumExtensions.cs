namespace Sentry.iOS.Extensions;

internal static class EnumExtensions
{
    // These align, so we can just cast
    public static SentryLevel ToSentryLevel(this SentryCocoa.SentryLevel level) => (SentryLevel)level;
    public static SentryCocoa.SentryLevel ToCocoaSentryLevel(this SentryLevel level) => (SentryCocoa.SentryLevel)level;

    public static BreadcrumbLevel ToBreadcrumbLevel(this SentryCocoa.SentryLevel level) =>
        level switch
        {
            SentryCocoa.SentryLevel.Debug => BreadcrumbLevel.Debug,
            SentryCocoa.SentryLevel.Info => BreadcrumbLevel.Info,
            SentryCocoa.SentryLevel.Warning => BreadcrumbLevel.Warning,
            SentryCocoa.SentryLevel.Error => BreadcrumbLevel.Error,
            SentryCocoa.SentryLevel.Fatal => BreadcrumbLevel.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };

    public static SentryCocoa.SentryLevel ToCocoaSentryLevel(this BreadcrumbLevel level) =>
        level switch
        {
            BreadcrumbLevel.Debug => SentryCocoa.SentryLevel.Debug,
            BreadcrumbLevel.Info => SentryCocoa.SentryLevel.Info,
            BreadcrumbLevel.Warning => SentryCocoa.SentryLevel.Warning,
            BreadcrumbLevel.Error => SentryCocoa.SentryLevel.Error,
            BreadcrumbLevel.Critical => SentryCocoa.SentryLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, message: default)
        };
}
