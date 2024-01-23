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
}
