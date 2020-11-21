using System;

namespace Sentry.Internal
{
    internal class Disposable : IDisposable
    {
        private readonly Action _dispose;

        public Disposable(Action dispose) => _dispose = dispose;

        public void Dispose() => _dispose();

        public static IDisposable Create(Action dispose) =>
            new Disposable(dispose);
    }
}
