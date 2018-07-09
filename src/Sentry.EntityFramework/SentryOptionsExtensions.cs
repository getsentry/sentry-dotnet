using Sentry.EntityFramework.ErrorProcessors;

namespace Sentry.EntityFramework
{
    public static class SentryOptionsExtensions
    {
        public static SentryOptions AddEntityFramework(this SentryOptions sentryOptions)
        {
            sentryOptions.AddExceptionProcessor(new DbEntityValidationExceptionProcessor());
            // DbConcurrencyExceptionProcessor is untested due to problems with testing it, so it might not be production ready
            //sentryOptions.AddExceptionProcessor(new DbConcurrencyExceptionProcessor());
            return sentryOptions;
        }
    }
}
