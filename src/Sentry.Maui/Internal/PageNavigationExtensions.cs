using Sentry.Internal;

namespace Sentry.Maui.Internal;

internal static class PageNavigationExtensions
{
#if !NET10_0_OR_GREATER
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
#endif

    /// <summary>
    /// Gets the destination page from navigation arguments.
    /// </summary>
    public static Page? GetDestinationPage(this NavigatedFromEventArgs eventArgs) =>
#if NET10_0_OR_GREATER
        eventArgs.DestinationPage;
#else
        DestinationPageProperty?.GetValue(eventArgs) as Page;
#endif

    /// <summary>
    /// Gets the previous page from navigation arguments.
    /// </summary>
    public static Page? GetPreviousPage(this NavigatedToEventArgs eventArgs) =>
#if NET10_0_OR_GREATER
        eventArgs.PreviousPage;
#else
        PreviousPageProperty?.GetValue(eventArgs) as Page;
#endif
}
