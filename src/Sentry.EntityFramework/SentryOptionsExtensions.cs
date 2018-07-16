using System.ComponentModel;
using Sentry.EntityFramework.ErrorProcessors;

// ReSharper disable once CheckNamespace - Make it visible without: using Sentry.EntityFramework
namespace Sentry
{
    /// <summary>
    /// Extension methods to <see cref="SentryOptions"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryOptionsExtensions
    {
        /// <summary>
        /// Adds the entity framework integration.
        /// </summary>
        /// <param name="sentryOptions">The sentry options.</param>
        /// <returns></returns>
        public static SentryOptions AddEntityFramework(this SentryOptions sentryOptions)
        {
            sentryOptions.AddExceptionProcessor(new DbEntityValidationExceptionProcessor());
            // DbConcurrencyExceptionProcessor is untested due to problems with testing it, so it might not be production ready
            //sentryOptions.AddExceptionProcessor(new DbConcurrencyExceptionProcessor());
            return sentryOptions;
        }
    }
}
