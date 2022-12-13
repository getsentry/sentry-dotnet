namespace Sentry.Maui.Internal;

internal interface IMauiEventsBinder
{
    void BindApplicationEvents(Application application);

    void BindElementEvents(Element element);
}
