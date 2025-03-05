namespace Sentry.Extensibility;

/// <summary>
/// A filter to be applied to an exception instance.
/// </summary>
public interface IExceptionFilter
{
    /// <summary>
    /// Whether to filter out or not the exception.
    /// </summary>
    /// <param name="ex">The exception about to be captured.</param>
    /// <returns><c>true</c> if [the event should be filtered out]; otherwise, <c>false</c>.</returns>
    public bool Filter(Exception ex);
}
