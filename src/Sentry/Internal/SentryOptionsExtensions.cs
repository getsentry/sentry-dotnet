namespace Sentry.Internal
{
    internal static class SentryOptionsExtensions
    {
        /// <summary>
        /// Applies the options to the event
        /// </summary>
        public static void Apply(this SentryOptions options, SentryEvent evt)
        {
            if (evt.Release == null)
            {
                evt.Release = options.Release ?? ApplicationVersionLocator.GetCurrent();
            }
        }
    }
}
