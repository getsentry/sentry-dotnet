using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging.Tests;

public class SentryStructuredLoggerProviderTests
{
    [Fact]
    public void SmokeTest()
    {
        IOptions<SentryLoggingOptions> options = Options.Create(new SentryLoggingOptions
        {
            EnableLogs = true,
        });
        IHub hub = Substitute.For<IHub>();

        var provider = new SentryStructuredLoggerProvider(options, hub);

        ILogger logger = provider.CreateLogger("categoryName");

        logger.Should().BeOfType<SentryStructuredLogger>();

        provider.Dispose();
    }
}
