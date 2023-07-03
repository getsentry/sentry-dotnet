namespace Sentry.OpenTelemetry.Tests;

public class SentryOptionsExtensionsTests
{
    [Fact]
    public void IsSentryRequest_WithNullUri_ReturnsFalse()
    {
        var options = new SentryOptions();

        var actual = options.IsSentryRequest((Uri)null);

        Assert.False(actual);
    }

    [Fact]
    public void IsSentryRequest_WithEmptyUri_ReturnsFalse()
    {
        var options = new SentryOptions();

        var actual = options.IsSentryRequest(string.Empty);

        Assert.False(actual);
    }

    [Fact]
    public void IsSentryRequest_WithInvalidUri_ReturnsFalse()
    {
        var options = new SentryOptions
        {
            Dsn = "https://foo.com"
        };

        var actual = options.IsSentryRequest(new Uri("https://bar.com"));

        Assert.False(actual);
    }

    [Fact]
    public void IsSentryRequest_WithValidUri_ReturnsTrue()
    {
        var options = new SentryOptions
        {
            Dsn = "https://b887218a80114d26a9b1a51c5f88e0b4@o447951.ingest.sentry.io/6601807"
        };

        var actual = options.IsSentryRequest(new Uri("https://o447951.ingest.sentry.io/api/6601807/envelope/"));

        Assert.True(actual);
    }
}
