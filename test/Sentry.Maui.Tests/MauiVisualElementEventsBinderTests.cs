using Sentry.Maui.Internal;
using Sentry.Maui.Tests.Mocks;
#if __ANDROID__
using View = Android.Views.View;
#endif

namespace Sentry.Maui.Tests;

public class MauiVisualElementEventsBinderTests
{
    private class Fixture
    {
        public MauiVisualElementEventsBinder Binder { get; }

        public SentryMauiOptions Options { get; } = new();

        public Fixture()
        {
            Options.Debug = true;
            var logger = Substitute.For<IDiagnosticLogger>();
            logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
            Options.DiagnosticLogger = logger;
            var options = Microsoft.Extensions.Options.Options.Create(Options);
            Binder = new MauiVisualElementEventsBinder(options);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void OnElementLoaded_SenderIsNotVisualElement_LogsDebugAndReturns()
    {
        // Arrange
        var element = new MockElement("element");

        // Act
        _fixture.Binder.OnElementLoaded(element, EventArgs.Empty);

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
        _fixture.Binder.OnElementLoaded(element, EventArgs.Empty);

        // Assert
        _fixture.Options.DiagnosticLogger.Received(1).LogDebug("OnElementLoaded: handler is null");
    }

}
