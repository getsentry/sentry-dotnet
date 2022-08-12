// ReSharper disable once CheckNamespace
namespace Sentry;

public partial class SentryOptions
{
    /// <summary>
    /// Exposes additional options for the iOS platform.
    /// </summary>
    public IOSOptions IOS { get; }

    /// <summary>
    /// Provides additional options for the iOS platform.
    /// </summary>
    public class IOSOptions
    {
        private readonly SentryOptions _options;

        internal IOSOptions(SentryOptions options)
        {
            _options = options;
        }

        // TODO
    }
}
