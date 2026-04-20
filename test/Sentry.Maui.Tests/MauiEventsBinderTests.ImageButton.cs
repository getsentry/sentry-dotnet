using Sentry.Internal;
using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Fact]
    public void ImageButton_Pressed_StartsTransactionWithClickOp()
    {
        // Arrange
        var button = new ImageButton { AutomationId = "my-img-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(ImageButton.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Operation == MauiEventsBinder.UserInteractionClickOp),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void ImageButton_Pressed_UsesAutomationIdAsIdentifier()
    {
        // Arrange
        var button = new ImageButton { AutomationId = "my-img-btn", StyleId = "styleId-ignored" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(ImageButton.Pressed), EventArgs.Empty);

        // Assert - AutomationId wins over StyleId
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == "my-img-btn"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void ImageButton_Pressed_FallsBackToStyleIdWhenNoAutomationId()
    {
        // Arrange
        var button = new ImageButton { StyleId = "my-img-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(ImageButton.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == "my-img-btn"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void ImageButton_Pressed_NoAutomationIdOrStyleId_DoesNotStartTransaction()
    {
        // Arrange
        var button = new ImageButton();
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        // Act
        button.RaiseEvent(nameof(ImageButton.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
        _fixture.Options.DiagnosticLogger!.Received(1).Log(
            SentryLevel.Warning,
            Arg.Is<string>(m => m.Contains("AutomationId") && m.Contains("StyleId") && m.Contains("Click transaction skipped")),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public void ImageButton_Pressed_UsesPageTypeNameInTransactionName()
    {
        // Arrange
        var button = new ImageButton { AutomationId = "my-img-btn" };
        _ = new ContentPage { Content = new VerticalStackLayout { button } };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(ImageButton.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == $"{nameof(ContentPage)}.my-img-btn"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void ImageButton_Pressed_EnableUserInteractionTracingFalse_DoesNotStart()
    {
        // Arrange
        _fixture.Options.EnableUserInteractionTracing = false;
        var button = new ImageButton { AutomationId = "my-img-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        // Act
        button.RaiseEvent(nameof(ImageButton.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void ImageButton_Pressed_EnableAutoTransactionsFalse_DoesNotStart()
    {
        // Arrange
        _fixture.Options.EnableAutoTransactions = false;
        var button = new ImageButton { AutomationId = "my-img-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        // Act
        button.RaiseEvent(nameof(ImageButton.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void ImageButton_Pressed_ThenShellNavigating_NavigationIsChildSpanOfClick()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var button = new ImageButton { AutomationId = "my-img-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        var navSpan = Substitute.For<ISpan>();
        var clickTransaction = Substitute.For<ITransactionTracer>();
        clickTransaction.IsFinished.Returns(false);
        clickTransaction.StartChild(Arg.Any<string>()).Returns(navSpan);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(clickTransaction);

        // Act - press button, then navigate
        button.RaiseEvent(nameof(ImageButton.Pressed), EventArgs.Empty);
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert - only one transaction created (click), navigation is a child span
        _fixture.Hub.Received(1).StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
        clickTransaction.Received(1).StartChild("ui.load");
    }

    [Fact]
    public void ImageButton_Unbind_StopsStartingTransactions()
    {
        // Arrange
        var button = new ImageButton { AutomationId = "my-img-btn" };
        var el = new ElementEventArgs(button);
        _fixture.Binder.OnApplicationOnDescendantAdded(null, el);

        // Act - unbind, then press
        _fixture.Binder.OnApplicationOnDescendantRemoved(null, el);
        button.RaiseEvent(nameof(ImageButton.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }
}
