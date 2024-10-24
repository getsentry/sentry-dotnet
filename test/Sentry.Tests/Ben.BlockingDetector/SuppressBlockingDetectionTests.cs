using Sentry.Ben.BlockingDetector;

namespace Sentry.Tests.Ben.BlockingDetector;

public class SuppressBlockingDetectionTests
{
    [Fact]
    public void Constructor_SuppressesBlockingDetection()
    {
        // Arrange
        var listenerState = Substitute.For<ITaskBlockingListenerState>();
        var monitor = Substitute.For<IBlockingMonitor>();
        var context = new DetectBlockingSynchronizationContext(monitor);

        // Act
        using (new SuppressBlockingDetection(context, listenerState))
        {
            // Assert
            listenerState.Received(1).Suppress();
            context._isSuppressed.Should().Be(1);
        }
    }
}
