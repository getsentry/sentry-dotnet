namespace Sentry;

/// <summary>
/// Defines the logic for applying state onto a scope.
/// </summary>
public interface ISentryScopeStateProcessor
{
    /// <summary>
    /// Applies state onto a scope.
    /// </summary>
    public void Apply(Scope scope, object state);
}
