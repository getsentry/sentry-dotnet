namespace Sentry.Cocoa.Extensions;

internal static class EnumExtensions
{
    // These align, so we can just cast
    public static SentryLevel ToSentryLevel(this CocoaSdk.SentryObjCLevel level) => (SentryLevel)level;

    public static CocoaSdk.SentryObjCLevel ToCocoaSentryLevel(this SentryLevel level) => level switch
    {
        SentryLevel.Debug => CocoaSdk.SentryObjCLevel.Debug,
        SentryLevel.Info => CocoaSdk.SentryObjCLevel.Info,
        SentryLevel.Warning => CocoaSdk.SentryObjCLevel.Warning,
        SentryLevel.Error => CocoaSdk.SentryObjCLevel.Error,
        SentryLevel.Fatal => CocoaSdk.SentryObjCLevel.Fatal,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };

    public static BreadcrumbLevel ToBreadcrumbLevel(this CocoaSdk.SentryObjCLevel level) =>
        level switch
        {
            CocoaSdk.SentryObjCLevel.Debug => BreadcrumbLevel.Debug,
            CocoaSdk.SentryObjCLevel.Info => BreadcrumbLevel.Info,
            CocoaSdk.SentryObjCLevel.Warning => BreadcrumbLevel.Warning,
            CocoaSdk.SentryObjCLevel.Error => BreadcrumbLevel.Error,
            CocoaSdk.SentryObjCLevel.Fatal => BreadcrumbLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };

    public static CocoaSdk.SentryObjCLevel ToCocoaSentryLevel(this BreadcrumbLevel level) =>
        level switch
        {
            BreadcrumbLevel.Debug => CocoaSdk.SentryObjCLevel.Debug,
            BreadcrumbLevel.Info => CocoaSdk.SentryObjCLevel.Info,
            BreadcrumbLevel.Warning => CocoaSdk.SentryObjCLevel.Warning,
            BreadcrumbLevel.Error => CocoaSdk.SentryObjCLevel.Error,
            BreadcrumbLevel.Fatal => CocoaSdk.SentryObjCLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, message: default)
        };

    public static bool? ToNullableBoolean(this CocoaSdk.SentryObjCSampleDecision decision) =>
        decision switch
        {
            CocoaSdk.SentryObjCSampleDecision.Yes => true,
            CocoaSdk.SentryObjCSampleDecision.No => false,
            CocoaSdk.SentryObjCSampleDecision.Undecided => null,
            _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, null)
        };

    public static CocoaSdk.SentryObjCSampleDecision ToCocoaSampleDecision(this bool? decision) =>
        decision switch
        {
            true => CocoaSdk.SentryObjCSampleDecision.Yes,
            false => CocoaSdk.SentryObjCSampleDecision.No,
            null => CocoaSdk.SentryObjCSampleDecision.Undecided
        };

    public static SpanStatus? ToSpanStatus(this CocoaSdk.SentryObjCSpanStatus status) =>
        status switch
        {
            CocoaSdk.SentryObjCSpanStatus.Undefined => null,
            CocoaSdk.SentryObjCSpanStatus.Ok => SpanStatus.Ok,
            CocoaSdk.SentryObjCSpanStatus.Cancelled => SpanStatus.Cancelled,
            CocoaSdk.SentryObjCSpanStatus.InternalError => SpanStatus.InternalError,
            CocoaSdk.SentryObjCSpanStatus.UnknownError => SpanStatus.UnknownError,
            CocoaSdk.SentryObjCSpanStatus.InvalidArgument => SpanStatus.InvalidArgument,
            CocoaSdk.SentryObjCSpanStatus.DeadlineExceeded => SpanStatus.DeadlineExceeded,
            CocoaSdk.SentryObjCSpanStatus.NotFound => SpanStatus.NotFound,
            CocoaSdk.SentryObjCSpanStatus.AlreadyExists => SpanStatus.AlreadyExists,
            CocoaSdk.SentryObjCSpanStatus.PermissionDenied => SpanStatus.PermissionDenied,
            CocoaSdk.SentryObjCSpanStatus.ResourceExhausted => SpanStatus.ResourceExhausted,
            CocoaSdk.SentryObjCSpanStatus.FailedPrecondition => SpanStatus.FailedPrecondition,
            CocoaSdk.SentryObjCSpanStatus.Aborted => SpanStatus.Aborted,
            CocoaSdk.SentryObjCSpanStatus.OutOfRange => SpanStatus.OutOfRange,
            CocoaSdk.SentryObjCSpanStatus.Unimplemented => SpanStatus.Unimplemented,
            CocoaSdk.SentryObjCSpanStatus.Unavailable => SpanStatus.Unavailable,
            CocoaSdk.SentryObjCSpanStatus.DataLoss => SpanStatus.DataLoss,
            CocoaSdk.SentryObjCSpanStatus.Unauthenticated => SpanStatus.Unauthenticated,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, message: default)
        };

    public static CocoaSdk.SentryObjCSpanStatus ToCocoaSpanStatus(this SpanStatus? status) =>
        status switch
        {
            null => CocoaSdk.SentryObjCSpanStatus.Undefined,
            SpanStatus.Ok => CocoaSdk.SentryObjCSpanStatus.Ok,
            SpanStatus.Cancelled => CocoaSdk.SentryObjCSpanStatus.Cancelled,
            SpanStatus.InternalError => CocoaSdk.SentryObjCSpanStatus.InternalError,
            SpanStatus.UnknownError => CocoaSdk.SentryObjCSpanStatus.UnknownError,
            SpanStatus.InvalidArgument => CocoaSdk.SentryObjCSpanStatus.InvalidArgument,
            SpanStatus.DeadlineExceeded => CocoaSdk.SentryObjCSpanStatus.DeadlineExceeded,
            SpanStatus.NotFound => CocoaSdk.SentryObjCSpanStatus.NotFound,
            SpanStatus.AlreadyExists => CocoaSdk.SentryObjCSpanStatus.AlreadyExists,
            SpanStatus.PermissionDenied => CocoaSdk.SentryObjCSpanStatus.PermissionDenied,
            SpanStatus.ResourceExhausted => CocoaSdk.SentryObjCSpanStatus.ResourceExhausted,
            SpanStatus.FailedPrecondition => CocoaSdk.SentryObjCSpanStatus.FailedPrecondition,
            SpanStatus.Aborted => CocoaSdk.SentryObjCSpanStatus.Aborted,
            SpanStatus.OutOfRange => CocoaSdk.SentryObjCSpanStatus.OutOfRange,
            SpanStatus.Unimplemented => CocoaSdk.SentryObjCSpanStatus.Unimplemented,
            SpanStatus.Unavailable => CocoaSdk.SentryObjCSpanStatus.Unavailable,
            SpanStatus.DataLoss => CocoaSdk.SentryObjCSpanStatus.DataLoss,
            SpanStatus.Unauthenticated => CocoaSdk.SentryObjCSpanStatus.Unauthenticated,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, message: default)
        };

    // These align, so we can just cast
    public static TransactionNameSource ToTransactionNameSource(this CocoaSdk.SentryObjCTransactionNameSource source) =>
        (TransactionNameSource)source;
    public static CocoaSdk.SentryObjCTransactionNameSource ToCocoaTransactionNameSource(this TransactionNameSource source) =>
        (CocoaSdk.SentryObjCTransactionNameSource)source;
}
