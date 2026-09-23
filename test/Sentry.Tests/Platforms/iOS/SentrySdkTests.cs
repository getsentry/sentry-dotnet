using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Sentry.Tests.Platforms.iOS;

public class SentrySdkTests
{
    [Fact]
    public void ProcessOnBeforeSend_BeforeSendThrows_DropsNativeEventAndLogsError()
    {
        var exception = new InvalidOperationException("callback failed");
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        options.SetBeforeSend((SentryEvent _, SentryHint _) => throw exception);

        var result = SentrySdk.ProcessOnBeforeSend(options, new CocoaSdk.SentryObjCEvent(), Substitute.For<IHub>());

        result.Should().BeNull();
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == SentryLevel.Error &&
            entry.Exception == exception &&
            entry.Message == "The BeforeSend callback threw an exception. The event will be dropped.");
    }

    [Fact]
    public void ProcessOnBeforeSend_BeforeSendReturnsEvent_KeepsNativeEvent()
    {
        var options = new SentryOptions();
        options.SetBeforeSend((SentryEvent @event, SentryHint _) => @event);
        var native = new CocoaSdk.SentryObjCEvent();

        var result = SentrySdk.ProcessOnBeforeSend(options, native, Substitute.For<IHub>());

        result.Should().BeSameAs(native);
    }
}
