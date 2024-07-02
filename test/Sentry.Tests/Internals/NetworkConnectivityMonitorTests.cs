namespace Sentry.Tests.Internals;

public class NetworkConnectivityMonitorTest
{
    [Fact]
    public void HostAvailable_CheckOnlyRunsOnce()
    {
        // Arrange
        var calls = 0;
        var mre = new ManualResetEvent(false);
        var pingHost = Substitute.For<IPingHost>();
        pingHost
            .IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        using var backoff = new NetworkConnectivityMonitor(pingHost, Callback);

        // Act
        mre.WaitOne();

        // Assert
        Assert.Equal(1, calls);
        pingHost.Received(1).IsAvailableAsync(Arg.Any<CancellationToken>());

        void Callback()
        {
            calls++;
            mre.Set();
        }
    }

    [Fact]
    public void HostUnavailable_ShouldIncreaseDelay()
    {
        // Arrange
        var callbacks = 0;
        var loops = 0;
        var mre = new ManualResetEvent(false);

        var pingHost = Substitute.For<IPingHost>();
        pingHost.When(x => x.IsAvailableAsync(Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                if (loops++ > 1)
                {
                    mre.Set();
                }
            });
        pingHost.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var initialDelay = 1000; // set initial delay to ease the testing
        var maxDelay = 2000; // set maximum delay to ease the testing

        using var backoff = new NetworkConnectivityMonitor(pingHost, Callback, initialDelay, maxDelay, x => x * 2);

        // Act
        mre.WaitOne();

        // Assert
        backoff._delayInMilliseconds.Should().Be(maxDelay);
        callbacks.Should().Be(0);

        void Callback()
        {
            callbacks++;
        }
    }
}
