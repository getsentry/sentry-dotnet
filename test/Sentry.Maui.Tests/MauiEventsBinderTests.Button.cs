using Sentry.Maui.Internal;

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
            Arg.Is<string>(m => m.Contains("AutomationId") && m.Contains("StyleId") && m.Contains("Click transaction skipped")),
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
    public void Button_Pressed_SameButton_AlwaysCreatesNewTransaction()
    {
        // Arrange
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        var firstTransaction = Substitute.For<ITransactionTracer>();
        firstTransaction.Name.Returns("my-btn");
        firstTransaction.IsFinished.Returns(false);
        var secondTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(firstTransaction, secondTransaction);

        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Act - press the same button again
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert - each click creates a new transaction
        _fixture.Hub.Received(2).StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
        // Previous childless transaction not explicitly finished — idle timeout will discard it
        firstTransaction.DidNotReceive().Finish(Arg.Any<SpanStatus>());
    }

    [Fact]
    public void Button_Pressed_DifferentButton_PreviousNotExplicitlyFinished()
    {
        // The previous UI tx is left for its own idle timer to finalize (capture if it has
        // child spans, discard if not). We don't duplicate that logic here.
        // Arrange
        var firstButton = new Button { AutomationId = "first" };
        var secondButton = new Button { AutomationId = "second" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(firstButton));
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(secondButton));
        var firstTransaction = Substitute.For<ITransactionTracer>();
        firstTransaction.IsFinished.Returns(false);
        var secondTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(firstTransaction, secondTransaction);

        // Act
        firstButton.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);
        secondButton.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        firstTransaction.DidNotReceive().Finish(Arg.Any<SpanStatus>());
        _fixture.Hub.Received(2).StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Button_Pressed_DifferentButton_ScopeReplacedWithNewTransaction()
    {
        // Regression: a stale previous tx was left on scope, causing later navigation
        // spans to attach to the wrong button click.
        // Arrange
        var firstButton = new Button { AutomationId = "first" };
        var secondButton = new Button { AutomationId = "second" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(firstButton));
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(secondButton));
        var firstTransaction = Substitute.For<ITransactionTracer>();
        firstTransaction.IsFinished.Returns(false);
        var secondTransaction = Substitute.For<ITransactionTracer>();
        secondTransaction.IsFinished.Returns(false);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(firstTransaction, secondTransaction);

        // Act
        firstButton.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);
        Assert.Same(firstTransaction, _fixture.Scope.Transaction); // sanity
        secondButton.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert - scope swapped from stale tx1 to fresh tx2
        Assert.Same(secondTransaction, _fixture.Scope.Transaction);
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
    public void OnWindowOnStopped_ChildlessInteractionTransaction_NotExplicitlyFinished()
    {
        // Arrange
        var window = new Window();
        _fixture.Binder.HandleWindowEvents(window);
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        var clickTransaction = Substitute.For<ITransactionTracer>();
        clickTransaction.IsFinished.Returns(false);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(clickTransaction);

        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Act
        window.RaiseEvent(nameof(Window.Stopped), EventArgs.Empty);

        // Assert - childless tx not explicitly finished; idle timeout will discard
        clickTransaction.DidNotReceive().Finish(Arg.Any<SpanStatus>());
    }

    [Fact]
    public void OnWindowOnStopped_InteractionTransactionWithChildren_FinishesTransaction()
    {
        // Arrange
        var window = new Window();
        _fixture.Binder.HandleWindowEvents(window);
        var button = new Button { AutomationId = "my-btn" };
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        var clickTransaction = Substitute.For<ITransactionTracer>();
        clickTransaction.IsFinished.Returns(false);
        clickTransaction.Spans.Returns(new[] { Substitute.For<ISpan>() }); // has a child span
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(clickTransaction);

        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

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

        // Act - unbind, then press
        _fixture.Binder.OnApplicationOnDescendantRemoved(null, el);
        button.RaiseEvent(nameof(Button.Pressed), EventArgs.Empty);

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }
}
