using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Sentry.Internal;

namespace Sentry.Maui.CommunityToolkit.Mvvm;

/// <summary>
/// Scans all elements for known commands that are implement
/// </summary>
internal class MauiCommunityToolkitMvvmEventsBinder(IHub hub) : IMauiElementEventBinder
{
    private const string SpanName = "ctmvvm";
    private const string SpanOp = "relay.command";

    /// <summary>
    /// Binds to the element
    /// </summary>
    /// <param name="element"></param>
    /// <param name="addBreadcrumb"></param>
    public void Bind(VisualElement element, Action<BreadcrumbEvent> addBreadcrumb) => Iterate(element, true);

    /// <summary>
    /// Unbinds from the element
    /// </summary>
    /// <param name="element"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void UnBind(VisualElement element) => Iterate(element, false);


    private void Iterate(VisualElement element, bool bind)
    {
        switch (element)
        {
            case Button button:
                TryBindTo(button.Command, bind);
                break;

            case ImageButton imageButton:
                TryBindTo(imageButton.Command, bind);
                break;

            case CarouselView carousel:
                TryBindTo(carousel.CurrentItemChangedCommand, bind);
                TryBindTo(carousel.RemainingItemsThresholdReachedCommand, bind);
                TryBindTo(carousel.PositionChangedCommand, bind);
                break;

            case CollectionView collectionView:
                TryBindTo(collectionView.RemainingItemsThresholdReachedCommand, bind);
                TryBindTo(collectionView.SelectionChangedCommand, bind);
                break;

            case Entry entry:
                TryBindTo(entry.ReturnCommand, bind);
                break;

            case RefreshView refresh:
                TryBindTo(refresh.Command, bind);
                break;

            case SearchBar searchBar:
                TryBindTo(searchBar.SearchCommand, bind);
                break;

            default:
                TryGestureBinding(element, bind);
                break;
        }
    }

    private void TryGestureBinding(VisualElement element, bool bind)
    {
        if (element is IGestureRecognizers gestureRecognizers)
        {
            foreach (var gestureRecognizer in gestureRecognizers.GestureRecognizers)
            {
                TryBindTo(gestureRecognizer, bind);
            }
        }
    }

    private void TryBindTo(IGestureRecognizer recognizer, bool bind)
    {
        switch (recognizer)
        {
            case TapGestureRecognizer tap:
                TryBindTo(tap.Command, bind);
                break;

            case SwipeGestureRecognizer swipe:
                TryBindTo(swipe.Command, bind);
                break;

            case DragGestureRecognizer drag:
                TryBindTo(drag.DragStartingCommand, bind); // unlikely to ever be async
                TryBindTo(drag.DropCompletedCommand, bind);
                break;

            case PointerGestureRecognizer pointer:
                TryBindTo(pointer.PointerPressedCommand, bind);
                TryBindTo(pointer.PointerReleasedCommand, bind);
                TryBindTo(pointer.PointerEnteredCommand, bind); // unlikely to ever be async
                TryBindTo(pointer.PointerExitedCommand, bind);
                break;

                // no command bindings on these gestures, so they're left out
                // PinchGestureRecognizer
                // PanGestureRecognizer
        }
    }

    private void TryBindTo(ICommand? command, bool bind)
    {
        const string isSubscribedProperty = "IsSubscribed";

        if (!bind || command is not IAsyncRelayCommand relayCommand)
        {
            return;
        }

        // since events can retrigger binding pickups, this ensures we don't hook up event handlers more than once.
        if (!relayCommand.GetFused<bool>(isSubscribedProperty))
        {
            lock (relayCommand)
            {
                if (!relayCommand.GetFused<bool>(isSubscribedProperty))
                {
                    relayCommand.PropertyChanged += RelayCommandOnPropertyChanged;
                    relayCommand.SetFused(isSubscribedProperty, true);
                }
            }
        }
    }

    private void RelayCommandOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IAsyncRelayCommand.IsRunning))
        {
            return;
        }

        var relay = (IAsyncRelayCommand)sender!;
        if (relay.IsRunning)
        {
            // Note that we may be creating a transaction here and if so we explicitly don't store it on
            // Scope.Transaction, because Scope.Transaction is AsyncLocal<T> and MAUI Apps have a global scope. The
            // results would be that we would store the transaction on the scope, but it would never be cleared again,
            // since the next call to OnPropertyChanged for this RelayCommand will (likely) be from a different thread.
            var span = hub.StartSpan(SpanName, SpanOp);
            relay.SetFused(span);
        }
        else if (relay.GetFused<ISpan>() is { } span)
        {
            span.Finish();
        }
    }
}
