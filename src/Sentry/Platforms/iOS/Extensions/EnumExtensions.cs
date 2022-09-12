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

    public static bool? ToNullableBoolean(this SentryCocoa.SentrySampleDecision decision) =>
        decision switch
        {
            SentryCocoa.SentrySampleDecision.Yes => true,
            SentryCocoa.SentrySampleDecision.No => false,
            SentryCocoa.SentrySampleDecision.Undecided => null,
            _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, null)
        };

    public static SentryCocoa.SentrySampleDecision ToCocoaSampleDecision(this bool? decision) =>
        decision switch
        {
            true => SentryCocoa.SentrySampleDecision.Yes,
            false => SentryCocoa.SentrySampleDecision.No,
            null => SentryCocoa.SentrySampleDecision.Undecided
        };

    public static SpanStatus? ToSpanStatus(this SentryCocoa.SentrySpanStatus status) =>
        status switch
        {
            SentryCocoa.SentrySpanStatus.Undefined => null,
            SentryCocoa.SentrySpanStatus.Ok => SpanStatus.Ok,
            SentryCocoa.SentrySpanStatus.Cancelled => SpanStatus.Cancelled,
            SentryCocoa.SentrySpanStatus.InternalError => SpanStatus.InternalError,
            SentryCocoa.SentrySpanStatus.UnknownError => SpanStatus.UnknownError,
            SentryCocoa.SentrySpanStatus.InvalidArgument => SpanStatus.InvalidArgument,
            SentryCocoa.SentrySpanStatus.DeadlineExceeded => SpanStatus.DeadlineExceeded,
            SentryCocoa.SentrySpanStatus.NotFound => SpanStatus.NotFound,
            SentryCocoa.SentrySpanStatus.AlreadyExists => SpanStatus.AlreadyExists,
            SentryCocoa.SentrySpanStatus.PermissionDenied => SpanStatus.PermissionDenied,
            SentryCocoa.SentrySpanStatus.ResourceExhausted => SpanStatus.ResourceExhausted,
            SentryCocoa.SentrySpanStatus.FailedPrecondition => SpanStatus.FailedPrecondition,
            SentryCocoa.SentrySpanStatus.Aborted => SpanStatus.Aborted,
            SentryCocoa.SentrySpanStatus.OutOfRange => SpanStatus.OutOfRange,
            SentryCocoa.SentrySpanStatus.Unimplemented => SpanStatus.Unimplemented,
            SentryCocoa.SentrySpanStatus.Unavailable => SpanStatus.Unavailable,
            SentryCocoa.SentrySpanStatus.DataLoss => SpanStatus.DataLoss,
            SentryCocoa.SentrySpanStatus.Unauthenticated => SpanStatus.Unauthenticated,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, message: default)
        };

    public static SentryCocoa.SentrySpanStatus ToCocoaSpanStatus(this SpanStatus? status) =>
        status switch
        {
            null => SentryCocoa.SentrySpanStatus.Undefined,
            SpanStatus.Ok => SentryCocoa.SentrySpanStatus.Ok,
            SpanStatus.Cancelled => SentryCocoa.SentrySpanStatus.Cancelled,
            SpanStatus.InternalError => SentryCocoa.SentrySpanStatus.InternalError,
            SpanStatus.UnknownError => SentryCocoa.SentrySpanStatus.UnknownError,
            SpanStatus.InvalidArgument => SentryCocoa.SentrySpanStatus.InvalidArgument,
            SpanStatus.DeadlineExceeded => SentryCocoa.SentrySpanStatus.DeadlineExceeded,
            SpanStatus.NotFound => SentryCocoa.SentrySpanStatus.NotFound,
            SpanStatus.AlreadyExists => SentryCocoa.SentrySpanStatus.AlreadyExists,
            SpanStatus.PermissionDenied => SentryCocoa.SentrySpanStatus.PermissionDenied,
            SpanStatus.ResourceExhausted => SentryCocoa.SentrySpanStatus.ResourceExhausted,
            SpanStatus.FailedPrecondition => SentryCocoa.SentrySpanStatus.FailedPrecondition,
            SpanStatus.Aborted => SentryCocoa.SentrySpanStatus.Aborted,
            SpanStatus.OutOfRange => SentryCocoa.SentrySpanStatus.OutOfRange,
            SpanStatus.Unimplemented => SentryCocoa.SentrySpanStatus.Unimplemented,
            SpanStatus.Unavailable => SentryCocoa.SentrySpanStatus.Unavailable,
            SpanStatus.DataLoss => SentryCocoa.SentrySpanStatus.DataLoss,
            SpanStatus.Unauthenticated => SentryCocoa.SentrySpanStatus.Unauthenticated,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, message: default)
        };

    public static TransactionNameSource ToTransactionNameSource(this SentryCocoa.TransactionNameSource source) =>
        source.Name() switch
        {
            SentryCocoa.TransactionNameSource.Custom => TransactionNameSource.Custom,
            SentryCocoa.TransactionNameSource.Url => TransactionNameSource.Url,
            SentryCocoa.TransactionNameSource.Route => TransactionNameSource.Route,
            SentryCocoa.TransactionNameSource.View => TransactionNameSource.View,
            SentryCocoa.TransactionNameSource.Task => TransactionNameSource.Task,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source.Name(), message: default)
        };

    public static SentryCocoa.TransactionNameSource ToCocoaTransactionNameSource(this TransactionNameSource source) =>
        source switch
        {
            TransactionNameSource.Custom => SentryCocoa.TransactionNameSource.Custom!,
            TransactionNameSource.Url => SentryCocoa.TransactionNameSource.Url!,
            TransactionNameSource.Route => SentryCocoa.TransactionNameSource.Route!,
            TransactionNameSource.View => SentryCocoa.TransactionNameSource.View!,
            TransactionNameSource.Task => SentryCocoa.TransactionNameSource.Task!,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, message: default)
        };

}
