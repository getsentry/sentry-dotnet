namespace Sentry.Tests.Internals;

public class ProgressiveBackoffTest
{
    [Fact]
    public void CheckReturnsTrue_OnlyRunsOnce()
    {
        // Arrange
        var calls = 0;
        var mre = new ManualResetEvent(false);

        using var backoff = new ProgressiveBackoff(Check);

        // Act
        mre.WaitOne();

        // Assert
        Assert.Equal(1, calls);

        async Task<bool> Check(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            calls++;
            mre.Set();
            return true;
        }
    }

    [Fact]
    public void CheckReturnsFalse_ShouldIncreaseDelay()
    {
        // Arrange
        var calls = 0;
        var mre = new ManualResetEvent(false);

        var initialDelay = 1000; // set initial delay to ease the testing
        var maxDelay = 2000; // set maximum delay to ease the testing

        using var backoff = new ProgressiveBackoff(Check, initialDelay, maxDelay, x => x * 2);

        // Act
        mre.WaitOne();

        // Assert
        backoff._delayInMilliseconds.Should().Be(maxDelay);

        async Task<bool> Check(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            if (calls++ > 1)
            {
                mre.Set();
            }
            return false;
        }
    }
}
