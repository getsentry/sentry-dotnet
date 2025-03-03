namespace Sentry.Cocoa.Extensions;

internal static class EnumExtensions
{
    // These align, so we can just cast
    public static SentryLevel ToSentryLevel(this CocoaSdk.SentryLevel level) => (SentryLevel)level;

    public static CocoaSdk.SentryLevel ToCocoaSentryLevel(this SentryLevel level) => level switch
    {
        SentryLevel.Debug => CocoaSdk.SentryLevel.Debug,
        SentryLevel.Info => CocoaSdk.SentryLevel.Info,
        SentryLevel.Warning => CocoaSdk.SentryLevel.Warning,
        SentryLevel.Error => CocoaSdk.SentryLevel.Error,
        SentryLevel.Fatal => CocoaSdk.SentryLevel.Fatal
    };

    public static BreadcrumbLevel ToBreadcrumbLevel(this CocoaSdk.SentryLevel level) =>
        level switch
        {
            CocoaSdk.SentryLevel.Debug => BreadcrumbLevel.Debug,
            CocoaSdk.SentryLevel.Info => BreadcrumbLevel.Info,
            CocoaSdk.SentryLevel.Warning => BreadcrumbLevel.Warning,
            CocoaSdk.SentryLevel.Error => BreadcrumbLevel.Error,
            CocoaSdk.SentryLevel.Fatal => BreadcrumbLevel.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };

    public static CocoaSdk.SentryLevel ToCocoaSentryLevel(this BreadcrumbLevel level) =>
        level switch
        {
            BreadcrumbLevel.Debug => CocoaSdk.SentryLevel.Debug,
            BreadcrumbLevel.Info => CocoaSdk.SentryLevel.Info,
            BreadcrumbLevel.Warning => CocoaSdk.SentryLevel.Warning,
            BreadcrumbLevel.Error => CocoaSdk.SentryLevel.Error,
            BreadcrumbLevel.Critical => CocoaSdk.SentryLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, message: default)
        };

    public static bool? ToNullableBoolean(this CocoaSdk.SentrySampleDecision decision) =>
        decision switch
        {
            CocoaSdk.SentrySampleDecision.Yes => true,
            CocoaSdk.SentrySampleDecision.No => false,
            CocoaSdk.SentrySampleDecision.Undecided => null,
            _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, null)
        };

    public static CocoaSdk.SentrySampleDecision ToCocoaSampleDecision(this bool? decision) =>
        decision switch
        {
            true => CocoaSdk.SentrySampleDecision.Yes,
            false => CocoaSdk.SentrySampleDecision.No,
            null => CocoaSdk.SentrySampleDecision.Undecided
        };

    public static SpanStatus? ToSpanStatus(this CocoaSdk.SentrySpanStatus status) =>
        status switch
        {
            CocoaSdk.SentrySpanStatus.Undefined => null,
            CocoaSdk.SentrySpanStatus.Ok => SpanStatus.Ok,
            CocoaSdk.SentrySpanStatus.Cancelled => SpanStatus.Cancelled,
            CocoaSdk.SentrySpanStatus.InternalError => SpanStatus.InternalError,
            CocoaSdk.SentrySpanStatus.UnknownError => SpanStatus.UnknownError,
            CocoaSdk.SentrySpanStatus.InvalidArgument => SpanStatus.InvalidArgument,
            CocoaSdk.SentrySpanStatus.DeadlineExceeded => SpanStatus.DeadlineExceeded,
            CocoaSdk.SentrySpanStatus.NotFound => SpanStatus.NotFound,
            CocoaSdk.SentrySpanStatus.AlreadyExists => SpanStatus.AlreadyExists,
            CocoaSdk.SentrySpanStatus.PermissionDenied => SpanStatus.PermissionDenied,
            CocoaSdk.SentrySpanStatus.ResourceExhausted => SpanStatus.ResourceExhausted,
            CocoaSdk.SentrySpanStatus.FailedPrecondition => SpanStatus.FailedPrecondition,
            CocoaSdk.SentrySpanStatus.Aborted => SpanStatus.Aborted,
            CocoaSdk.SentrySpanStatus.OutOfRange => SpanStatus.OutOfRange,
            CocoaSdk.SentrySpanStatus.Unimplemented => SpanStatus.Unimplemented,
            CocoaSdk.SentrySpanStatus.Unavailable => SpanStatus.Unavailable,
            CocoaSdk.SentrySpanStatus.DataLoss => SpanStatus.DataLoss,
            CocoaSdk.SentrySpanStatus.Unauthenticated => SpanStatus.Unauthenticated,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, message: default)
        };

    public static CocoaSdk.SentrySpanStatus ToCocoaSpanStatus(this SpanStatus? status) =>
        status switch
        {
            null => CocoaSdk.SentrySpanStatus.Undefined,
            SpanStatus.Ok => CocoaSdk.SentrySpanStatus.Ok,
            SpanStatus.Cancelled => CocoaSdk.SentrySpanStatus.Cancelled,
            SpanStatus.InternalError => CocoaSdk.SentrySpanStatus.InternalError,
            SpanStatus.UnknownError => CocoaSdk.SentrySpanStatus.UnknownError,
            SpanStatus.InvalidArgument => CocoaSdk.SentrySpanStatus.InvalidArgument,
            SpanStatus.DeadlineExceeded => CocoaSdk.SentrySpanStatus.DeadlineExceeded,
            SpanStatus.NotFound => CocoaSdk.SentrySpanStatus.NotFound,
            SpanStatus.AlreadyExists => CocoaSdk.SentrySpanStatus.AlreadyExists,
            SpanStatus.PermissionDenied => CocoaSdk.SentrySpanStatus.PermissionDenied,
            SpanStatus.ResourceExhausted => CocoaSdk.SentrySpanStatus.ResourceExhausted,
            SpanStatus.FailedPrecondition => CocoaSdk.SentrySpanStatus.FailedPrecondition,
            SpanStatus.Aborted => CocoaSdk.SentrySpanStatus.Aborted,
            SpanStatus.OutOfRange => CocoaSdk.SentrySpanStatus.OutOfRange,
            SpanStatus.Unimplemented => CocoaSdk.SentrySpanStatus.Unimplemented,
            SpanStatus.Unavailable => CocoaSdk.SentrySpanStatus.Unavailable,
            SpanStatus.DataLoss => CocoaSdk.SentrySpanStatus.DataLoss,
            SpanStatus.Unauthenticated => CocoaSdk.SentrySpanStatus.Unauthenticated,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, message: default)
        };

    // These align, so we can just cast
    public static TransactionNameSource ToTransactionNameSource(this CocoaSdk.SentryTransactionNameSource source) =>
        (TransactionNameSource)source;
    public static CocoaSdk.SentryTransactionNameSource ToCocoaTransactionNameSource(this TransactionNameSource source) =>
        (CocoaSdk.SentryTransactionNameSource)source;
}
