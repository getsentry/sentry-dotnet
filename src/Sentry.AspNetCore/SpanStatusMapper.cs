using Sentry.Protocol;

namespace Sentry.AspNetCore
{
    internal static class SpanStatusMapper
    {
        public static SpanStatus FromStatusCode(int statusCode) => statusCode switch
        {
            400 => SpanStatus.InvalidArgument,
            401 => SpanStatus.Unauthenticated,
            403 => SpanStatus.PermissionDenied,
            404 => SpanStatus.NotFound,
            409 => SpanStatus.AlreadyExists,
            429 => SpanStatus.ResourceExhausted,
            499 => SpanStatus.Cancelled,
            500 => SpanStatus.InternalError,
            501 => SpanStatus.Unimplemented,
            503 => SpanStatus.Unavailable,
            504 => SpanStatus.DeadlineExceeded,
            _   => SpanStatus.UnknownError
        };
    }
}
