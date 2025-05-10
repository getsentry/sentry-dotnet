using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sentry.Samples.Maui;

public partial class CtMvvmViewModel : ObservableObject
{
    // Sentry automatically creates spans for async relay commands
    [RelayCommand]
    private async Task Test()
    {
        var rand = new Random().Next(1, 10);
        await Task.Delay(TimeSpan.FromSeconds(rand));
    }
}
