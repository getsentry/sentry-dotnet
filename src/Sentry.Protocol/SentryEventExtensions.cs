namespace Sentry.Protocol
{
    public static class SentryEventExtensions
    {
        public static bool HasUser(this SentryEvent evt) => evt.InternalUser != null;
    }
}
