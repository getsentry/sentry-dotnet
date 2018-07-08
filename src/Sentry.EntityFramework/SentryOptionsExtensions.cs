using Sentry.EntityFramework.ErrorProcessors;

namespace Sentry.EntityFramework
{
    public static class SentryOptionsExtensions
    {
        public static SentryOptions AddEntityFramework(this SentryOptions sentryOptions)
        {
            sentryOptions.AddExceptionProcessor(new DbEntityValidationExceptionProcessor());
            sentryOptions.AddExceptionProcessor(new ConcurrencyExceptionHandler());
            return sentryOptions;
        }
    }
}
