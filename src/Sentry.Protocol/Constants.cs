namespace Sentry.Protocol
{
    /// <summary>
    /// Constant values.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Empty string disables the SDK.
        /// </summary>
        /// <see href="https://docs.sentry.io/clientdev/overview/#usage-for-end-users"/>
        public const string DisableSdkDsnValue = "";
        /// <summary>
        /// Default maximum number of breadcrumbs to hold in memory.
        /// </summary>
        public const int DefaultMaxBreadcrumbs = 100;
        /// <summary>
        /// Protocol version.
        /// </summary>
        public const int ProtocolVersion = 7;
        /// <summary>
        /// Platform key that defines an events is coming from any .NET implementation
        /// </summary>
        public const string Platform = "csharp";
    }
}
