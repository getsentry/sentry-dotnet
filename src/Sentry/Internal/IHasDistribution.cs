namespace Sentry.Internal;
// NOTE: We only need this interface because IEventLike is public and thus we can't
// add more properties without introducing a potentially breaking change.
// TODO: Move the Distribution property to IEventLike in the next major release.

internal interface IHasDistribution
{
    /// <summary>
    /// The release distribution of the application.
    /// </summary>
    public string? Distribution { get; set; }
}

internal static class HasDistributionExtensions
{
    internal static string? GetDistribution(this IEventLike obj) =>
        (obj as IHasDistribution)?.Distribution;

    internal static void WithDistribution(this IEventLike obj, Action<IHasDistribution> action)
    {
        if (obj is IHasDistribution hasDistribution)
        {
            action.Invoke(hasDistribution);
        }
    }
}
