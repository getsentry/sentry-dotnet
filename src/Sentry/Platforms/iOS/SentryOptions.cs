namespace Sentry;

public partial class SentryOptions
{
    /// <summary>
    /// Exposes additional options for the iOS platform.
    /// </summary>
    public IOSOptions IOS { get; } = new();

    /// <summary>
    /// Provides additional options for the iOS platform.
    /// </summary>
    public class IOSOptions
    {
        internal IOSOptions() { }

        // TODO
    }
}
