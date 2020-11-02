using System;
using BenchmarkDotNet.Attributes;

namespace Sentry.Benchmarks
{
    public class CaptureExceptionDuplicateDetectionBenchmarks
    {
        private IDisposable _sdk;

        [Params(1, 10, 100)]
        public int EventCount;

        private static Action<SentryOptions> SharedConfig => (o =>
        {
            o.Dsn = Constants.ValidDsn;
            o.SentryHttpClientFactory = new FakeHttpClientFactory();
        });

        [GlobalSetup(Target = nameof(CaptureException_WithDuplicateDetection))]
        public void EnabledWithDuplicateDetectionSdk() => _sdk = SentrySdk.Init(o =>
        {
            o.DisableDuplicateEventDetection();
            SharedConfig(o);
        });

        [GlobalSetup(Target = nameof(CaptureException_WithoutDuplicateDetection))]
        public void EnabledWithoutDuplicateDetectionSdk() => _sdk = SentrySdk.Init(SharedConfig);

        [GlobalCleanup(Target = nameof(CaptureException_WithoutDuplicateDetection) + "," +
                                nameof(CaptureException_WithDuplicateDetection))]
        public void DisableDsk() => _sdk.Dispose();

        [Benchmark(Description = "CaptureException with duplicate detection")]
        public void CaptureException_WithoutDuplicateDetection()
        {
            for (var i = 0; i < EventCount; i++)
            {
                SentrySdk.CaptureException(new Exception());
            }
        }

        [Benchmark(Description = "CaptureException without duplicate detection")]
        public void CaptureException_WithDuplicateDetection()
        {
            for (var i = 0; i < EventCount; i++)
            {
                SentrySdk.CaptureException(new Exception());
            }
        }
    }
}
