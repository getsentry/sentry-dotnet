using NSubstitute.ExceptionExtensions;
using Sentry.Maui.Internal;
using Sentry.Testing;

namespace Sentry.Maui.Tests;

public class MauiNetworkStatusListenerTests
{
    private readonly SentryOptions _options;

    public MauiNetworkStatusListenerTests(ITestOutputHelper output)
    {
        _options = new SentryOptions {DiagnosticLogger = new TestOutputDiagnosticLogger(output)};
    }

    [Fact]
    public void OnlineReturnsTrueWhenNoPermission()
    {
        var connectivity = Substitute.For<IConnectivity>();
        connectivity.NetworkAccess.Throws(new PermissionException(default));

        var listener = new MauiNetworkStatusListener(connectivity, _options);
        Assert.True(listener.Online);
    }

    [Fact]
    public async Task WaitForNetworkOnlineReturnsImmediatelyWhenNoPermission()
    {
        var connectivity = Substitute.For<IConnectivity>();
        connectivity.NetworkAccess.Throws(new PermissionException(default));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var listener = new MauiNetworkStatusListener(connectivity, _options);
        await listener.WaitForNetworkOnlineAsync(cts.Token);
        Assert.False(cts.IsCancellationRequested);
    }

    [Theory]
    [InlineData(NetworkAccess.Internet, true)]
    [InlineData(NetworkAccess.Unknown, true)]
    [InlineData(NetworkAccess.None, false)]
    [InlineData(NetworkAccess.Local, false)]
    [InlineData(NetworkAccess.ConstrainedInternet, false)]
    public void OnlineByNetworkAccessType(NetworkAccess access, bool expected)
    {
        var connectivity = Substitute.For<IConnectivity>();
        connectivity.NetworkAccess.Returns(access);

        var listener = new MauiNetworkStatusListener(connectivity, _options);
        Assert.Equal(expected, listener.Online);
    }

    [Theory]
    [InlineData(NetworkAccess.Internet, true)]
    [InlineData(NetworkAccess.Unknown, true)]
    [InlineData(NetworkAccess.None, false)]
    [InlineData(NetworkAccess.Local, false)]
    [InlineData(NetworkAccess.ConstrainedInternet, false)]
    public async Task WaitForNetworkOnlineByNetworkAccessType(NetworkAccess access, bool expected)
    {
        var connectivity = Substitute.For<IConnectivity>();
        connectivity.NetworkAccess.Returns(access);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var listener = new MauiNetworkStatusListener(connectivity, _options);

            await Task.WhenAll(
                listener.WaitForNetworkOnlineAsync(cts.Token),
                Task.Run(async () =>
                {
                    // Yield to make sure the first task runs and is waiting before we raise the event
                    await Task.Yield();
                    connectivity.ConnectivityChanged +=
                        Raise.EventWith(default, new ConnectivityChangedEventArgs(access, default));
                }, cts.Token));

            // If we timed-out waiting (to simulate being offline), then cancellation will be requested
            Assert.Equal(expected, !cts.IsCancellationRequested);

            // No point in waiting any longer
            cts.Cancel();
        }
        catch (OperationCanceledException)
        {
        }
    }
}
