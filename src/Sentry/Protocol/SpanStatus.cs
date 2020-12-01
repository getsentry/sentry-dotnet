using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// Span status.
    /// </summary>
    public enum SpanStatus
    {
        /// <summary>The operation completed successfully.</summary>
        [EnumMember(Value = "ok")]
        Ok,

        /// <summary>Deadline expired before operation could complete.</summary>
        [EnumMember(Value = "deadlineExceeded")]
        DeadlineExceeded,

        /// <summary>401 Unauthorized (actually does mean unauthenticated according to RFC 7235).</summary>
        [EnumMember(Value = "unauthenticated")]
        Unauthenticated,

        /// <summary>403 Forbidden</summary>
        [EnumMember(Value = "permissionDenied")]
        PermissionDenied,

        /// <summary>404 Not Found. Some requested entity (file or directory) was not found.</summary>
        [EnumMember(Value = "notFound")]
        NotFound,

        /// <summary>429 Too Many Requests</summary>
        [EnumMember(Value = "resourceExhausted")]
        ResourceExhausted,

        /// <summary>Client specified an invalid argument. 4xx.</summary>
        [EnumMember(Value = "invalidArgument")]
        InvalidArgument,

        /// <summary>501 Not Implemented</summary>
        [EnumMember(Value = "unimplemented")]
        Unimplemented,

        /// <summary>503 Service Unavailable</summary>
        [EnumMember(Value = "unavailable")]
        Unavailable,

        /// <summary>Other/generic 5xx.</summary>
        [EnumMember(Value = "internalError")]
        InternalError,

        /// <summary>Unknown. Any non-standard HTTP status code.</summary>
        [EnumMember(Value = "unknownError")]
        UnknownError,

        /// <summary>The operation was cancelled (typically by the user).</summary>
        [EnumMember(Value = "cancelled")]
        Cancelled,

        /// <summary>Already exists (409).</summary>
        [EnumMember(Value = "alreadyExists")]
        AlreadyExists,

        /// <summary>Operation was rejected because the system is not in a state required for the operation's</summary>
        [EnumMember(Value = "failedPrecondition")]
        FailedPrecondition,

        /// <summary>The operation was aborted, typically due to a concurrency issue.</summary>
        [EnumMember(Value = "aborted")]
        Aborted,

        /// <summary>Operation was attempted past the valid range.</summary>
        [EnumMember(Value = "outOfRange")]
        OutOfRange,

        /// <summary>Unrecoverable data loss or corruption</summary>
        [EnumMember(Value = "dataLoss")]
        DataLoss,
    }
}
