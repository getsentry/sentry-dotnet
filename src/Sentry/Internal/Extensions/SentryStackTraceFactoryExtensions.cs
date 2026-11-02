using Sentry.Extensibility;

namespace Sentry.Internal.Extensions;

internal static class SentryStackTraceFactoryExtensions
{
    public static SentryStackTrace? TryCreate(this ISentryStackTraceFactory factory, SentryOptions options, Exception? exception = null)
    {
        try
        {
            return factory.Create(exception);
        }
        catch (Exception e)
        {
            options.LogError(e, "Stack trace factory {0} threw an exception. The stack trace will be omitted.", factory.GetType().Name);
            return null;
        }
    }
}
