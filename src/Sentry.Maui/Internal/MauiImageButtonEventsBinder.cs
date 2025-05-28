namespace Sentry.Maui.Internal;

/// <inheritdoc />
public class MauiImageButtonEventsBinder : IMauiElementEventBinder
{
    private Action<BreadcrumbEvent>? addBreadcrumbCallback;

    /// <inheritdoc />
    public void Bind(VisualElement element, Action<BreadcrumbEvent> addBreadcrumb)
    {
        addBreadcrumbCallback = addBreadcrumb;

        if (element is ImageButton image)
        {
            image.Clicked += OnButtonOnClicked;
            image.Pressed += OnButtonOnPressed;
            image.Released += OnButtonOnReleased;
        }
    }

    /// <inheritdoc />
    public void UnBind(VisualElement element)
    {
        if (element is ImageButton image)
        {
            image.Clicked -= OnButtonOnClicked;
            image.Pressed -= OnButtonOnPressed;
            image.Released -= OnButtonOnReleased;
        }
    }


    private void OnButtonOnClicked(object? sender, EventArgs _)
        => addBreadcrumbCallback?.Invoke(new(sender, nameof(ImageButton.Clicked)));

    private void OnButtonOnPressed(object? sender, EventArgs _)
        => addBreadcrumbCallback?.Invoke(new(sender, nameof(ImageButton.Pressed)));

    private void OnButtonOnReleased(object? sender, EventArgs _)
        => addBreadcrumbCallback?.Invoke(new(sender, nameof(ImageButton.Released)));
}
