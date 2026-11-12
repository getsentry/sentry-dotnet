#if ANDROID
namespace Sentry.Tests.Platforms.Android;

public class SentrySdkTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void CreateBeforeSendCallback_NoEnabledFilter_ReturnsNull(bool enabled, bool hasFilter)
    {
        var options = new SentryOptions();
        options.Android.SuppressSegfaults = false;
        options.Native.EnableBeforeSend = enabled;
        if (hasFilter)
        {
            options.SetBeforeSend((evt, _) => evt);
        }
        using var javaOptions = new JavaSdk.SentryOptions();

        SentrySdk.CreateBeforeSendCallback(options, javaOptions).Should().BeNull();
    }

    [Fact]
    public void CreateBeforeSendCallback_SuppressionWithDisabledFilter_ReturnsUnconvertedEventWithoutCallingFilter()
    {
        var options = new SentryOptions();
        options.Android.SuppressSegfaults = true;
        options.Native.EnableBeforeSend = false;
        var called = false;
        options.SetBeforeSend((evt, _) =>
        {
            called = true;
            return evt;
        });
        using var javaOptions = new JavaSdk.SentryOptions();
        using var callback = SentrySdk.CreateBeforeSendCallback(options, javaOptions);
        using var evt = new JavaSdk.SentryEvent
        {
            EventId = new JavaSdk.Protocol.SentryId(new string('z', 32))
        };
        using var hint = new JavaSdk.Hint();

        var result = callback!.Execute(evt, hint);

        called.Should().BeFalse();
        result.Should().BeSameAs(evt);
    }

    [Fact]
    public void CreateBeforeSendCallback_BeforeSendThrows_ReturnsNullAndLogsError()
    {
        // Arrange
        var exception = new InvalidOperationException("callback failed");
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        options.Native.EnableBeforeSend = true;
        options.SetBeforeSend((SentryEvent _, SentryHint _) => throw exception);
        options.Android.SuppressSegfaults = false;
        using var javaOptions = new JavaSdk.SentryOptions();
        using var callback = SentrySdk.CreateBeforeSendCallback(options, javaOptions);
        using var evt = new JavaSdk.SentryEvent();
        using var hint = new JavaSdk.Hint();

        // Act
        var result = callback!.Execute(evt, hint);

        // Assert
        result.Should().BeNull();
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == SentryLevel.Error &&
            entry.Exception == exception &&
            entry.Message == "Android BeforeSend callback failed.");
    }
}
#endif
