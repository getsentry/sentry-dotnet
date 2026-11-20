using Sentry.Android.Callbacks;

namespace Sentry.Tests.Platforms.Android;

public class BeforeSendCallbackTests
{
    [Fact]
    public void Execute_NativeEventCannotBeRead_DropsEventBeforeCallback()
    {
        var called = false;
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        using var javaOptions = new JavaSdk.SentryOptions();
        using var callback = new BeforeSendCallback((evt, _) =>
        {
            called = true;
            return evt;
        }, options, javaOptions);
        using var native = new JavaSdk.SentryEvent
        {
            // Java accepts a 32-character ID; the managed SDK requires hexadecimal digits.
            EventId = new JavaSdk.Protocol.SentryId(new string('z', 32))
        };
        using var hint = new JavaSdk.Hint();

        var result = callback.Execute(native, hint);

        result.Should().BeNull();
        called.Should().BeFalse();
        logger.Entries.Should().ContainSingle(entry => entry.Exception is FormatException);
    }

    [Fact]
    public void Execute_FilteredEventCannotBeWritten_DropsEventAfterCallback()
    {
        var exception = new InvalidOperationException("Cannot read fingerprint");
        var fingerprint = Substitute.For<IReadOnlyList<string>>();
        fingerprint.Count.Throws(exception);
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        using var javaOptions = new JavaSdk.SentryOptions();
        using var callback = new BeforeSendCallback((evt, _) =>
        {
            evt.ServerName = null;
            evt.Fingerprint = fingerprint;
            return evt;
        }, options, javaOptions);
        using var native = new JavaSdk.SentryEvent { ServerName = "private-host" };
        using var hint = new JavaSdk.Hint();

        var result = callback.Execute(native, hint);

        result.Should().BeNull();
        logger.Entries.Should().ContainSingle(entry => entry.Exception == exception);
    }

    [Fact]
    public void Execute_CallbackRedactsServerName_ReturnsFilteredEvent()
    {
        using var javaOptions = new JavaSdk.SentryOptions();
        using var callback = new BeforeSendCallback((evt, _) =>
        {
            evt.ServerName = null;
            return evt;
        }, new SentryOptions(), javaOptions);
        using var native = new JavaSdk.SentryEvent { ServerName = "private-host" };
        using var hint = new JavaSdk.Hint();

        using var result = callback.Execute(native, hint);

        result.Should().NotBeNull();
        result!.EventId!.ToString().Should().Be(native.EventId!.ToString());
        result.ServerName.Should().BeNull();
    }
}
