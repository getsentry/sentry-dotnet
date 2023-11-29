namespace Sentry.Maui.Internal;

internal interface IMauiEventsBinder
{
    void BindApplicationEvents(Application application);

    void UnbindApplicationEvents(Application application);
}
