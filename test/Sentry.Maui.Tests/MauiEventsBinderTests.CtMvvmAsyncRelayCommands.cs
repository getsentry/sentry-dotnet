using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentry.Maui.CommunityToolkitMvvm;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Fact]
    public async Task AsyncRelayCommand_AddsSpan()
    {
        var vm = new TestCtMvvmViewModel();
        var button = new Button
        {
            Command = vm.TestCommand
        };
        var binder = new CtMvvmMauiElementEventBinder(_fixture.Hub);
        binder.Bind(button, _ => { });

        button.Command.Execute(null!);

        // we need to wait for async command to run
        await Task.Delay(TimeSpan.FromSeconds(1.1));

        // Assert
        _fixture.Scope.Transaction.Should().NotBeNull("transaction should have been created");
        _fixture.Scope.Transaction!.Spans
            .Any(x => x.Operation.Equals("relay.command"))
            .Should().BeTrue("span should be created with operation 'relay.command'");
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
