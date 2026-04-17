using Sentry.Internal;
using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Fact]
    public void Button_Clicked_StartsTransactionWithClickOp()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Operation == MauiEventsBinder.UserInteractionClickOp),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Clicked_UsesAutomationIdAsIdentifier()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn", StyleId = "styleId-ignored" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert - AutomationId wins over StyleId
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == "my-btn"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Clicked_FallsBackToStyleIdWhenNoAutomationId()
    {
        // Arrange
        var button = new Button { StyleId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == "my-btn"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Clicked_NoAutomationIdOrStyleId_DoesNotStartTransaction()
    {
        // Arrange
        var button = new Button();
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        // Act
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
        _fixture.Options.DiagnosticLogger!.Received(1).Log(
            SentryLevel.Warning,
            Arg.Is<string>(m => m.Contains("AutomationId") && m.Contains("StyleId")),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public void Button_Clicked_UsesPageTypeNameInTransactionName()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn" };
        _ = new ContentPage { Content = new VerticalStackLayout { button } };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == $"{nameof(ContentPage)}.my-btn"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Clicked_NoContainingPage_UsesIdentifierOnly()
    {
        // Arrange - button not attached to any page
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == "my-btn"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Clicked_TransactionNameSourceIsComponent()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.NameSource == TransactionNameSource.Component),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Clicked_EnableUserInteractionTracingFalse_DoesNotStart()
    {
        // Arrange
        _fixture.Options.EnableUserInteractionTracing = false;
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        // Act
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Clicked_EnableAutoTransactionsFalse_DoesNotStart()
    {
        // Arrange
        _fixture.Options.EnableAutoTransactions = false;
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        // Act
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Clicked_SameButton_ResetsIdleTimeoutAndDoesNotStartNew()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        var transaction = Substitute.For<ITransactionTracer>();
        transaction.Name.Returns("my-btn");
        transaction.IsFinished.Returns(false);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(transaction);

        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Act - click the same button again
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
        transaction.Received(1).ResetIdleTimeout();
    }

    [Fact]
    public void Button_Clicked_DifferentButton_FinishesPreviousAndStartsNew()
    {
        // Arrange
        var firstButton = new Button { AutomationId = "first" };
        var secondButton = new Button { AutomationId = "second" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(firstButton));
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(secondButton));
        var firstTransaction = Substitute.For<ITransactionTracer>();
        firstTransaction.Name.Returns("first");
        var secondTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(firstTransaction, secondTransaction);

        // Act
        firstButton.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);
        secondButton.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert
        firstTransaction.Received(1).Finish(SpanStatus.Ok);
        _fixture.Hub.Received(2).StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Clicked_ManualTransactionOnScope_NotBoundToScope()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        var userTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Scope.Transaction = userTransaction;

        var autoTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(autoTransaction);

        // Act
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert - SDK starts the auto transaction but does NOT bind to scope
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Operation == MauiEventsBinder.UserInteractionClickOp),
            Arg.Any<TimeSpan?>());
        Assert.Same(userTransaction, _fixture.Scope.Transaction);
    }

    [Fact]
    public void Button_Clicked_NavigationTransactionOnScope_NotBoundToScope()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        var navTransaction = Substitute.For<ITransactionTracer>();
        var clickTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(navTransaction, clickTransaction);

        // Start a navigation, which binds navTransaction to the scope
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));
        Assert.Same(navTransaction, _fixture.Scope.Transaction);

        // Act - click while nav transaction is on scope
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert - click transaction was started but scope still holds the navigation transaction
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Operation == MauiEventsBinder.UserInteractionClickOp),
            Arg.Any<TimeSpan?>());
        Assert.Same(navTransaction, _fixture.Scope.Transaction);
    }

    [Fact]
    public void Shell_Navigating_WhileInteractionTransactionActive_FinishesInteractionAsCancelled()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        var clickTransaction = Substitute.For<ITransactionTracer>();
        clickTransaction.IsFinished.Returns(false);
        var navTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(clickTransaction, navTransaction);

        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Act - navigation starts while click transaction is still live
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert - click transaction finished as Cancelled
        clickTransaction.Received(1).Finish(SpanStatus.Cancelled);
    }

    [Fact]
    public void Window_Stopped_FinishesActiveInteractionTransaction()
    {
        // Arrange
        var window = new Window();
        _fixture.Binder.HandleWindowEvents(window);
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        var clickTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(clickTransaction);

        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Act
        window.RaiseEvent(nameof(Window.Stopped), EventArgs.Empty);

        // Assert
        clickTransaction.Received(1).Finish(SpanStatus.Ok);
    }

    [Fact]
    public void Button_Unbind_StopsStartingTransactions()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn" };
        var el = new ElementEventArgs(button);
        _fixture.Binder.OnApplicationOnDescendantAdded(null, el);

        // Act - unbind, then click
        _fixture.Binder.OnApplicationOnDescendantRemoved(null, el);
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }
}
