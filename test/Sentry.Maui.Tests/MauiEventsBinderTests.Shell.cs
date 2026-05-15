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
    public void OnShellOnNavigating_SetsOperationAndDescription()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var mockTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.GetSpan().Returns(mockTransaction);
        var navSpan = Substitute.For<ISpan>();
        mockTransaction.StartChild(Arg.Any<string>())
            .Returns(navSpan);

        // Act
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert
        mockTransaction.Received(1).StartChild(Arg.Any<string>());
        navSpan.Description.Should().Be("bar");
    }

    [Fact]
    public void OnShellOnNavigating_EnableAutoTransactionsFalse_DoesNotCreateSpan()
    {
        // Arrange
        _fixture.Options.EnableAutoTransactions = false;
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var mockTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.GetSpan().Returns(mockTransaction);

        // Act
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert
        mockTransaction.DidNotReceive().StartChild(Arg.Any<string>());
    }

    [Fact]
    public void OnShellOnNavigating_DifferentRoute_FinishesPreviousNavSpan()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var mockTransaction = Substitute.For<ITransactionTracer>();
        _fixture.Hub.GetSpan().Returns(mockTransaction);
        var firstSpan = Substitute.For<ISpan>();
        var secondSpan = Substitute.For<ISpan>();
        mockTransaction.StartChild(Arg.Any<string>())
            .Returns(firstSpan, secondSpan);
        var navSpan = _fixture.Binder.StartNavigationSpan("oldName");
        navSpan.Should().Be(firstSpan); // sanity check

        // Act
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("bar"), new ShellNavigationState("baz"), ShellNavigationSource.Push, false));

        // Assert
        firstSpan.Received(1).Finish(SpanStatus.Ok);
        _fixture.Binder.CurrentNavSpan.Should().Be(secondSpan);
    }

    [Fact]
    public void OnShellOnNavigating_ActiveUiTransaction_ResetsIdleTimeout()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var uiTransaction = Substitute.For<ITransactionTracer>();
        uiTransaction.Name.Returns("bar");
        uiTransaction.IsFinished.Returns(false);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(uiTransaction);
        _fixture.Binder.StartUiTransaction("btnClick");

        // Act
        shell.RaiseEvent(nameof(Shell.Navigating),
            new ShellNavigatingEventArgs(new ShellNavigationState("bar"), new ShellNavigationState("bar"), ShellNavigationSource.Push, false));

        // Assert - only one transaction was started, and its idle timeout was reset
        _fixture.Hub.Received(1).StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>());
        uiTransaction.Received(1).ResetIdleTimeout();
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
    public void OnShellOnNavigated_FinishesNavSpan()
    {
        // Arrange
        var shell = new Shell { StyleId = "shell" };
        _fixture.Binder.HandleShellEvents(shell);
        var mockTransaction = Substitute.For<ITransactionTracer>();
        mockTransaction.IsFinished.Returns(false);
        _fixture.Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<TimeSpan?>())
            .Returns(mockTransaction);
        mockTransaction.StartChild(Arg.Any<string>())
            .Returns(Substitute.For<ISpan>());
        _fixture.Hub.GetSpan().Returns(mockTransaction);
        var navSpan = _fixture.Binder.StartNavigationSpan("oldName");

        // Act
        shell.RaiseEvent(nameof(Shell.Navigated),
            new ShellNavigatedEventArgs(new ShellNavigationState("foo"), new ShellNavigationState("//resolved/bar"), ShellNavigationSource.Push));

        // Assert
        Assert.NotNull(navSpan);
        navSpan.Description.Should().Be("//resolved/bar");
        navSpan.Received(1).Finish(SpanStatus.Ok);
    }
}
