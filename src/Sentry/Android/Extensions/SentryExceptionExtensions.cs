using Sentry.Protocol;

namespace Sentry.Android.Extensions;

internal static class SentryExceptionExtensions
{
    public static SentryException ToSentryException(this Java.Protocol.SentryException exception) =>
        new()
        {
            Type = exception.Type,
            Value = exception.Value,
            Module = exception.Module,
            ThreadId = exception.ThreadId?.IntValue() ?? 0,
            Stacktrace = exception.Stacktrace?.ToSentryStackTrace(),
            Mechanism = exception.Mechanism?.ToMechanism()
        };

    public static Java.Protocol.SentryException ToJavaSentryException(this SentryException exception) =>
        new()
        {
            Type = exception.Type,
            Value = exception.Value,
            Module = exception.Module,
            ThreadId = exception.ThreadId == default ? null : new JavaLong(exception.ThreadId),
            Stacktrace = exception.Stacktrace?.ToJavaSentryStackTrace(),
            Mechanism = exception.Mechanism?.ToJavaMechanism()
        };
}
