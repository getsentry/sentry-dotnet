namespace Sentry.Protocol
{
    /// <summary>
    /// Defines the logic for applying state onto a scope.
    /// </summary>
    public interface ISentryScopeProcessor
    {
        /// <summary>
        /// Applies state onto a scope.
        /// </summary>
        void Apply(BaseScope scope, object state);
    }
}
