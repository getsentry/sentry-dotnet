namespace Sentry.Protocol.Spans;

internal static class SpanV2Attributes
{
    /// <summary>The span op (e.g., "http.client", "db.query") of the span</summary>
    public const string Operation = "sentry.op";

    /// <summary>The release version of the application</summary>
    public const string Release = "sentry.release";

    /// <summary>The environment name (e.g., "production", "staging", "development")</summary>
    public const string Environment = "sentry.environment";

    /// <summary>The segment name (e.g., "GET /users")</summary>
    public const string SegmentName = "sentry.segment.name";

    /// <summary>The segment span id</summary>
    public const string SegmentId = "sentry.segment.id";

    /// <summary>The source of the span name. MUST be set on segment spans, MAY be set on child spans.</summary>
    public const string Source = "sentry.span.source";

    /// <summary>The id of the currently running profiler (continuous profiling)</summary>
    public const string ProfilerId = "sentry.profiler_id";

    /// <summary>The id of the currently running replay (if available)</summary>
    public const string ReplayId = "sentry.replay_id";

    /// <summary>The operating system name (e.g., "Linux", "Windows", "macOS")</summary>
    public const string OSName = "os.name";

    /// <summary>The browser name (e.g., "Chrome", "Firefox", "Safari")</summary>
    public const string BrowserName = "browser.name";

    /// <summary>The user ID (gated by sendDefaultPii)</summary>
    public const string UserId = "user.id";

    /// <summary>The user email (gated by sendDefaultPii)</summary>
    public const string UserEmail = "user.email";

    /// <summary>The user IP address (gated by sendDefaultPii)</summary>
    public const string UserIpAddress = "user.ip_address";

    /// <summary>The user username (gated by sendDefaultPii)</summary>
    public const string UserName = "user.name";

    /// <summary>The thread ID</summary>
    public const string ThreadId = "thread.id";

    /// <summary>The thread name</summary>
    public const string ThreadName = "thread.name";

    /// <summary>Name of the Sentry SDK (e.g., "sentry.php", "sentry.javascript")</summary>
    public const string SDKName = "sentry.sdk.name";

    /// <summary>Version of the Sentry SDK</summary>
    public const string SDKVersion = "sentry.sdk.version";
}
