using System;
using BenchmarkDotNet.Attributes;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Benchmarks
{
    public class ScopeManagementBenchmarks
    {
        private SentryScopeManager _scopeManager;

        [IterationSetup]
        public void IterationSetup() => _scopeManager = new SentryScopeManager(new SentryOptions(), DisabledHub.Instance);

        [IterationCleanup]
        public void IterationCleanup() => _scopeManager.Dispose();

        [Params(1, 10, 100)]
        public int Depth;

        [Benchmark(Description = "Scope Push/Pop: Recursively")]
        public void PushScope_Recursively()
        {
            PushScope(Depth).Dispose();

            IDisposable PushScope(int i)
            {
                if (i == 0)
                {
                    return DisabledHub.Instance;
                }
                using (_scopeManager.PushScope())
                {
                    return PushScope(i - 1);
                }
            }
        }
    }
}
