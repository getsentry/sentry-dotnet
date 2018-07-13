using System;
using BenchmarkDotNet.Attributes;

namespace Sentry.Benchmarks
{
    // Only really affects application startup time
    public class SentrySdkInitAndDisposeBenchmarks
    {
        [GlobalSetup(Target = nameof(Init_Dispose_DsnEnvVar))]
        public void SetDsnToEnvironmentVariable()
            => Environment.SetEnvironmentVariable("SENTRY_DSN", Constants.ValidDsn, EnvironmentVariableTarget.Process);

        [GlobalCleanup(Target = nameof(Init_Dispose_DsnEnvVar))]
        public void UnsetDsnToEnvironmentVariable()
            => Environment.SetEnvironmentVariable("SENTRY_DSN", null, EnvironmentVariableTarget.Process);

        [Benchmark(Baseline = true, Description = "Init/Dispose: no DSN provided, disabled SDK")]
        public void Init_Dispose_NoDsnFound() => SentrySdk.Init().Dispose();

        [Benchmark(Description = "Init/Dispose: DSN provided via parameter, enabled SDK")]
        public void Init_Dispose_WithDsn() => SentrySdk.Init(Constants.ValidDsn).Dispose();

        [Benchmark(Description = "Init/Dispose: DSN via env var, enabled SDK")]
        public void Init_Dispose_DsnEnvVar() => SentrySdk.Init(Constants.ValidDsn).Dispose();
    }
}
