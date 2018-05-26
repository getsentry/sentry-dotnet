namespace Sentry.Protocol
{
    /// <summary>
    /// The level of the Breadcrumb
    /// </summary>
    public enum BreadcrumbLevel
    {
        /// <summary>
        /// Debug level
        /// </summary>
        Debug = -1,
        /// <summary>
        /// Information level
        /// </summary>
        /// <remarks>
        /// This is value 0, hence, default
        /// </remarks>
        Info = 0, // Defaults to Info
        /// <summary>
        /// Warning breadcrumb level
        /// </summary>
        Warning = 1,
        /// <summary>
        /// Error breadcrumb level
        /// </summary>
        Error = 2,
        /// <summary>
        /// Critical breadcrumb level
        /// </summary>
        Critical = 3,
    }
}
