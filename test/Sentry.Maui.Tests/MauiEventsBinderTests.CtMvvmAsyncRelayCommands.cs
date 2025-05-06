using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentry.Maui.CommunityToolkitMvvm;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Fact]
    public async Task AsyncRelayCommand_AddsTransaction()
    {
        var vm = new TestCtMvvmViewModel();
        var button = new Button
        {
            Command = vm.TestCommand
        };
        //_fixture.Binder.OnApplicationOnDescendantAdded(null, new ElementEventArgs(button));
        var binder = new CtMvvmMauiElementEventBinder(_fixture.Hub);
        binder.Bind(button, _ => {});

        button.Command.Execute(null!);

        // we need to wait for async command to run
        await Task.Delay(TimeSpan.FromSeconds(1.1));

        // Assert
        _fixture.Scope.Transaction.Should().NotBeNull("Transaction should be created");
        _fixture.Scope.Transaction!.Name.Should().Be("ctmvvm");
        _fixture.Scope.Transaction!.Status.Should().Be(SpanStatus.Ok, "Transaction should be ok");
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
