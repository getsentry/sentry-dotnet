namespace Sentry.Protocol
{
    public static class Constants
    {
        // https://docs.sentry.io/clientdev/overview/#usage-for-end-users
        public const string DisableSdkDsnValue = "";
        public const int DefaultMaxBreadcrumbs = 100;
        public const int ProtocolVersion = 7;

        /// <summary>
        /// Platform key that defines an events is coming from any .NET implementation
        /// </summary>
        public const string Platform = "csharp";
    }
}
