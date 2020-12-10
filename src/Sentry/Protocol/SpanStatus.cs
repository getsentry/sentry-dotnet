namespace Sentry.Protocol
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
}
