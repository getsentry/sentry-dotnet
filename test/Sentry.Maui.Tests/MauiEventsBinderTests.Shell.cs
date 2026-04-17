using Sentry.Internal;
using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Fact]
    public void Shell_Navigating_AddsBreadcrumb()
    {
        // Arrange
        var shell = new Shell
        {
            StyleId = "shell"
        };
        _fixture.Binder.HandleShellEvents(shell);

        var current = new ShellNavigationState("foo");
        var target = new ShellNavigationState("bar");
        const ShellNavigationSource source = ShellNavigationSource.Push;

        // Act
        shell.RaiseEvent(nameof(Shell.Navigating), new ShellNavigatingEventArgs(current, target, source, false));

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(Shell)}.{nameof(Shell.Navigating)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.NavigationType, crumb.Type);
        Assert.Equal(MauiEventsBinder.NavigationCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(Shell)}.Name", "shell");
        crumb.Data.Should().Contain($"from", "foo");
        crumb.Data.Should().Contain($"to", "bar");
        crumb.Data.Should().Contain($"Source", "Push");
    }

    [Fact]
    public void Shell_UnbindNavigating_DoesNotAddBreadcrumb()
    {
        // Arrange
        var shell = new Shell
        {
            StyleId = "shell"
        };
        _fixture.Binder.HandleShellEvents(shell);

        var current = new ShellNavigationState("foo");
        var target = new ShellNavigationState("bar");
        const ShellNavigationSource source = ShellNavigationSource.Push;

        shell.RaiseEvent(nameof(Shell.Navigating), new ShellNavigatingEventArgs(current, target, source, false));
        Assert.Single(_fixture.Scope.Breadcrumbs); // Sanity check

        _fixture.Binder.HandleShellEvents(shell, bind: false);

        // Act
        shell.RaiseEvent(nameof(Shell.Navigating), new ShellNavigatingEventArgs(target, current, source, false));

        // Assert
        Assert.Single(_fixture.Scope.Breadcrumbs);
    }

    [Fact]
    public void Shell_Navigated_AddsBreadcrumb()
    {
        // Arrange
        var shell = new Shell
        {
            StyleId = "shell"
        };
        _fixture.Binder.HandleShellEvents(shell);

        var previous = new ShellNavigationState("foo");
        var current = new ShellNavigationState("bar");
        const ShellNavigationSource source = ShellNavigationSource.Push;

        // Act
        shell.RaiseEvent(nameof(Shell.Navigated), new ShellNavigatedEventArgs(previous, current, source));

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(Shell)}.{nameof(Shell.Navigated)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.NavigationType, crumb.Type);
        Assert.Equal(MauiEventsBinder.NavigationCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(Shell)}.Name", "shell");
        crumb.Data.Should().Contain($"from", "foo");
        crumb.Data.Should().Contain($"to", "bar");
        crumb.Data.Should().Contain($"Source", "Push");
    }

    [Fact]
    public void Shell_UnbindNavigated_DoesNotAddBreadcrumb()
    {
        // Arrange
        var shell = new Shell
        {
            StyleId = "shell"
        };
        _fixture.Binder.HandleShellEvents(shell);

        var previous = new ShellNavigationState("foo");
        var current = new ShellNavigationState("bar");
        const ShellNavigationSource source = ShellNavigationSource.Push;

        shell.RaiseEvent(nameof(Shell.Navigated), new ShellNavigatedEventArgs(previous, current, source));
        Assert.Single(_fixture.Scope.Breadcrumbs); // Sanity check

        _fixture.Binder.HandleShellEvents(shell, bind: false);

        // Act
        shell.RaiseEvent(nameof(Shell.Navigated), new ShellNavigatedEventArgs(current, previous, source));

        // Assert
        Assert.Single(_fixture.Scope.Breadcrumbs);
    }

    [Fact]
    public void Shell_Navigating_FirstNavigation_SetsTransactionOnScope()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var mockTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(mockTransaction);

        // Act
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert
        Assert.Same(mockTransaction, _fixture.Scope.Transaction);
    }

    [Fact]
    public void Shell_Navigating_UsesRouteAsTransactionName()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == "bar"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Shell_Navigating_UsesUiLoadAsOperation()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(Substitute.For<ITransactionTracer>());

        // Act
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Operation == "ui.load"),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Shell_Navigating_NavigationTransactionsDisabled_DoesNotStartTransaction()
    {
        // Arrange
        _fixture.Options.EnableAutoTransactions = false;
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);

        // Act
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert
        _fixture.Hub.DidNotReceive().StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public void Shell_Navigating_DifferentDestination_ClearsTransactionFromScope()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var firstTransaction = Substitute.For<ITransactionTracer>();
        var secondTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(firstTransaction, secondTransaction);

        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Act - navigate to a different destination, finishing the first transaction
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("bar"), new ShellNavigationState("baz"), ShellNavigationSource.Push, false));

        // Assert - scope now holds the new transaction, not the old one
        Assert.Same(secondTransaction, _fixture.Scope.Transaction);
    }

    [Fact]
    public void Shell_Navigating_FinishesPreviousTransaction()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var firstTransaction = Substitute.For<ITransactionTracer>();
        var secondTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(firstTransaction, secondTransaction);

        // Act - navigate twice
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("bar"), new ShellNavigationState("baz"), ShellNavigationSource.Push, false));

        // Assert - first transaction was finished before the second started
        firstTransaction.Received(1).Finish(SpanStatus.Ok);
    }

    [Fact]
    public void Shell_Navigating_DifferentRoute_ReplacesScopeTransaction()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var firstTransaction = Substitute.For<ITransactionTracer>();
        var secondTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(firstTransaction, secondTransaction);

        // Act - first navigation binds firstTransaction to scope
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));
        Assert.Same(firstTransaction, _fixture.Scope.Transaction);

        // Act - second navigation to a different route
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("bar"), new ShellNavigationState("baz"), ShellNavigationSource.Push, false));

        // Assert - scope transaction replaced with the new one
        Assert.Same(secondTransaction, _fixture.Scope.Transaction);
    }

    [Fact]
    public void Shell_Navigated_UpdatesTransactionName()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var mockTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(mockTransaction);

        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Act
        shell.RaiseEvent(nameof(Shell.Navigated),
            new ShellNavigatedEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("//resolved/bar"), ShellNavigationSource.Push));

        // Assert
        mockTransaction.Name.Should().Be("//resolved/bar");
    }

    [Fact]
    public void Shell_Navigating_ManualTransactionOnScope_AutoTransactionCreatedButNotBoundToScope()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);

        // Simulate the user setting their own transaction on the scope before navigation
        var userTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Scope.Transaction = userTransaction;

        var autoTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(autoTransaction);

        // Act
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert - SDK still starts the auto transaction, but does NOT replace the user's scope transaction
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Is<ITransactionContext>(c => c.Name == "bar" && c.Operation == "ui.load"),
            Arg.Any<TimeSpan?>());
        Assert.Same(userTransaction, _fixture.Scope.Transaction);
    }

    [Fact]
    public void Shell_Navigating_SameRoute_ActiveTransaction_ResetsIdleTimeout()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var firstTransaction = Substitute.For<ITransactionTracer>();
        firstTransaction.Name.Returns("bar");
        firstTransaction.IsFinished.Returns(false);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(firstTransaction);

        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Act - navigate to the same route again
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("bar"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert - only one transaction was started, and its idle timeout was reset
        _fixture.Hub.Received(1).StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
        firstTransaction.Received(1).ResetIdleTimeout();
    }

    [Fact]
    public void Shell_Navigating_SameRoute_PreviousTransactionFinished_StartsNewTransaction()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var firstTransaction = Substitute.For<ITransactionTracer>();
        var secondTransaction = Substitute.For<ITransactionTracer>();
        firstTransaction.Name.Returns("bar");
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(firstTransaction, secondTransaction);

        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Simulate the idle timeout firing and auto-finishing the transaction
        firstTransaction.IsFinished.Returns(true);

        // Act - same route, but the previous transaction has already finished
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("bar"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert - a new transaction is started rather than reusing the finished one
        _fixture.Hub.Received(2).StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
        Assert.Same(secondTransaction, _fixture.Scope.Transaction);
    }

    [Fact]
    public void Window_Stopped_FinishesActiveTransaction()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var window = new Window();
        _fixture.Binder.HandleWindowEvents(window);
        var mockTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(mockTransaction);

        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Act
        window.RaiseEvent(nameof(Window.Stopped), EventArgs.Empty);

        // Assert
        mockTransaction.Received(1).Finish(SpanStatus.Ok);
    }

}
