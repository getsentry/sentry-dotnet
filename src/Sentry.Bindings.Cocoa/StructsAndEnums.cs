// -----------------------------------------------------------------------------
// This file is auto-generated by Objective Sharpie and patched via the script
// at /scripts/generate-cocoa-bindings.ps1.  Do not edit this file directly.
// If changes are required, update the script instead.
// -----------------------------------------------------------------------------

using System.Runtime.InteropServices;
using Foundation;
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
    EventNotSent = 107,
    FileIO = 108,
    Kernel = 109
}

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
internal enum SentryAppStartType : ulong
{
    Warm,
    Cold,
    Unknown
}
