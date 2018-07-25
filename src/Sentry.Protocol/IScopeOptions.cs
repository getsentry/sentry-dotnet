namespace Sentry.Protocol
{
    /// <summary>
    /// Scope options
    /// </summary>
    public interface IScopeOptions
    {
        /// <summary>
        /// The maximum breadcrumbs to keep before dropping older ones.
        /// </summary>
        int MaxBreadcrumbs { get; }
    }
}
