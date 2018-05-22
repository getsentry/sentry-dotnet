// ReSharper disable once CheckNamespace
namespace Sentry
{
    /// <summary>
    /// The level of the event sent to Sentry
    /// </summary>
    public enum SentryLevel : short
    {
        /// <summary>
        /// Fatal
        /// </summary>
        Fatal = -1,
        /// <summary>
        /// Error
        /// </summary>
        Error, // defaults to 0
        /// <summary>
        /// Warning
        /// </summary>
        Warning,
        /// <summary>
        /// Informational
        /// </summary>
        Info,
        /// <summary>
        /// Debug
        /// </summary>
        Debug
    }
}
