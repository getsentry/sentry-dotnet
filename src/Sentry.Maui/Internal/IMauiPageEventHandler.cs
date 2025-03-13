namespace Sentry.Maui.Internal;

/// <summary>
/// Allows you to receive MAUI page level events without hooking (this list is NOT exhaustive at this time)
/// </summary>
public interface IMauiPageEventHandler
{
    /// <summary>
    /// Page.OnAppearing
    /// </summary>
    /// <param name="page"></param>
    void OnAppearing(Page page);

    /// <summary>
    /// Page.OnDisappearing
    /// </summary>
    /// <param name="page"></param>
    void OnDisappearing(Page page);

    /// <summary>
    /// Page.OnNavigatedTo
    /// </summary>
    /// <param name="page"></param>
    void OnNavigatedTo(Page page);

    /// <summary>
    /// Page.OnNavigatedFrom
    /// </summary>
    /// <param name="page"></param>
    void OnNavigatedFrom(Page page);
}
