// ReSharper disable once CheckNamespace
namespace Sentry;

public partial class SentryOptions
{
    /// <summary>
    /// Exposes additional options for the iOS platform.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public IosOptions iOS { get; } = new();

    /// <summary>
    /// Provides additional options for the iOS platform.
    /// </summary>
    public class IosOptions
    {
        internal IosOptions() { }

        // TODO
    }
}
