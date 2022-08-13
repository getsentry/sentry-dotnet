using System;

namespace Sentry.Internal
{
    // NOTE: We only need these interfaces because IEventLike and ISession are public and thus we can't
    // add more properties without introducing a potentially breaking change.
    // TODO: Move the Distribution properties to those interfaces in the next major release.

    internal interface IHasDistribution
    {
        /// <summary>
        /// The release distribution of the application.
        /// </summary>
        public string? Distribution { get; set; }
    }

    internal interface IHasReadOnlyDistribution
    {
        /// <summary>
        /// The release distribution of the application.
        /// </summary>
        public string? Distribution { get; }
    }

    internal static class HasDistributionExtensions
    {
        internal static string? GetDistribution(this IEventLike obj) =>
            (obj as IHasDistribution)?.Distribution;

        internal static string? GetDistribution(this ISession obj) =>
            (obj as IHasReadOnlyDistribution)?.Distribution;

        internal static void WithDistribution(this IEventLike obj, Action<IHasDistribution> action)
        {
            if (obj is IHasDistribution hasDistribution)
            {
                action.Invoke(hasDistribution);
            }
        }
    }
}
