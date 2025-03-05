namespace Sentry.EntityFramework;

/// <summary>
/// A query logger interface.
/// </summary>
public interface IQueryLogger
{
    /// <summary>
    /// Logs a query with a related level.
    /// </summary>
    /// <param name="text">The query text.</param>
    /// <param name="level">The level.</param>
    public void Log(string text, BreadcrumbLevel level = BreadcrumbLevel.Debug);
}
