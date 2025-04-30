namespace Sentry.Maui.Internal;

/// <summary>
/// Detects and breadcrumbs any gesture recognizers attached to the visual element
/// </summary>
public class MauiGestureRecognizerEventsBinder : IMauiElementEventBinder
{
    private static Action<BreadcrumbEvent>? _addBreadcrumb = null!;

    /// <summary>
    /// Searches VisualElement for gesture recognizers to bind to
    /// </summary>
    public void Bind(VisualElement element, Action<BreadcrumbEvent> addBreadcrumb)
    {
        _addBreadcrumb ??= addBreadcrumb; // this is fine... it's the same callback for everyone and it never changes
        TryBind(element, true);
    }


    /// <summary>
    /// Searches VisualElement for gesture recognizers to unbind from
    /// </summary>
    /// <param name="element"></param>
    public void UnBind(VisualElement element)
    {
        _addBreadcrumb = null;
        TryBind(element, false);
    }

    private static void TryBind(VisualElement element, bool bind)
    {
        if (element is IGestureRecognizers recognizers)
        {
            foreach (var recognizer in recognizers.GestureRecognizers)
            {
                SetHooks(recognizer, bind);
            }
        }
    }


    private static void SetHooks(IGestureRecognizer recognizer, bool bind)
    {
        switch (recognizer)
        {
            case TapGestureRecognizer tap:
                tap.Tapped -= OnTapGesture;

                if (bind)
                {
                    tap.Tapped += OnTapGesture;
                }
                break;

            case SwipeGestureRecognizer swipe:
                swipe.Swiped -= OnSwipeGesture;

                if (bind)
                {
                    swipe.Swiped += OnSwipeGesture;
                }
                break;

            case PinchGestureRecognizer pinch:
                pinch.PinchUpdated -= OnPinchGesture;

                if (bind)
                {
                    pinch.PinchUpdated += OnPinchGesture;
                }
                break;

            case DragGestureRecognizer drag:
                drag.DragStarting -= OnDragStartingGesture;
                drag.DropCompleted -= OnDropCompletedGesture;

                if (bind)
                {
                    drag.DragStarting += OnDragStartingGesture;
                    drag.DropCompleted += OnDropCompletedGesture;
                }
                break;

            case PanGestureRecognizer pan:
                pan.PanUpdated -= OnPanGesture;

                if (bind)
                {
                    pan.PanUpdated += OnPanGesture;
                }
                break;

            case PointerGestureRecognizer pointer:
                pointer.PointerEntered -= OnPointerEnteredGesture;
                pointer.PointerExited -= OnPointerExitedGesture;
                pointer.PointerMoved -= OnPointerMovedGesture;
                pointer.PointerPressed -= OnPointerPressedGesture;
                pointer.PointerReleased -= OnPointerReleasedGesture;

                if (bind)
                {
                    pointer.PointerEntered += OnPointerEnteredGesture;
                    pointer.PointerExited += OnPointerExitedGesture;
                    pointer.PointerMoved += OnPointerMovedGesture;
                    pointer.PointerPressed += OnPointerPressedGesture;
                    pointer.PointerReleased += OnPointerReleasedGesture;
                }
                break;
        }
    }

    private static void OnPointerReleasedGesture(object? sender, PointerEventArgs e) => _addBreadcrumb?.Invoke(new(
        sender,
        nameof(PointerGestureRecognizer.PointerReleased),
        ToPointerData(e)
    ));

    private static void OnPointerPressedGesture(object? sender, PointerEventArgs e) => _addBreadcrumb?.Invoke(new(
        sender,
        nameof(PointerGestureRecognizer.PointerPressed),
        ToPointerData(e)
    ));

    private static void OnPointerMovedGesture(object? sender, PointerEventArgs e) => _addBreadcrumb?.Invoke(new(
        sender,
        nameof(PointerGestureRecognizer.PointerMoved),
        ToPointerData(e)
    ));

    private static void OnPointerExitedGesture(object? sender, PointerEventArgs e) => _addBreadcrumb?.Invoke(new(
        sender,
        nameof(PointerGestureRecognizer.PointerExited),
        ToPointerData(e)
    ));

    private static void OnPointerEnteredGesture(object? sender, PointerEventArgs e) => _addBreadcrumb?.Invoke(new(
        sender,
        nameof(PointerGestureRecognizer.PointerEntered),
        ToPointerData(e)
    ));

    private static IEnumerable<(string Key, string Value)> ToPointerData(PointerEventArgs e) =>
    [
        // some of the data here may have some challenges being pulled out
        #if ANDROID
        ("MotionEventAction", e.PlatformArgs?.MotionEvent.Action.ToString() ?? string.Empty)
        //("MotionEventActionButton", e.PlatformArgs?.MotionEvent.ActionButton.ToString() ?? String.Empty)
        #elif IOS
        ("State", e.PlatformArgs?.GestureRecognizer.State.ToString() ?? string.Empty),
        //("ButtonMask", e.PlatformArgs?.GestureRecognizer.ButtonMask.ToString() ?? String.Empty)
        #endif
    ];

    private static void OnPanGesture(object? sender, PanUpdatedEventArgs e) => _addBreadcrumb?.Invoke(new(
        sender,
        nameof(PanGestureRecognizer.PanUpdated),
        [
            ("GestureId", e.GestureId.ToString()),
            ("StatusType", e.StatusType.ToString()),
            ("TotalX", e.TotalX.ToString()),
            ("TotalY", e.TotalY.ToString())
        ]
    ));

    private static void OnDropCompletedGesture(object? sender, DropCompletedEventArgs e) => _addBreadcrumb?.Invoke(new(
        sender,
        nameof(DragGestureRecognizer.DropCompleted)
    ));

    private static void OnDragStartingGesture(object? sender, DragStartingEventArgs e) => _addBreadcrumb?.Invoke(new(
        sender,
        nameof(DragGestureRecognizer.DragStarting)
    ));


    private static void OnPinchGesture(object? sender, PinchGestureUpdatedEventArgs e) => _addBreadcrumb?.Invoke(new(
        sender,
        nameof(PinchGestureRecognizer.PinchUpdated),
        [
            ("GestureStatus", e.Status.ToString()),
            ("Scale", e.Scale.ToString()),
            ("ScaleOrigin", e.ScaleOrigin.ToString())
        ]
    ));

    private static void OnSwipeGesture(object? sender, SwipedEventArgs e) => _addBreadcrumb?.Invoke(new(
        sender,
        nameof(SwipeGestureRecognizer.Swiped),
        [("Direction", e.Direction.ToString())]
    ));

    private static void OnTapGesture(object? sender, TappedEventArgs e) => _addBreadcrumb?.Invoke(new(
        sender,
        nameof(TapGestureRecognizer.Tapped),
        [("ButtonMask", e.Buttons.ToString())]
    ));
}
