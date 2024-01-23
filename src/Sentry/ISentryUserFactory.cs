namespace Sentry;

/// <summary>
/// Sentry User Factory
/// </summary>
public interface ISentryUserFactory
{
    /// <summary>
    /// Creates a Sentry <see cref="SentryUser"/> representing the current principal.
    /// </summary>
    /// <returns>The protocol user</returns>
    public SentryUser? Create();
}
