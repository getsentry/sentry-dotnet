namespace Sentry.Integrations;

/// <summary>
/// Since we can't add methods to <see cref="ISdkIntegration"/> without it being  a breaking change, this interface
/// allows us to add some cleanup logic as an internal interface. We can move this to <see cref="ISdkIntegration"/>
/// in the next major release.
/// </summary>
internal interface ITidySdkIntegration: ISdkIntegration
{
    /// <summary>
    /// Performs any necessary cleanup for the integration
    /// </summary>
    /// <remarks>
    /// Called when the Hub is disposed
    /// </remarks>
    public void Cleanup();
}
