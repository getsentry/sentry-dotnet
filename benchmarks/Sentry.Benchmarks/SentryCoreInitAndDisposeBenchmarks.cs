using System;
using BenchmarkDotNet.Attributes;

namespace Sentry.Benchmarks
{
    // Only really affects application startup time
    public class SentryCoreInitAndDisposeBenchmarks
    {
        [GlobalSetup(Target = nameof(Init_Dispose_DsnEnvVar))]
        public void SetDsnToEnvironmentVariable()
            => Environment.SetEnvironmentVariable(Internal.Constants.DsnEnvironmentVariable, Constants.ValidDsn, EnvironmentVariableTarget.Process);

        [GlobalCleanup(Target = nameof(Init_Dispose_DsnEnvVar))]
        public void UnsetDsnToEnvironmentVariable()
            => Environment.SetEnvironmentVariable(Internal.Constants.DsnEnvironmentVariable, null, EnvironmentVariableTarget.Process);

        [Benchmark(Baseline = true, Description = "Init/Dispose: no DSN provided, disabled SDK")]
        public void Init_Dispose_NoDsnFound() => SentryCore.Init().Dispose();

        [Benchmark(Description = "Init/Dispose: DSN provided via parameter, enabled SDK")]
        public void Init_Dispose_WithDsn() => SentryCore.Init(Constants.ValidDsn).Dispose();

        [Benchmark(Description = "Init/Dispose: DSN via env var, enabled SDK")]
        public void Init_Dispose_DsnEnvVar() => SentryCore.Init(Constants.ValidDsn).Dispose();
    }
}
