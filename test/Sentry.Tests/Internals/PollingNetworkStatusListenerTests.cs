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
            .IsAvailableAsync()
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
        await pingHost.Received(1).IsAvailableAsync();
    }

    [Fact]
    public async Task HostUnavailable_ShouldIncreaseDelay()
    {
        // Arrange
        var initialDelay = 100; // set initial delay to ease the testing
        var pingHost = Substitute.For<IPing>();
        pingHost
            .IsAvailableAsync()
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
        await pingHost.Received().IsAvailableAsync();
        pollingListener._delayInMilliseconds.Should().BeGreaterThan(initialDelay);
    }
}
