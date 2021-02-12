using System.Net;

namespace Sentry
{
    /// <summary>
    /// Span status.
    /// </summary>
    public enum SpanStatus
    {
        /// <summary>The operation completed successfully.</summary>
        Ok,

        /// <summary>Deadline expired before operation could complete.</summary>
        DeadlineExceeded,

        /// <summary>401 Unauthorized (actually does mean unauthenticated according to RFC 7235).</summary>
        Unauthenticated,

        /// <summary>403 Forbidden</summary>
        PermissionDenied,

        /// <summary>404 Not Found. Some requested entity (file or directory) was not found.</summary>
        NotFound,

        /// <summary>429 Too Many Requests</summary>
        ResourceExhausted,

        /// <summary>Client specified an invalid argument. 4xx.</summary>
        InvalidArgument,

        /// <summary>501 Not Implemented</summary>
        Unimplemented,

        /// <summary>503 Service Unavailable</summary>
        Unavailable,

        /// <summary>Other/generic 5xx.</summary>
        InternalError,

        /// <summary>Unknown. Any non-standard HTTP status code.</summary>
        UnknownError,

        /// <summary>The operation was cancelled (typically by the user).</summary>
        Cancelled,

        /// <summary>Already exists (409).</summary>
        AlreadyExists,

        /// <summary>Operation was rejected because the system is not in a state required for the operation's</summary>
        FailedPrecondition,

        /// <summary>The operation was aborted, typically due to a concurrency issue.</summary>
        Aborted,

        /// <summary>Operation was attempted past the valid range.</summary>
        OutOfRange,

        /// <summary>Unrecoverable data loss or corruption</summary>
        DataLoss,
    }

    // Can't add static methods to enums unfortunately
    internal static class SpanStatusConverter
    {
        public static SpanStatus FromHttpStatusCode(int code) => code switch
        {
            < 400 => SpanStatus.Ok,
            400 => SpanStatus.InvalidArgument,
            401 => SpanStatus.Unauthenticated,
            403 => SpanStatus.PermissionDenied,
            404 => SpanStatus.NotFound,
            409 => SpanStatus.AlreadyExists,
            429 => SpanStatus.ResourceExhausted,
            499 => SpanStatus.Cancelled,
            < 500 => SpanStatus.InvalidArgument,
            500 => SpanStatus.InternalError,
            501 => SpanStatus.Unimplemented,
            503 => SpanStatus.Unavailable,
            504 => SpanStatus.DeadlineExceeded,
            < 600 => SpanStatus.InternalError,
            _ => SpanStatus.UnknownError
        };

        public static SpanStatus FromHttpStatusCode(HttpStatusCode code) =>
            FromHttpStatusCode((int)code);
    }
}
