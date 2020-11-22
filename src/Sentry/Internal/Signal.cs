using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Internal
{
    internal class Signal : IDisposable
    {
        private readonly object _lock = new object();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);

        public Signal(bool isReleasedInitially = false)
        {
            if (isReleasedInitially)
            {
                Release();
            }
        }

        public void Release()
        {
            // Make sure the semaphore does not go above 1
            lock (_lock)
            {
                if (_semaphore.CurrentCount >= 1)
                {
                    return;
                }

                _semaphore.Release();
            }
        }

        public async Task WaitAsync(CancellationToken cancellationToken = default) =>
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        public void Dispose() => _semaphore.Dispose();
    }
}
