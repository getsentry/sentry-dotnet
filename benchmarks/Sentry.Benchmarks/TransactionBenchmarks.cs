using BenchmarkDotNet.Attributes;

namespace Sentry.Benchmarks;

[SimpleJob(launchCount:1, warmupCount: 1, invocationCount: 10, iterationCount: 10)]
public class TransactionBenchmarks
{
    private const string Operation = "Operation";
    private const string Name = "Name";

    [Params(1, 10, 100)]
    public int SpanCount;

    private IDisposable _sdk;

    [GlobalSetup]
    public void EnabledSdk() => _sdk = SentrySdk.Init(o =>
    {
        o.Dsn = Constants.ValidDsn;
        o.TracesSampleRate = 1.0;
    });

    [GlobalCleanup]
    public void DisableDsk() => _sdk.Dispose();

    [Benchmark(Description = "ConcurrentBag")]
    public void ConcurrentBag()
    {
        TransactionTracer.UseConcurrentBag = true;

        var transaction = SentrySdk.StartTransaction(Name, Operation);

        for (var i = 0; i < SpanCount; i++)
        {
            var span = transaction.StartChild(Operation);
            span.Finish();
        }

        transaction.Finish();
    }

    [Benchmark(Description = "SyncCollection")]
    public void SynchronizedCollection()
    {
        TransactionTracer.UseConcurrentBag = false;

        var transaction = SentrySdk.StartTransaction(Name, Operation);

        for (var i = 0; i < SpanCount; i++)
        {
            var span = transaction.StartChild(Operation);
            span.Finish();
        }

        transaction.Finish();
    }
}
