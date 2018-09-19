namespace Sentry.Internal
{
    internal static class Constants
    {
        public const string DsnEnvironmentVariable = "SENTRY_DSN";
        public const string ReleaseEnvironmentVariable = "SENTRY_RELEASE";
        public const string EnvironmentEnvironmentVariable = "SENTRY_ENVIRONMENT";

        // See: https://github.com/getsentry/sentry-release-registry
        public const string SdkName = "sentry.dotnet";
    }
}
