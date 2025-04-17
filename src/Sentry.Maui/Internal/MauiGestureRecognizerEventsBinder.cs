namespace Sentry.Maui.Internal;

/// <summary>
/// Binds to
/// </summary>
public class MauiGestureRecognizerEventsBinder : IMauiElementEventBinder
{
    private Action<BreadcrumbEvent> _addBreadcrumb = null!;

    /// <summary>
    /// Searches VisualElement for gesture recognizers to bind to
    /// </summary>
    public void Bind(VisualElement element, Action<BreadcrumbEvent> addBreadcrumb)  => TryBind(element, true);

    /// <summary>
    /// Searches VisualElement for gesture recognizers to unbind from
    /// </summary>
    /// <param name="element"></param>
    public void UnBind(VisualElement element) => TryBind(element, false);

    private void TryBind(VisualElement element, bool bind)
    {
        if (element is IGestureRecognizers recognizers)
        {
            foreach (var recognizer in recognizers.GestureRecognizers)
            {
                SetHooks(recognizer, bind);
            }
        }
    }


    private void SetHooks(IGestureRecognizer recognizer, bool bind)
    {
        switch (recognizer)
        {
            case TapGestureRecognizer tap:
                if (bind)
                {
                    tap.Tapped += OnTapGesture;
                }
                else
                {
                    tap.Tapped -= OnTapGesture;
                }
                break;

            case SwipeGestureRecognizer swipe:
                if (bind)
                {
                    swipe.Swiped += OnSwipeGesture;
                }
                else
                {
                    swipe.Swiped -= OnSwipeGesture;
                }
                break;

            case PinchGestureRecognizer pinch:
                if (bind)
                {
                    pinch.PinchUpdated += OnPinchGesture;
                }
                else
                {
                    pinch.PinchUpdated -= OnPinchGesture;
                }
                break;

            case DragGestureRecognizer drag:
                if (bind)
                {
                    drag.DragStarting += OnDragStartingGesture;
                    drag.DropCompleted += OnDropCompletedGesture;
                }
                else
                {
                    drag.DragStarting -= OnDragStartingGesture;
                    drag.DropCompleted -= OnDropCompletedGesture;
                }
                break;

            case PanGestureRecognizer pan:
                if (bind)
                {
                    pan.PanUpdated += OnPanGesture;
                }
                else
                {
                    pan.PanUpdated -= OnPanGesture;
                }
                break;

            case PointerGestureRecognizer pointer:
                if (bind)
                {
                    pointer.PointerEntered += OnPointerEnteredGesture;
                    pointer.PointerExited += OnPointerExitedGesture;
                    pointer.PointerMoved += OnPointerMovedGesture;
                    pointer.PointerPressed += OnPointerPressedGesture;
                    pointer.PointerReleased += OnPointerReleasedGesture;
                }
                else
                {
                    pointer.PointerEntered -= OnPointerEnteredGesture;
                    pointer.PointerExited -= OnPointerExitedGesture;
                    pointer.PointerMoved -= OnPointerMovedGesture;
                    pointer.PointerPressed -= OnPointerPressedGesture;
                    pointer.PointerReleased -= OnPointerReleasedGesture;
                }
                break;
        }
    }

    private void OnPointerReleasedGesture(object? sender, PointerEventArgs e) => _addBreadcrumb.Invoke(new(
        sender,
        nameof(PointerGestureRecognizer.PointerReleased),
        ToPointerData(e)
    ));

    private void OnPointerPressedGesture(object? sender, PointerEventArgs e) => _addBreadcrumb.Invoke(new(
        sender,
        nameof(PointerGestureRecognizer.PointerPressed),
        ToPointerData(e)
    ));

    private void OnPointerMovedGesture(object? sender, PointerEventArgs e) => _addBreadcrumb.Invoke(new(
        sender,
        nameof(PointerGestureRecognizer.PointerMoved),
        ToPointerData(e)
    ));

    private void OnPointerExitedGesture(object? sender, PointerEventArgs e) => _addBreadcrumb.Invoke(new(
        sender,
        nameof(PointerGestureRecognizer.PointerExited),
        ToPointerData(e)
    ));

    private void OnPointerEnteredGesture(object? sender, PointerEventArgs e) => _addBreadcrumb.Invoke(new(
        sender,
        nameof(PointerGestureRecognizer.PointerEntered),
        ToPointerData(e)
    ));

    private static IEnumerable<(string Key, string Value)> ToPointerData(PointerEventArgs e) =>
    [
        // some of the data here may have some challenges being pulled out
        #if ANDROID
        ("MotionEventAction", e.PlatformArgs?.MotionEvent.Action.ToString() ?? String.Empty)
        //("MotionEventActionButton", e.PlatformArgs?.MotionEvent.ActionButton.ToString() ?? String.Empty)
        #elif IOS
        ("State", e.PlatformArgs?.GestureRecognizer.State.ToString() ?? String.Empty),
        //("ButtonMask", e.PlatformArgs?.GestureRecognizer.ButtonMask.ToString() ?? String.Empty)
        #endif
    ];

    private void OnPanGesture(object? sender, PanUpdatedEventArgs e) => _addBreadcrumb.Invoke(new(
        sender,
        nameof(PanGestureRecognizer.PanUpdated),
        [
            ("GestureId", e.GestureId.ToString()),
            ("StatusType", e.StatusType.ToString()),
            ("TotalX", e.TotalX.ToString()),
            ("TotalY", e.TotalY.ToString())
        ]
    ));

    private void OnDropCompletedGesture(object? sender, DropCompletedEventArgs e) => _addBreadcrumb.Invoke(new(
        sender,
        nameof(DragGestureRecognizer.DropCompleted)
    ));

    private void OnDragStartingGesture(object? sender, DragStartingEventArgs e) => _addBreadcrumb.Invoke(new(
        sender,
        nameof(DragGestureRecognizer.DragStarting)
    ));


    private void OnPinchGesture(object? sender, PinchGestureUpdatedEventArgs e) => _addBreadcrumb.Invoke(new(
        sender,
        nameof(PinchGestureRecognizer.PinchUpdated),
        [
            ("GestureStatus", e.Status.ToString()),
            ("Scale", e.Scale.ToString()),
            ("ScaleOrigin", e.ScaleOrigin.ToString())
        ]
    ));

    private void OnSwipeGesture(object? sender, SwipedEventArgs e) => _addBreadcrumb.Invoke(new(
        sender,
        nameof(SwipeGestureRecognizer.Swiped),
        [("Direction", e.Direction.ToString())]
    ));

    private void OnTapGesture(object? sender, TappedEventArgs e) => _addBreadcrumb.Invoke(new(
        sender,
        nameof(TapGestureRecognizer.Tapped),
        [("ButtonMask", e.Buttons.ToString())]
    ));
}
