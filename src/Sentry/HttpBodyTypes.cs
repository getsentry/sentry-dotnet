namespace Sentry;

/// <summary>
/// The HTTP bodies that may be attached to telemetry.
/// </summary>
[Flags]
public enum HttpBodyTypes
{
    /// <summary>
    /// No bodies are attached.
    /// </summary>
    None = 0,

    /// <summary>
    /// Bodies of requests received by this application.
    /// </summary>
    IncomingRequest = 1,

    /// <summary>
    /// Bodies of requests this application sends to other services.
    /// </summary>
    OutgoingRequest = 1 << 1,

    /// <summary>
    /// Bodies of responses this application returns.
    /// </summary>
    IncomingResponse = 1 << 2,

    /// <summary>
    /// Bodies of responses this application receives from other services.
    /// </summary>
    OutgoingResponse = 1 << 3,

    /// <summary>
    /// Every body type supported by the platform.
    /// </summary>
    All = IncomingRequest | OutgoingRequest | IncomingResponse | OutgoingResponse
}
