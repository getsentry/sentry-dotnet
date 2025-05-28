using Sentry.Internal;

namespace Sentry.Maui.Internal;

internal static class PageNavigationExtensions
{
    private static readonly PropertyInfo? DestinationPageProperty;
    private static readonly PropertyInfo? PreviousPageProperty;

    [UnconditionalSuppressMessage("Trimming", "IL2075: DynamicallyAccessedMembers", Justification = AotHelper.AvoidAtRuntime)]
    static PageNavigationExtensions()
    {
        if (AotHelper.IsTrimmed)
        {
            return;
        }
        DestinationPageProperty = typeof(NavigatedFromEventArgs)
            .GetProperty("DestinationPage", BindingFlags.Instance | BindingFlags.NonPublic);
        PreviousPageProperty = typeof(NavigatedToEventArgs)
            .GetProperty("PreviousPage", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    /// <summary>
    /// Reads the (internal) NavigatedFromEventArgs.DestinationPage property via reflection.
    /// Note that this will return null if trimming is enabled.
    /// </summary>
    public static Page? GetDestinationPage(this NavigatedFromEventArgs eventArgs) =>
        DestinationPageProperty?.GetValue(eventArgs) as Page;

    /// <summary>
    /// Reads the (internal) NavigatedFromEventArgs.PreviousPage property via reflection.
    /// Note that this will return null if trimming is enabled.
    /// </summary>
    public static Page? GetPreviousPage(this NavigatedToEventArgs eventArgs) =>
        PreviousPageProperty?.GetValue(eventArgs) as Page;
}
