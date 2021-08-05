namespace Sentry
{
    /// <summary>
    /// Observer for the sync. of Scopes across SDKs.
    /// </summary>
    public interface IScopeObserver : IHasBreadcrumbs, IHasTags, IHasExtra
    {
        /// <summary>
        /// Gets the user information.
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        User User { get; set; }
    }
}
