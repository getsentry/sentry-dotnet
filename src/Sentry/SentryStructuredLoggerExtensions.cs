namespace Sentry;

internal static class SentryStructuredLoggerExtensions
{
    internal static void Flush(this SentryStructuredLogger logger)
    {
        logger.Flush(Timeout.InfiniteTimeSpan);
    }
}
