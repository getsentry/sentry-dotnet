using BenchmarkDotNet.Attributes;
using NSubstitute;
using Sentry.Internal;
using Sentry.Profiling;

namespace Sentry.Benchmarks;

public class ProfilingBenchmarks
{
    [Params(25, 100, 1000, 10000)]
    public int TxRuntimeMs;

    private IHub _hub = Substitute.For<IHub>();
    private ITransactionProfilerFactory _factory = new SamplingTransactionProfilerFactory(Path.GetTempPath(), new());

    [Benchmark]
    public void WithoutProfiling()
    {
        var tt = new TransactionTracer(_hub, "test", "");
        RunForMs(TxRuntimeMs);
        var transaction = new Transaction(tt);
    }

    [Benchmark]
    public void WithProfiling()
    {
        var tt = new TransactionTracer(_hub, "test", "");
        var sut = _factory.Start(tt, CancellationToken.None);
        tt.TransactionProfiler = sut;
        RunForMs(TxRuntimeMs);
        sut.Finish();
        var transaction = new Transaction(tt);
        var collectTask = sut.CollectAsync(transaction);
        collectTask.Wait();
    }

    private void RunForMs(int milliseconds)
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            // Rather arbitrary numnbers here, just to keep get the profiler to capture something.
            FindPrimeNumber(milliseconds);
            Thread.Sleep(milliseconds / 10);
        }
    }

    private static long FindPrimeNumber(int n)
    {
        int count = 0;
        long a = 2;
        while (count < n)
        {
            long b = 2;
            int prime = 1;// to check if found a prime
            while (b * b <= a)
            {
                if (a % b == 0)
                {
                    prime = 0;
                    break;
                }
                b++;
            }
            if (prime > 0)
            {
                count++;
            }
            a++;
        }
        return (--a);
    }
}
