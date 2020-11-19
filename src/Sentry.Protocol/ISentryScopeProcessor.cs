namespace Sentry.Protocol
{
    public interface ISentryScopeProcessor
    {
        void Apply(BaseScope scope, object state);
    }
}
