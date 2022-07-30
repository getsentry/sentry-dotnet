namespace Sentry.Android.Extensions;

internal static class EnumExtensions
{
    public static SentryLevel ToSentryLevel(this Java.SentryLevel level) =>
        level.Name() switch
        {
            "DEBUG" => SentryLevel.Debug,
            "INFO" => SentryLevel.Info,
            "WARNING" => SentryLevel.Warning,
            "ERROR" => SentryLevel.Error,
            "FATAL" => SentryLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level.Name(), message: default)
        };

    public static Java.SentryLevel ToJavaSentryLevel(this SentryLevel level) =>
        level switch
        {
            SentryLevel.Debug => Java.SentryLevel.Debug!,
            SentryLevel.Info => Java.SentryLevel.Info!,
            SentryLevel.Warning => Java.SentryLevel.Warning!,
            SentryLevel.Error => Java.SentryLevel.Error!,
            SentryLevel.Fatal => Java.SentryLevel.Fatal!,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, message: default)
        };

    public static BreadcrumbLevel ToBreadcrumbLevel(this Java.SentryLevel level) =>
        level.Name() switch
        {
            "DEBUG" => BreadcrumbLevel.Debug,
            "INFO" => BreadcrumbLevel.Info,
            "WARNING" => BreadcrumbLevel.Warning,
            "ERROR" => BreadcrumbLevel.Error,
            "FATAL" => BreadcrumbLevel.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level.Name(), message: default)
        };

    public static Java.SentryLevel ToJavaSentryLevel(this BreadcrumbLevel level) =>
        level switch
        {
            BreadcrumbLevel.Debug => Java.SentryLevel.Debug!,
            BreadcrumbLevel.Info => Java.SentryLevel.Info!,
            BreadcrumbLevel.Warning => Java.SentryLevel.Warning!,
            BreadcrumbLevel.Error => Java.SentryLevel.Error!,
            BreadcrumbLevel.Critical => Java.SentryLevel.Fatal!,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, message: default)
        };

    public static SpanStatus ToSpanStatus(this Java.SpanStatus status) =>
        status.Name() switch
        {
            "OK" => SpanStatus.Ok,
            "CANCELLED" => SpanStatus.Cancelled,
            "INTERNAL_ERROR" => SpanStatus.InternalError,
            "UNKNOWN" => SpanStatus.UnknownError,
            "UNKNOWN_ERROR" => SpanStatus.UnknownError,
            "INVALID_ARGUMENT" => SpanStatus.InvalidArgument,
            "DEADLINE_EXCEEDED" => SpanStatus.DeadlineExceeded,
            "NOT_FOUND" => SpanStatus.NotFound,
            "ALREADY_EXISTS" => SpanStatus.AlreadyExists,
            "PERMISSION_DENIED" => SpanStatus.PermissionDenied,
            "RESOURCE_EXHAUSTED" => SpanStatus.ResourceExhausted,
            "FAILED_PRECONDITION" => SpanStatus.FailedPrecondition,
            "ABORTED" => SpanStatus.Aborted,
            "OUT_OF_RANGE" => SpanStatus.OutOfRange,
            "UNIMPLEMENTED" => SpanStatus.Unimplemented,
            "UNAVAILABLE" => SpanStatus.Unavailable,
            "DATA_LOSS" => SpanStatus.DataLoss,
            "UNAUTHENTICATED" => SpanStatus.Unauthenticated,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status.Name(), message: default)
        };

    public static Java.SpanStatus ToJavaSpanStatus(this SpanStatus status) =>
        status switch
        {
            SpanStatus.Ok => Java.SpanStatus.Ok!,
            SpanStatus.Cancelled => Java.SpanStatus.Cancelled!,
            SpanStatus.InternalError => Java.SpanStatus.InternalError!,
            SpanStatus.UnknownError => Java.SpanStatus.UnknownError!,
            SpanStatus.InvalidArgument => Java.SpanStatus.InvalidArgument!,
            SpanStatus.DeadlineExceeded => Java.SpanStatus.DeadlineExceeded!,
            SpanStatus.NotFound => Java.SpanStatus.NotFound!,
            SpanStatus.AlreadyExists => Java.SpanStatus.AlreadyExists!,
            SpanStatus.PermissionDenied => Java.SpanStatus.PermissionDenied!,
            SpanStatus.ResourceExhausted => Java.SpanStatus.ResourceExhausted!,
            SpanStatus.FailedPrecondition => Java.SpanStatus.FailedPrecondition!,
            SpanStatus.Aborted => Java.SpanStatus.Aborted!,
            SpanStatus.OutOfRange => Java.SpanStatus.OutOfRange!,
            SpanStatus.Unimplemented => Java.SpanStatus.Unimplemented!,
            SpanStatus.Unavailable => Java.SpanStatus.Unavailable!,
            SpanStatus.DataLoss => Java.SpanStatus.DataLoss!,
            SpanStatus.Unauthenticated => Java.SpanStatus.Unauthenticated!,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, message: default)
        };
}
