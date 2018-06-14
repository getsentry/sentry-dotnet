namespace Sentry.Internal
{
    internal static class Constants
    {
        public const string DsnEnvironmentVariable = "SENTRY_DSN";
        // https://docs.sentry.io/clientdev/overview/#usage-for-end-users
        public const string DisableSdkDsnValue = "";

        public const int DefaultMaxBreadcrumbs = 100;

        public const int ProtocolVersion = 7;
    }
}
