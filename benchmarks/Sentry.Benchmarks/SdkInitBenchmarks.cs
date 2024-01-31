using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;

namespace Sentry.Benchmarks;

[SimpleJob(RunStrategy.ColdStart, launchCount: 10, iterationCount: 1, invocationCount: 1, warmupCount: 0)]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
[JitStatsDiagnoser]
public class SdkInitBenchmarks
{
    [Benchmark(Description = "Init SDK (Full)")]
    public void Init_SDK_Full()
    {
        SentrySdk.Init(DsnSamples.ValidDsn);
    }

    [Benchmark(Description = "Init SDK (Slim)")]
    public void Init_SDK_Slim()
    {
        SentrySdk.Init(o =>
        {
            o.Dsn = DsnSamples.ValidDsn;
            o.DisableAppDomainProcessExitFlush();
            o.DisableAppDomainUnhandledExceptionCapture();
            o.DisableUnobservedTaskExceptionCapture();
            o.DisableDuplicateEventDetection();
            o.DisableDiagnosticSourceIntegration();
            o.DisableWinUiUnhandledExceptionIntegration();
            o.AutoSessionTracking = false;
            o.IsGlobalModeEnabled = true;
            o.DetectStartupTime = StartupTimeDetectionMode.Fast;
            o.TracesSampleRate = 0;
        });
    }
}
