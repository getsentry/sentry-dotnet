namespace Sentry.Maui.Internal;

internal static class PageNavigationExtensions
{
    private static readonly PropertyInfo? DestinationPageProperty =
        typeof(NavigatedFromEventArgs).GetProperty("DestinationPage", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly PropertyInfo? PreviousPageProperty =
        typeof(NavigatedToEventArgs).GetProperty("PreviousPage", BindingFlags.Instance | BindingFlags.NonPublic);

    public static Page? GetDestinationPage(this NavigatedFromEventArgs eventArgs) =>
        DestinationPageProperty?.GetValue(eventArgs) as Page;

    public static Page? GetPreviousPage(this NavigatedToEventArgs eventArgs) =>
        PreviousPageProperty?.GetValue(eventArgs) as Page;
}
