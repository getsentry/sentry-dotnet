namespace Sentry.Internal
{
    internal static class Constants
    {
        public const string DsnEnvironmentVariable = "SENTRY_DSN";
        public const string ReleaseEnvironmentVariable = "SENTRY_RELEASE";
        public const string EnvironmentEnvironmentVariable = "SENTRY_ENVIRONMENT";

        // https://docs.sentry.io/clientdev/overview/#usage-for-end-users
        public const string DisableSdkDsnValue = "";

        public const int DefaultMaxBreadcrumbs = 100;

        public const int ProtocolVersion = 7;

        public const string SdkName = "Sentry.NET";
        public const string Platform = "csharp";
    }
}
