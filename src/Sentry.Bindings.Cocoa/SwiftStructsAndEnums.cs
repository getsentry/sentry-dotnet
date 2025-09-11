/*
 * This file defines iOS API contracts for enums we need from Sentry-Swift.h.
 * Note that we are **not** using Objective Sharpie to generate these contracts (instead they're maintained manually).
 */
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using Sentry;

namespace Sentry.CocoaSdk;

[Native]
internal enum SentryFeedbackSource : long
{
    Widget = 0,
    Custom = 1
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
internal enum SentryStructuredLogLevel : long
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warn = 3,
    Error = 4,
    Fatal = 5
}

[Native]
internal enum SentryProfileLifecycle : long
{
    Manual = 0,
    Trace = 1
}

[Native]
internal enum SentryReplayQuality : long
{
    Low = 0,
    Medium = 1,
    High = 2
}

[Native]
internal enum SentryReplayType : long
{
    Session = 0,
    Buffer = 1
}

[Native]
internal enum SentryRRWebEventType : long
{
    None = 0,
    Touch = 3,
    Meta = 4,
    Custom = 5
}

[Native]
internal enum SentryTransactionNameSource : long
{
    Custom = 0,
    Url = 1,
    Route = 2,
    View = 3,
    Component = 4,
    Task = 5
}
