namespace Sentry.Maui.Internal;

internal interface IMauiEventsBinder
{
    void HandleApplicationEvents(Application application, bool bind = true);
}
