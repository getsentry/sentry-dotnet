#if ANDROID
namespace Sentry.Tests.Platforms.Android;

public class SentrySdkTests
{
    [Fact]
    public void BeforeSendWrapper_SuppressSIGSEGV_ReturnsNull()
    {
        // Arrange
        var options = new SentryOptions();
        var evt = new SentryEvent
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Type = "SIGSEGV",
                    Value = "Segfault"
                }
            }
        };
        var hint = new SentryHint();

        // Act
        var result = SentrySdk.BeforeSendWrapper(options).Invoke(evt, hint);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void BeforeSendWrapper_BeforeSendCallbackDefined_CallsBeforeSend()
    {
        // Arrange
        var beforeSend = Substitute.For<Func<SentryEvent, SentryHint, SentryEvent>>();
        beforeSend.Invoke(Arg.Any<SentryEvent>(), Arg.Any<SentryHint>()).Returns(callInfo => callInfo.Arg<SentryEvent>());

        var options = new SentryOptions();
        options.Native.EnableBeforeSend = true;
        options.SetBeforeSend(beforeSend);
        var evt = new SentryEvent();
        var hint = new SentryHint();

        // Act
        var result = SentrySdk.BeforeSendWrapper(options).Invoke(evt, hint);

        // Assert
        beforeSend.Received(1).Invoke(Arg.Any<SentryEvent>(), Arg.Any<SentryHint>());
        result.Should().Be(evt);
    }

    [Fact]
    public void BeforeSendWrapper_NoBeforeSendCallback_ReturnsEvent()
    {
        // Arrange
        var options = new SentryOptions();
        options.Native.EnableBeforeSend = true;
        var evt = new SentryEvent();
        var hint = new SentryHint();

        // Act
        var result = SentrySdk.BeforeSendWrapper(options).Invoke(evt, hint);

        // Assert
        result.Should().Be(evt);
    }

    [Fact]
    public void BeforeSendWrapper_NativeBeforeSendDisabled_ReturnsEvent()
    {
        // Arrange
        var beforeSend = Substitute.For<Func<SentryEvent, SentryHint, SentryEvent>>();
        beforeSend.Invoke(Arg.Any<SentryEvent>(), Arg.Any<SentryHint>()).Returns(_ => null);

        var options = new SentryOptions();
        options.SetBeforeSend(beforeSend);
        options.Native.EnableBeforeSend = false;
        var evt = new SentryEvent();
        var hint = new SentryHint();

        // Act
        var result = SentrySdk.BeforeSendWrapper(options).Invoke(evt, hint);

        // Assert
        beforeSend.DidNotReceive().Invoke(Arg.Any<SentryEvent>(), Arg.Any<SentryHint>());
        result.Should().Be(evt);
    }
}
#endif
