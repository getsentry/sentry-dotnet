using Sentry.Maui.Internal;
using Sentry.Maui.Tests.Mocks;
#if __ANDROID__
using View = Android.Views.View;
#endif

namespace Sentry.Maui.Tests;

public class MauiSessionReplayMaskControlsOfTypeBinderTests
{
    private class Fixture
    {
        public MauiSessionReplayMaskControlsOfTypeBinder ControlsOfTypeBinder { get; }

        public SentryMauiOptions Options { get; } = new();

        public Fixture()
        {
            Options.Debug = true;
            var logger = Substitute.For<IDiagnosticLogger>();
            logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
            Options.DiagnosticLogger = logger;
            var options = Microsoft.Extensions.Options.Options.Create(Options);
            ControlsOfTypeBinder = new MauiSessionReplayMaskControlsOfTypeBinder(options);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void OnElementLoaded_SenderIsNotVisualElement_LogsDebugAndReturns()
    {
        // Arrange
        var element = new MockElement("element");

        // Act
        _fixture.ControlsOfTypeBinder.OnElementLoaded(element, EventArgs.Empty);

        // Assert
        _fixture.Options.DiagnosticLogger.Received(1).LogDebug("OnElementLoaded: sender is not a VisualElement");
    }

    [Fact]
    public void OnElementLoaded_HandlerIsNull_LogsDebugAndReturns()
    {
        // Arrange
        var element = new MockVisualElement("element")
        {
            Handler = null
        };

        // Act
        _fixture.ControlsOfTypeBinder.OnElementLoaded(element, EventArgs.Empty);

        // Assert
        _fixture.Options.DiagnosticLogger.Received(1).LogDebug("OnElementLoaded: handler is null");
    }

#if __ANDROID__
    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(1.0, 0.0)]
    [InlineData(1.0, 1.0)]
    public void SessionReplayEnabled_IsEnabled(
        double sessionSampleRate, double onErrorSampleRate)
    {
        // Arrange
        var options = new SentryMauiOptions { Dsn = ValidDsn };
        // force custom masking to be enabled
        options.Native.ExperimentalOptions.SessionReplay.MaskControlsOfType<object>();
        // One of the below has to be non-zero for session replay to be enabled
        options.Native.ExperimentalOptions.SessionReplay.SessionSampleRate = sessionSampleRate;
        options.Native.ExperimentalOptions.SessionReplay.OnErrorSampleRate = onErrorSampleRate;

        // Act
        var iOptions = Microsoft.Extensions.Options.Options.Create(options);
        var binder = new MauiSessionReplayMaskControlsOfTypeBinder(iOptions);

        // Assert
        binder.IsEnabled.Should().Be(true);
    }

    [Fact]
    public void SessionReplayDisabled_IsNotEnabled()
    {
        // Arrange
        var options = new SentryMauiOptions { Dsn = ValidDsn };
        // force custom masking to be enabled
        options.Native.ExperimentalOptions.SessionReplay.MaskControlsOfType<object>();
        // No sessionSampleRate or onErrorSampleRate set... so should be disabled

        // Act
        var iOptions = Microsoft.Extensions.Options.Options.Create(options);
        var binder = new MauiSessionReplayMaskControlsOfTypeBinder(iOptions);

        // Assert
        binder.IsEnabled.Should().Be(false);
    }

    [Fact]
    public void UseSentry_NoMaskedControls_DoesNotRegisterMauiVisualElementEventsBinder()
    {
        // Arrange
        var options = new SentryMauiOptions { Dsn = ValidDsn };
        options.Native.ExperimentalOptions.SessionReplay.OnErrorSampleRate = 1.0;
        options.Native.ExperimentalOptions.SessionReplay.SessionSampleRate = 1.0;
        // Not really necessary, but just to be explicit
        options.Native.ExperimentalOptions.SessionReplay.MaskedControls.Clear();

        // Act
        var iOptions = Microsoft.Extensions.Options.Options.Create(options);
        var binder = new MauiSessionReplayMaskControlsOfTypeBinder(iOptions);

        // Assert
        binder.IsEnabled.Should().Be(false);
    }
#endif

}
