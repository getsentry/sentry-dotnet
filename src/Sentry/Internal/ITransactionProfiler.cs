using Sentry.Protocol;

namespace Sentry.Internal;

internal interface ITransactionProfiler
{
    void OnTransactionStart(ITransaction transaction);

    ProfileInfo? OnTransactionFinish(Transaction transaction);
}
