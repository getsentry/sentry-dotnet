namespace Sentry.Protocol
{
    /// <summary>
    /// The level of the Breadcrumb
    /// </summary>
    public enum BreadcrumbLevel
    {
        Debug = -1,
        Info = 0, // Defaults to Info 
        Warning = 1,
        Error = 2,
        Critical = 3,
    }
}
