using Sentry.Integrations;

namespace Sentry.Internal
{
    // Unregistering is not supported by the unified API
    internal interface IInternalSdkIntegration : ISdkIntegration
    {
        /// <summary>
        /// Unregisters this integration with the hub.
        /// </summary>
        /// <remarks>
        /// This method is invoked when the Hub is disposed.
        /// </remarks>
        /// <param name="hub">The hub.</param>
        void Unregister(IHub hub);
    }
}
