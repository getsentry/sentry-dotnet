namespace Sentry.Tests.Internals;

public class PollingNetworkStatusListenerTest
{
    [Fact]
    public async Task HostAvailable_CheckOnlyRunsOnce()
    {
        // Arrange
        var initialDelay = 100;
        var pingHost = Substitute.For<IPing>();
        pingHost
            .IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var pollingListener = new PollingNetworkStatusListener(pingHost, initialDelay);
        pollingListener.Online = false;

        // Act
        var waitForNetwork = pollingListener.WaitForNetworkOnlineAsync();
        var timeout = Task.Delay(1000);
        var completedTask = await Task.WhenAny(waitForNetwork, timeout);

        // Assert
        completedTask.Should().Be(waitForNetwork);
        pollingListener.Online.Should().Be(true);
        await pingHost.Received(1).IsAvailableAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HostUnavailable_ShouldIncreaseDelay()
    {
        // Arrange
        var initialDelay = 100; // set initial delay to ease the testing
        var pingHost = Substitute.For<IPing>();
        pingHost
            .IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var pollingListener = new PollingNetworkStatusListener(pingHost, initialDelay);
        pollingListener.Online = false;

        // Act
        var waitForNetwork = pollingListener.WaitForNetworkOnlineAsync();
        var timeout = Task.Delay(2000);
        var completedTask = await Task.WhenAny(waitForNetwork, timeout);

        // Assert
        completedTask.Should().Be(timeout);
        pollingListener.Online.Should().Be(false);
        await pingHost.Received().IsAvailableAsync(Arg.Any<CancellationToken>());
        pollingListener._delayInMilliseconds.Should().BeGreaterThan(initialDelay);
    }

    [Fact]
    public async Task OperationCancelled_ShouldExitGracefully()
    {
        // Arrange
        const int initialDelay = 10_000;
        var pingHost = Substitute.For<IPing>();
        pingHost
            .IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var pollingListener = new PollingNetworkStatusListener(pingHost, initialDelay)
        {
            Online = false
        };
        var cts = new CancellationTokenSource();

        // Act
        var waitForNetwork = pollingListener.WaitForNetworkOnlineAsync(cts.Token);
        var timeout = Task.Delay(2000);
        cts.CancelAfter(100);
        var completedTask = await Task.WhenAny(waitForNetwork, timeout);

        // Assert
        completedTask.Should().Be(waitForNetwork);
        pollingListener.Online.Should().Be(false);
        await completedTask; // Throws exception if the task is faulted
    }
}
