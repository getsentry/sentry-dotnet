using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Internal
{
    internal class Lock : IDisposable
    {
        private readonly Signal _signal;

        public Lock() => _signal = new Signal(true);

        public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
        {
            await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            return Disposable.Create(_signal.Release);
        }

        public void Dispose() => _signal.Dispose();
    }
}
