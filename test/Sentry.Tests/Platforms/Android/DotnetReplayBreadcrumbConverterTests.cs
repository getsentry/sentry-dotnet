#if ANDROID
using Sentry.Android;

namespace Sentry.Tests.Platforms.Android;

public class DotnetReplayBreadcrumbConverterTests
{
    [Fact]
    public void Convert_HttpBreadcrumbWithStringTimestamps_ConvertsToNumeric()
    {
        // Arrange
        var options = new Sentry.JavaSdk.SentryOptions();
        var converter = new DotnetReplayBreadcrumbConverter(options);
        var breadcrumb = new Sentry.JavaSdk.Breadcrumb
        {
            Category = "http",
            Data =
            {
                { "url", "https://example.com" },
                { SentryHttpMessageHandler.HttpStartTimestampKey, "1625079600000" },
                { SentryHttpMessageHandler.HttpEndTimestampKey, "1625079660000" }
            }
        };

        // Act
        var rrwebEvent = converter.Convert(breadcrumb);

        // Assert
        rrwebEvent.Should().BeOfType<IO.Sentry.Rrweb.RRWebSpanEvent>();
        var rrWebSpanEvent = rrwebEvent as IO.Sentry.Rrweb.RRWebSpanEvent;
        Assert.NotNull(rrWebSpanEvent);
        // Note the converter divides by 1000 to get ms
        rrWebSpanEvent.StartTimestamp.Should().Be(1625079600L);
        rrWebSpanEvent.EndTimestamp.Should().Be(1625079660L);
    }
}
#endif
