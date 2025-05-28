namespace Sentry.Integrations;

/// <summary>
/// An SDK Integration.
/// </summary>
public interface ISdkIntegration
{
    /// <summary>
    /// Registers this integration with the hub.
    /// </summary>
    /// <remarks>
    /// This method is invoked when the Hub is created.
    /// </remarks>
    /// <param name="hub">The hub.</param>
    /// <param name="options">The options.</param>
    public void Register(IHub hub, SentryOptions options);
}
