namespace Sentry.Extensibility
{
    /// <summary>
    /// SDK API contract which combines a client and scope management
    /// </summary>
    /// <remarks>
    /// The contract of which <see cref="T:Sentry.SentryCore" /> exposes statically.
    /// This interface exist to allow better testability of integrations which otherwise
    /// would require dependency to the static <see cref="T:Sentry.SentryCore" />
    /// </remarks>
    /// <inheritdoc cref="ISentryClient" />
    /// <inheritdoc cref="ISentryScopeManager" />
    public interface IHub :
        ISentryClient,
        ISentryScopeManager
    {
    }
}
