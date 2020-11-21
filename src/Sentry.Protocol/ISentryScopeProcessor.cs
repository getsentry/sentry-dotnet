namespace Sentry.Protocol
{
    public interface ISentryScopeProcessor
    {
        void Apply(IScope scope, object state);
    }
}
