using BenchmarkDotNet.Attributes;

namespace Sentry.Benchmarks;

public class TransactionBenchmarks
{
    private const string Operation = "Operation";
    private const string Name = "Name";

    [Params(1, 10, 100, 1000)]
    public int SpanCount;

    private IDisposable _sdk;

    [GlobalSetup(Target = nameof(CreateTransaction))]
    public void EnabledSdk() => _sdk = SentrySdk.Init(Constants.ValidDsn);

    [GlobalCleanup(Target = nameof(CreateTransaction))]
    public void DisableDsk() => _sdk.Dispose();

    [Benchmark(Description = "Creates a Transaction")]
    public void CreateTransaction()
    {
        var transaction = SentrySdk.StartTransaction(Name, Operation);

        for (var i = 0; i < SpanCount; i++)
        {
            var span = transaction.StartChild(Operation);
            span.Finish();
        }

        transaction.Finish();
    }
}
