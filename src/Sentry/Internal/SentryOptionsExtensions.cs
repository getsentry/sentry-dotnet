using System;

namespace Sentry.Internal
{
    internal static class SentryOptionsExtensions
    {
        internal static readonly Lazy<string> Release = new Lazy<string>(ReleaseLocator.GetCurrent);

        /// <summary>
        /// Applies the options to the event
        /// </summary>
        public static void Apply(this SentryOptions options, SentryEvent evt)
        {
            if (evt.Release == null)
            {
                evt.Release = options.Release ?? Release.Value;
            }
        }
    }
}
