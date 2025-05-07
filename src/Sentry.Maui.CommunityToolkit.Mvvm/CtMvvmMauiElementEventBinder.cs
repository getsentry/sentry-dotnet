using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Sentry.Maui.CommunityToolkitMvvm;

/// <summary>
/// Scans all elements for known commands that are implement
/// </summary>
public class CtMvvmMauiElementEventBinder(IHub hub) : IMauiElementEventBinder
{
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
        if (command is IAsyncRelayCommand relayCommand)
        {
            // since events can retrigger binding pickups, we want to ensure we unhook any previous event handlers
            // instead of storing a ref to know if we've already bound to an event or not
            relayCommand.PropertyChanged -= RelayCommandOnPropertyChanged;
            if (bind)
            {
                relayCommand.PropertyChanged += RelayCommandOnPropertyChanged;
            }
        }
    }


    private readonly ConcurrentDictionary<IAsyncRelayCommand, ISpan> _contexts = new();
    private const string SpanName = "ctmvvm";
    private const string SpanOp = "relay.command";

    private void RelayCommandOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAsyncRelayCommand.IsRunning))
        {
            var relay = (IAsyncRelayCommand)sender!;

            if (relay.IsRunning)
            {
                var span = hub.StartSpan(SpanName, SpanOp);
                if (span is ITransactionTracer transaction)
                {
                    hub.ConfigureScope(x => x.Transaction ??= transaction);
                }
                _contexts.TryAdd(relay, span);
            }
            else if (_contexts.TryGetValue(relay, out var value))
            {
                value.Finish();
                _contexts.TryRemove(relay, out _);
            }
        }
    }
}
