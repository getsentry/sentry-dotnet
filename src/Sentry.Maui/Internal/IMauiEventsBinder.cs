namespace Sentry.Maui.Internal;

internal interface IMauiEventsBinder
{
    void BindApplicationEvents(Application application);

    void BindWindowEvents(Window window);

    void BindElementEvents(Element element);

    void BindVisualElementEvents(VisualElement element);

    void BindShellEvents(Shell shell);

    void BindPageEvents(Page page);

    void BindButtonEvents(Button button);
}
