namespace Sentry.Maui;

/// <summary>
/// Bind to MAUI controls to generate breadcrumbs and other metrics
/// </summary>
public interface IMauiElementEventBinder
{
    /// <summary>
    /// Bind to an element
    /// </summary>
    /// <param name="element"></param>
    /// <param name="addBreadcrumb">
    /// This adds a breadcrumb to the sentry hub
    /// NOTE: we will override the type, timestamp, and category of the breadcrumb
    /// </param>
    public void Bind(VisualElement element, Action<BreadcrumbEvent> addBreadcrumb);

    /// <summary>
    /// Unbind the element because MAUI is removing the page
    /// </summary>
    /// <param name="element"></param>
    public void UnBind(VisualElement element);
}
