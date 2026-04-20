using Sentry.Internal;
using Sentry.Maui.Internal;
using Sentry.Maui.Tests.Mocks;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Fact]
    public void Button_Pressed_StartsTransactionWithClickOp()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Operation == MauiEventsBinder.UserInteractionClickOp),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Pressed_UsesAutomationIdAsIdentifier()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn", StyleId = "styleId-ignored" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert - AutomationId wins over StyleId
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == "my-btn"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Pressed_FallsBackToStyleIdWhenNoAutomationId()
    {
        // Arrange
        var button = new Button { StyleId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == "my-btn"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Pressed_NoAutomationIdOrStyleId_DoesNotStartTransaction()
    {
        // Arrange
        var button = new Button();
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        // Act
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
        _fixture.Options.DiagnosticLogger!.Received(1).Log(
            SentryLevel.Warning,
            Arg.Is<string>(m => m.Contains("AutomationId") && m.Contains("StyleId")),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public void Button_Pressed_UsesPageTypeNameInTransactionName()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn" };
        _ = new ContentPage { Content = new VerticalStackLayout { button } };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == $"{nameof(ContentPage)}.my-btn"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Pressed_NoContainingPage_UsesIdentifierOnly()
    {
        // Arrange - button not attached to any page
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == "my-btn"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Pressed_TransactionNameSourceIsComponent()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.NameSource == TransactionNameSource.Component),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Pressed_EnableUserInteractionTracingFalse_DoesNotStart()
    {
        // Arrange
        _fixture.Options.EnableUserInteractionTracing = false;
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        // Act
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Pressed_EnableAutoTransactionsFalse_DoesNotStart()
    {
        // Arrange
        _fixture.Options.EnableAutoTransactions = false;
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        // Act
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Pressed_SameButton_ResetsIdleTimeoutAndDoesNotStartNew()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        var transaction = Substitute.For<ITransactionTracer>();
        transaction.Name.Returns("my-btn");
        transaction.IsFinished.Returns(false);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(transaction);

        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Act - press the same button again
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.Received(1).StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
        transaction.Received(1).ResetIdleTimeout();
    }

    [Fact]
    public void Button_Pressed_DifferentButton_FinishesPreviousAndStartsNew()
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
        firstButton.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);
        secondButton.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        firstTransaction.Received(1).Finish(SpanStatus.Ok);
        _fixture.Hub.Received(2).StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Pressed_ManualTransactionOnScope_NotBoundToScope()
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
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert - SDK starts the auto transaction but does NOT bind to scope
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Operation == MauiEventsBinder.UserInteractionClickOp),
            Arg.Any<TimeSpan?>());
        Assert.Same(userTransaction, _fixture.Scope.Transaction);
    }

    [Fact]
    public void Button_Pressed_NavigationTransactionOnScope_NotBoundToScope()
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

        // Act - press while nav transaction is on scope
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert - click transaction was started but scope still holds the navigation transaction
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Operation == MauiEventsBinder.UserInteractionClickOp),
            Arg.Any<TimeSpan?>());
        Assert.Same(navTransaction, _fixture.Scope.Transaction);
    }

    [Fact]
    public void Button_Pressed_ThenShellNavigating_NavigationIsChildSpanOfClick()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        var navSpan = Substitute.For<ISpan>();
        var clickTransaction = Substitute.For<ITransactionTracer>();
        clickTransaction.IsFinished.Returns(false);
        clickTransaction.StartChild(Arg.Any<string>()).Returns(navSpan);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(clickTransaction);

        // Act - press button, then navigate
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert - only one transaction created (click), navigation is a child span
        _fixture.Hub.Received(1).StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
        clickTransaction.Received(1).StartChild("ui.load");
    }

    [Fact]
    public void Button_Pressed_ThenModalPush_NavigationIsChildSpanOfClick()
    {
        // Arrange
        var application = MockApplication.Create();
        _fixture.Binder.HandleApplicationEvents(application);
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        var navSpan = Substitute.For<ISpan>();
        var clickTransaction = Substitute.For<ITransactionTracer>();
        clickTransaction.IsFinished.Returns(false);
        clickTransaction.StartChild(Arg.Any<string>()).Returns(navSpan);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(clickTransaction);

        var modalPage = new ContentPage { StyleId = "TestModalPage" };

        // Act - press button, then push modal
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);
        application.RaiseEvent(nameof(Application.ModalPushed), new ModalPushedEventArgs(modalPage));

        // Assert - only one transaction created (click), navigation is a child span
        _fixture.Hub.Received(1).StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
        clickTransaction.Received(1).StartChild("ui.load");
    }

    [Fact]
    public void Button_Pressed_ModalPopped_FinishesSpanNotTransaction()
    {
        // Arrange
        var application = MockApplication.Create();
        _fixture.Binder.HandleApplicationEvents(application);
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        var navSpan = Substitute.For<ISpan>();
        navSpan.IsFinished.Returns(false);
        var clickTransaction = Substitute.For<ITransactionTracer>();
        clickTransaction.IsFinished.Returns(false);
        clickTransaction.StartChild(Arg.Any<string>()).Returns(navSpan);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(clickTransaction);

        var modalPage = new ContentPage { StyleId = "TestModalPage" };

        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);
        application.RaiseEvent(nameof(Application.ModalPushed), new ModalPushedEventArgs(modalPage));

        // Act - pop modal
        application.RaiseEvent(nameof(Application.ModalPopped), new ModalPoppedEventArgs(modalPage));

        // Assert - child span finished, click transaction NOT finished (idle timeout manages it)
        navSpan.Received(1).Finish(SpanStatus.Ok);
        clickTransaction.DidNotReceive().Finish(Arg.Any<SpanStatus>());
    }

    [Fact]
    public void Button_Pressed_ShellNavigated_UpdatesSpanDescription()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        var navSpan = Substitute.For<ISpan>();
        navSpan.IsFinished.Returns(false);
        var clickTransaction = Substitute.For<ITransactionTracer>();
        clickTransaction.IsFinished.Returns(false);
        clickTransaction.StartChild(Arg.Any<string>()).Returns(navSpan);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(clickTransaction);

        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Act - navigated with resolved route
        shell.RaiseEvent(nameof(Shell.Navigated),
            new ShellNavigatedEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("//resolved/bar"), ShellNavigationSource.Push));

        // Assert - span description updated (not transaction name)
        navSpan.Description.Should().Be("//resolved/bar");
    }

    [Fact]
    public void StandaloneNavigation_NoClick_BehaviorUnchanged()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var navTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(navTransaction);

        // Act - navigate without any button press
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert - standalone navigation transaction created as before
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Operation == "ui.load"),
            Arg.Any<TimeSpan?>());
        Assert.Same(navTransaction, _fixture.Scope.Transaction);
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

        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Act
        window.RaiseEvent(nameof(Window.Stopped), EventArgs.Empty);

        // Assert
        clickTransaction.Received(1).Finish(SpanStatus.Ok);
    }

    [Fact]
    public void Window_Stopped_CleansUpNavigationSpan()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var window = new Window();
        _fixture.Binder.HandleWindowEvents(window);
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        var navSpan = Substitute.For<ISpan>();
        navSpan.IsFinished.Returns(false);
        var clickTransaction = Substitute.For<ITransactionTracer>();
        clickTransaction.IsFinished.Returns(false);
        clickTransaction.StartChild(Arg.Any<string>()).Returns(navSpan);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(clickTransaction);

        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Act
        window.RaiseEvent(nameof(Window.Stopped), EventArgs.Empty);

        // Assert - both the nav span and click transaction are cleaned up
        navSpan.Received(1).Finish(SpanStatus.Ok);
        clickTransaction.Received(1).Finish(SpanStatus.Ok);
    }

    [Fact]
    public void Button_Unbind_StopsStartingTransactions()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn" };
        var el = new ElementEventArgs(button);
        _fixture.Binder.OnApplicationOnDescendantAdded(null, el);

        // Act - unbind, then press
        _fixture.Binder.OnApplicationOnDescendantRemoved(null, el);
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }
}
