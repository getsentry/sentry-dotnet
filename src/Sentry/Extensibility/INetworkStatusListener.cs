namespace Sentry.Extensibility;

/// <summary>
/// Provides a mechanism to convey network status.
/// Used internally by some integrations. Not intended for public usage.
/// </summary>
/// <remarks>
/// This must be public because we use it in Sentry.Maui, which can't use InternalsVisibleTo
/// because MAUI assemblies are not strong-named.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface INetworkStatusListener
{
    /// <summary>
    /// Gets a value that indicates whether the network is online.
    /// </summary>
    public bool Online { get; }

    /// <summary>
    /// Asynchronously waits for the network to come online.
    /// </summary>
    /// <param name="cancellationToken">A token which cancels waiting.</param>
    public Task WaitForNetworkOnlineAsync(CancellationToken cancellationToken);
}
