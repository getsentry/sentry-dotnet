using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Sentry.Maui.CommunityToolkitMvvm;

/// <summary>
/// Scans all elements for known commands that are implement
/// </summary>
public class CtMvvmMauiElementEventBinder : IMauiElementEventBinder
{
    /// <summary>
    /// Binds to the element
    /// </summary>
    /// <param name="element"></param>
    /// <param name="addBreadcrumb"></param>
    public void Bind(VisualElement element, Action<BreadcrumbEvent> addBreadcrumb)  => Iterate(element, true);

    /// <summary>
    /// Unbinds from the element
    /// </summary>
    /// <param name="element"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void UnBind(VisualElement element) => Iterate(element, false);


    void Iterate(VisualElement element, bool bind)
    {
        switch (element)
        {
            case Button button:
                TryBindTo(button.Command, bind);
                break;

            case ImageButton imageButton:
                TryBindTo(imageButton.Command, bind);
                break;
            
        }

        if (element is IGestureRecognizers gestureRecognizers)
        {
            foreach (var gestureRecognizer in gestureRecognizers.GestureRecognizers)
            {
            }
        }
    }

    private static void TryBindTo(ICommand? command, bool bind)
    {
        if (command is IAsyncRelayCommand relayCommand)
        {
            if (bind)
            {
                relayCommand.PropertyChanged += RelayCommandOnPropertyChanged;
            }
            else
            {
                relayCommand.PropertyChanged -= RelayCommandOnPropertyChanged;
            }
        }
    }

    private static void RelayCommandOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAsyncRelayCommand.IsRunning))
        {
            var relay = (IAsyncRelayCommand)sender!;
            if (relay.IsRunning)
            {
                // start span
            }
            else
            {
                // finish span
            }
        }
    }
}
