namespace Sentry.Android.Extensions;

internal static class EnumExtensions
{
    public static SentryLevel ToSentryLevel(this JavaSdk.SentryLevel level) =>
        level.Name() switch
        {
            "DEBUG" => SentryLevel.Debug,
            "INFO" => SentryLevel.Info,
            "WARNING" => SentryLevel.Warning,
            "ERROR" => SentryLevel.Error,
            "FATAL" => SentryLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level.Name(), message: default)
        };

    public static JavaSdk.SentryLevel ToJavaSentryLevel(this SentryLevel level) =>
        level switch
        {
            SentryLevel.Debug => JavaSdk.SentryLevel.Debug,
            SentryLevel.Info => JavaSdk.SentryLevel.Info,
            SentryLevel.Warning => JavaSdk.SentryLevel.Warning,
            SentryLevel.Error => JavaSdk.SentryLevel.Error,
            SentryLevel.Fatal => JavaSdk.SentryLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, message: default)
        };

    public static BreadcrumbLevel ToBreadcrumbLevel(this JavaSdk.SentryLevel level) =>
        level.Name() switch
        {
            "DEBUG" => BreadcrumbLevel.Debug,
            "INFO" => BreadcrumbLevel.Info,
            "WARNING" => BreadcrumbLevel.Warning,
            "ERROR" => BreadcrumbLevel.Error,
            "FATAL" => BreadcrumbLevel.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level.Name(), message: default)
        };

    public static JavaSdk.SentryLevel ToJavaSentryLevel(this BreadcrumbLevel level) =>
        level switch
        {
            BreadcrumbLevel.Debug => JavaSdk.SentryLevel.Debug,
            BreadcrumbLevel.Info => JavaSdk.SentryLevel.Info,
            BreadcrumbLevel.Warning => JavaSdk.SentryLevel.Warning,
            BreadcrumbLevel.Error => JavaSdk.SentryLevel.Error,
            BreadcrumbLevel.Critical => JavaSdk.SentryLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, message: default)
        };

    public static SpanStatus ToSpanStatus(this JavaSdk.SpanStatus status) =>
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

    public static JavaSdk.SpanStatus ToJavaSpanStatus(this SpanStatus status) =>
        status switch
        {
            SpanStatus.Ok => JavaSdk.SpanStatus.Ok,
            SpanStatus.Cancelled => JavaSdk.SpanStatus.Cancelled,
            SpanStatus.InternalError => JavaSdk.SpanStatus.InternalError,
            SpanStatus.UnknownError => JavaSdk.SpanStatus.UnknownError,
            SpanStatus.InvalidArgument => JavaSdk.SpanStatus.InvalidArgument,
            SpanStatus.DeadlineExceeded => JavaSdk.SpanStatus.DeadlineExceeded,
            SpanStatus.NotFound => JavaSdk.SpanStatus.NotFound,
            SpanStatus.AlreadyExists => JavaSdk.SpanStatus.AlreadyExists,
            SpanStatus.PermissionDenied => JavaSdk.SpanStatus.PermissionDenied,
            SpanStatus.ResourceExhausted => JavaSdk.SpanStatus.ResourceExhausted,
            SpanStatus.FailedPrecondition => JavaSdk.SpanStatus.FailedPrecondition,
            SpanStatus.Aborted => JavaSdk.SpanStatus.Aborted,
            SpanStatus.OutOfRange => JavaSdk.SpanStatus.OutOfRange,
            SpanStatus.Unimplemented => JavaSdk.SpanStatus.Unimplemented,
            SpanStatus.Unavailable => JavaSdk.SpanStatus.Unavailable,
            SpanStatus.DataLoss => JavaSdk.SpanStatus.DataLoss,
            SpanStatus.Unauthenticated => JavaSdk.SpanStatus.Unauthenticated,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, message: default)
        };

    public static TransactionNameSource ToTransactionNameSource(this JavaSdk.Protocol.TransactionNameSource source) =>
        source.Name() switch
        {
            "CUSTOM" => TransactionNameSource.Custom,
            "URL" => TransactionNameSource.Url,
            "ROUTE" => TransactionNameSource.Route,
            "VIEW" => TransactionNameSource.View,
            "TASK" => TransactionNameSource.Task,
            "COMPONENT" => TransactionNameSource.Component,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source.Name(), message: default)
        };

    public static JavaSdk.Protocol.TransactionNameSource ToJavaTransactionNameSource(this TransactionNameSource source) =>
        source switch
        {
            TransactionNameSource.Custom => JavaSdk.Protocol.TransactionNameSource.Custom,
            TransactionNameSource.Url => JavaSdk.Protocol.TransactionNameSource.Url,
            TransactionNameSource.Route => JavaSdk.Protocol.TransactionNameSource.Route,
            TransactionNameSource.View => JavaSdk.Protocol.TransactionNameSource.View,
            TransactionNameSource.Task => JavaSdk.Protocol.TransactionNameSource.Task,
            TransactionNameSource.Component => JavaSdk.Protocol.TransactionNameSource.Component,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, message: default)
        };

    public static SentryReplayQuality ToSentryReplayQuality(this JavaSdk.SentryReplayOptions.SentryReplayQuality replayQuality) =>
        replayQuality.Name() switch
        {
            "LOW" => SentryReplayQuality.Low,
            "MEDIUM" => SentryReplayQuality.Medium,
            "HIGH" => SentryReplayQuality.High,
            _ => throw new ArgumentOutOfRangeException(nameof(replayQuality), replayQuality.Name(), message: default)
        };

    public static JavaSdk.SentryReplayOptions.SentryReplayQuality ToJavaReplayQuality(this SentryReplayQuality replayQuality) =>
        replayQuality switch
        {
            SentryReplayQuality.Low => JavaSdk.SentryReplayOptions.SentryReplayQuality.Low,
            SentryReplayQuality.Medium => JavaSdk.SentryReplayOptions.SentryReplayQuality.Medium,
            SentryReplayQuality.High => JavaSdk.SentryReplayOptions.SentryReplayQuality.High,
            _ => throw new ArgumentOutOfRangeException(nameof(replayQuality), replayQuality, message: default)
        };
}
