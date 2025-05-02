using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentry.Maui.Internal;
using Sentry.Maui.Tests.Mocks;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Fact]
    public async Task AsyncRelayCommand_AddsSpan()
    {
        var button = new Button();
        var page = new ContentPage();
        page.BindingContext = new TestCtMvvmViewModel();
        //page.Content.AddLogicalChild(button);

        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(page));
        _fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));

        // Act
        button.RaiseEvent(nameof(Button.Clicked), EventArgs.Empty);

        // we need to wait for async command to run
        await Task.Delay(TimeSpan.FromSeconds(1.1));

        // Assert
        _fixture.Scope.Span.Should().NotBeNull();
    }
}

public partial class TestCtMvvmViewModel : ObservableObject
{
    [RelayCommand]
    private async Task Test()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}
