using System;
using BenchmarkDotNet.Attributes;
using Sentry.Extensibility;

namespace Sentry.Benchmarks
{
    public class ScopeManagementBenchmarks
    {
        [IterationSetup]
        public void IterationSetup()
        {
            SentrySdk.Init();
            SentrySdk.BindClient(DisabledHub.Instance);
        }

        [IterationCleanup]
        public void IterationCleanup() => SentrySdk.Close();

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
                using (SentrySdk.PushScope())
                {
                    return PushScope(i - 1);
                }
            }
        }
    }
}
