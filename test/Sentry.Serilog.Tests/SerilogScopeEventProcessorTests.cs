using Microsoft.Extensions.Logging;

namespace Sentry.Serilog.Tests;

public class SerilogScopeEventProcessorTests
{
    [Theory]
    [InlineData("42", "42")]
    [InlineData(42, "42")]
    public void Emit_WithException_CreatesEventWithException(object value, string expected)
    {
        // Arrange
        var options = new SentryOptions();
        var sut = new SerilogScopeEventProcessor(options);

        using var log = new LoggerConfiguration().CreateLogger();
        var factory = new LoggerFactory().AddSerilog(log);
        var logger = factory.CreateLogger<SerilogScopeEventProcessorTests>();

        // Act
        SentryEvent evt;
        using (logger.BeginScope(new Dictionary<string, object> { ["Answer"] = value }))
        {
            evt = new SentryEvent();
            sut.Process(evt);
        }

        // Assert
        evt.Tags.Should().ContainKey("Answer");
        evt.Tags["Answer"].Should().Be(expected);
    }
}
