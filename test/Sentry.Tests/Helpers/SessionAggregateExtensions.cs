namespace Sentry.Tests.Helpers
{
    internal static class SessionAggregateExtensions
    {
        public static bool HasCountForStatus(this SessionAggregate aggregate, SessionEndStatus status)
        {
            if (status == SessionEndStatus.Exited)
            {
                return aggregate.ExitedCount > 0;
            }
            return aggregate.ErroredCount > 0;
        }
    }
}
