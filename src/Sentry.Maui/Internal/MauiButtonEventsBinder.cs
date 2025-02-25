namespace Sentry.Maui.Internal;

/// <inheritdoc />
public class MauiButtonEventsBinder : IMauiElementEventBinder
{
    private Action<BreadcrumbEvent>? addBreadcrumbCallback;

    /// <inheritdoc />
    public void Bind(VisualElement element, Action<BreadcrumbEvent> addBreadcrumb)
    {
        addBreadcrumbCallback = addBreadcrumb;

        if (element is Button button)
        {
            button.Clicked += OnButtonOnClicked;
            button.Pressed += OnButtonOnPressed;
            button.Released += OnButtonOnReleased;
        }
    }

    /// <inheritdoc />
    public void UnBind(VisualElement element)
    {
        if (element is Button button)
        {
            button.Clicked -= OnButtonOnClicked;
            button.Pressed -= OnButtonOnPressed;
            button.Released -= OnButtonOnReleased;
        }
    }


    private void OnButtonOnClicked(object? sender, EventArgs _)
        => addBreadcrumbCallback?.Invoke(new(sender, nameof(Button.Clicked)));

    private void OnButtonOnPressed(object? sender, EventArgs _)
        => addBreadcrumbCallback?.Invoke(new(sender, nameof(Button.Pressed)));

    private void OnButtonOnReleased(object? sender, EventArgs _)
        => addBreadcrumbCallback?.Invoke(new(sender, nameof(Button.Released)));
}
