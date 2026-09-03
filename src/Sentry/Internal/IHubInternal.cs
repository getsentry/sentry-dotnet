namespace Sentry.Internal;

/// <summary>
/// Internal hub interface exposing additional overloads not part of the public <see cref="IHub"/> contract.
/// Implemented by <see cref="Hub"/>, <see cref="Extensibility.DisabledHub"/>, and
/// <see cref="Extensibility.HubAdapter"/>.
/// </summary>
internal interface IHubInternal : IHub
{
    /// <summary>
    /// Starts a transaction that will automatically finish after <paramref name="idleTimeout"/> if not
    /// finished explicitly first.
    /// </summary>
    public ITransactionTracer StartTransaction(ITransactionContext context, TimeSpan? idleTimeout);
}
