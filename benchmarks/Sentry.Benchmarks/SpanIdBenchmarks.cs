using BenchmarkDotNet.Attributes;

namespace Sentry.Benchmarks;

public class SpanIdBenchmarks
{
    [Benchmark(Description = "Creates a Span ID")]
    public void CreateSpanId()
    {
        SpanId.Create();
    }
}
