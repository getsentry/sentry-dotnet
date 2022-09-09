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

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var listener = new MauiNetworkStatusListener(connectivity, _options);

        var task = listener.WaitForNetworkOnlineAsync(cts.Token);
        connectivity.ConnectivityChanged += Raise.EventWith(default, new ConnectivityChangedEventArgs(access, default));

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }

        // If we timed-out waiting (to simulate being offline), then cancellation will be requested
        Assert.Equal(expected, !cts.IsCancellationRequested);
    }

    [Fact]
    public async Task WaitForNetworkOnlineWhenAlreadyCancelled()
    {
        var connectivity = Substitute.For<IConnectivity>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var listener = new MauiNetworkStatusListener(connectivity, _options);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await listener.WaitForNetworkOnlineAsync(cts.Token));
    }
}
