namespace Sentry.Protocol
{
    /// <summary>
    /// Defines the logic for applying state onto a scope.
    /// </summary>
    public interface ISentryScopeStateProcessor
    {
        /// <summary>
        /// Applies state onto a scope.
        /// </summary>
        void Apply(IScope scope, object state);
    }
}
