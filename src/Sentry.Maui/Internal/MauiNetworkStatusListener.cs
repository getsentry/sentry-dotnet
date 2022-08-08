using Sentry.Extensibility;

namespace Sentry.Maui.Internal;

internal class MauiNetworkStatusListener : INetworkStatusListener
{
    private readonly IConnectivity _connectivity;
    private readonly bool _hasNetworkStatusPermission;

    public MauiNetworkStatusListener(IConnectivity connectivity, SentryOptions options)
    {
        _connectivity = connectivity;

        try
        {
            // Checking network access will throw if we don't have permission
            _ = connectivity.NetworkAccess;
            _hasNetworkStatusPermission = true;
        }
        catch (PermissionException)
        {
            _hasNetworkStatusPermission = false;
            options.DiagnosticLogger?.LogDebug("No network status permission.  Will assume device is online.");
        }
    }

    public bool Online => !_hasNetworkStatusPermission || TreatAsOnline(_connectivity.NetworkAccess);

    public async Task WaitForNetworkOnlineAsync(CancellationToken cancellationToken)
    {
        if (!_hasNetworkStatusPermission)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var tcs = new TaskCompletionSource();
        void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs args)
        {
            if (TreatAsOnline(args.NetworkAccess))
            {
                tcs.TrySetResult();
            }
        }

        _connectivity.ConnectivityChanged += OnConnectivityChanged;

        try
        {
            cancellationToken.Register(() => tcs.TrySetCanceled());
            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _connectivity.ConnectivityChanged -= OnConnectivityChanged;
        }
    }

    private static bool TreatAsOnline(NetworkAccess access) => access switch
    {
        // This is the expected status when we know we're online
        NetworkAccess.Internet => true,

        // If we can't tell, we should assume we're online
        NetworkAccess.Unknown => true,

        // Anything else means we know we can't reach the Internet, so we're offline
        _ => false
    };
}
