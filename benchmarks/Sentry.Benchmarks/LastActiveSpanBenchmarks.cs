using BenchmarkDotNet.Attributes;

namespace Sentry.Benchmarks;

public class LastActiveSpanBenchmarks
{
    private const string Operation = "Operation";
    private const string Name = "Name";

    [Params(1, 10, 100)]
    public int SpanCount;

    private IDisposable _sdk;

    [GlobalSetup(Target = nameof(CreateScopedSpans))]
    public void EnabledSdk() => _sdk = SentrySdk.Init(Constants.ValidDsn);

    [GlobalCleanup(Target = nameof(CreateScopedSpans))]
    public void DisableDsk() => _sdk.Dispose();

    [Benchmark(Description = "Create spans for scope access")]
    public void CreateScopedSpans()
    {
        var transaction = SentrySdk.StartTransaction(Name, Operation);
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        for (var i = 0; i < SpanCount; i++)
        {
            // Simulates a scenario where TransactionTracer.GetLastActiveSpan will be called frequently
            // See: https://github.com/getsentry/sentry-dotnet/blob/c2a31b4ead03da388c2db7fe07f290354aa51b9d/src/Sentry/Scope.cs#L567C1-L567C68
            CallOneFunction(i);
        }

        transaction.Finish();
    }

    private void CallOneFunction(int i)
    {
        var span = SentrySdk.GetSpan()!.StartChild($"One Function {i}");
        ThatCallsAnother(i);
        span.Finish();
    }

    private void ThatCallsAnother(int i)
    {
        var span = SentrySdk.GetSpan()!.StartChild($"Another Function {i}");
        AndAnother($"Alternate Description {i}");
        span.Finish();
    }

    private void AndAnother(string description)
    {
        SentrySdk.ConfigureScope(scope => scope.Span!.Description = description);
    }
}
