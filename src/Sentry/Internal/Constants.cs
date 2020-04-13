namespace Sentry.Internal
{
    /// <summary>
    /// Internal Constant Values.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// Sentry DSN environment variable.
        /// </summary>
        public const string DsnEnvironmentVariable = "SENTRY_DSN";
        /// <summary>
        /// Sentry release environment variable.
        /// </summary>
        public const string ReleaseEnvironmentVariable = "SENTRY_RELEASE";
        /// <summary>
        /// Sentry environment, environment variable.
        /// </summary>
        public const string EnvironmentEnvironmentVariable = "SENTRY_ENVIRONMENT";

        // See: https://github.com/getsentry/sentry-release-registry
        public const string SdkName = "sentry.dotnet";
    }
}
