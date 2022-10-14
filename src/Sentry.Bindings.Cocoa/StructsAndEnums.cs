using ObjCRuntime;

namespace Sentry.CocoaSdk;

[Native]
internal enum SentryLogLevel : long
{
    None = 1,
    Error,
    Debug,
    Verbose
}

[Native]
internal enum SentryLevel : ulong
{
    None = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5
}

[Native]
internal enum SentryPermissionStatus : long
{
    Unknown = 0,
    Granted,
    Partial,
    Denied
}

[Native]
internal enum SentryTransactionNameSource : long
{
    Custom = 0,
    Url,
    Route,
    View,
    Component,
    Task
}

[Native]
internal enum SentryAppStartType : ulong
{
    Warm,
    Cold,
    Unknown
}

[Native]
internal enum SentryError : long
{
    UnknownError = -1,
    InvalidDsnError = 100,
    SentryCrashNotInstalledError = 101,
    InvalidCrashReportError = 102,
    CompressionError = 103,
    JsonConversionError = 104,
    CouldNotFindDirectory = 105,
    RequestError = 106,
    EventNotSent = 107
}

// internal static class CFunctions
// {
//     // extern NSError * _Nullable NSErrorFromSentryError (SentryError error, NSString * _Nonnull description) __attribute__((visibility("default")));
//     [DllImport ("__Internal")]
//     [Verify (PlatformInvoke)]
//     [return: NullAllowed]
//     static extern NSError NSErrorFromSentryError (SentryError error, NSString description);

//     // extern NSString * _Nonnull nameForSentrySampleDecision (SentrySampleDecision decision);
//     [DllImport ("__Internal")]
//     [Verify (PlatformInvoke)]
//     static extern NSString nameForSentrySampleDecision (SentrySampleDecision decision);

//     // extern NSString * _Nonnull nameForSentrySpanStatus (SentrySpanStatus status);
//     [DllImport ("__Internal")]
//     [Verify (PlatformInvoke)]
//     static extern NSString nameForSentrySpanStatus (SentrySpanStatus status);
// }

[Native]
internal enum SentrySampleDecision : ulong
{
    Undecided,
    Yes,
    No
}

[Native]
internal enum SentrySpanStatus : ulong
{
    Undefined,
    Ok,
    DeadlineExceeded,
    Unauthenticated,
    PermissionDenied,
    NotFound,
    ResourceExhausted,
    InvalidArgument,
    Unimplemented,
    Unavailable,
    InternalError,
    UnknownError,
    Cancelled,
    AlreadyExists,
    FailedPrecondition,
    Aborted,
    OutOfRange,
    DataLoss
}

[Native]
internal enum SentrySessionStatus : ulong
{
    Ok = 0,
    Exited = 1,
    Crashed = 2,
    Abnormal = 3
}
