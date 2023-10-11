namespace Sentry;

/// <summary>
/// Sentry User Factory
/// </summary>
public interface ISentryUserFactory
{
    /// <summary>
    /// Creates a Sentry <see cref="User"/> representing the current principal.
    /// </summary>
    /// <returns>The protocol user</returns>
    public User? Create();
}
