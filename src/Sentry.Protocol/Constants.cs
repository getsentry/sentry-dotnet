namespace Sentry.Protocol
{
    public static class Constants
    {
        // https://docs.sentry.io/clientdev/overview/#usage-for-end-users
        public const string DisableSdkDsnValue = "";
        public const int DefaultMaxBreadcrumbs = 100;
        public const int ProtocolVersion = 7;
        internal const string Platform = "csharp";
    }
}
