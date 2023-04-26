using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.NETCore.Client;
using NSubstitute;
using Sentry.Internal;
using Sentry.Profiling;

namespace Sentry.Benchmarks;

public class ProfilingBenchmarks
{
    private IHub _hub = Substitute.For<IHub>();
    private ITransactionProfilerFactory _factory = new SamplingTransactionProfilerFactory(Path.GetTempPath(), new());

    public IEnumerable<object[]> Arguments()
    {
        foreach (var runtimeMs in new[] { 25, 100, 1000, 10000 })
        {
            yield return new object[] { runtimeMs, false, false, false, 0 };

            foreach (var processing in new[] { true, false })
            {
                foreach (var rundown in new[] { true, false })
                {
                    // Note (ID): different buffer size doesn't make any difference in performance.
                    foreach (var bufferMB in new[] { /* 32,*/ 256 })
                    {
                        yield return new object[] { runtimeMs, true, processing, rundown, bufferMB };
                    }
                }
            }
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Arguments))]
    public void Transaction(int runtimeMs, bool profiling, bool processing, bool rundown, int bufferMB)
    {
        var tt = new TransactionTracer(_hub, "test", "");
        SampleProfilerSession.RequestRundown = rundown;
        SampleProfilerSession.CircularBufferMB = bufferMB;
        if (profiling)
        {
            tt.TransactionProfiler = _factory.Start(tt, CancellationToken.None);
        }
        RunForMs(runtimeMs);
        tt.TransactionProfiler?.Finish();
        var transaction = new Transaction(tt);
        if (processing)
        {
            var collectTask = tt.TransactionProfiler.CollectAsync(transaction);
            collectTask.Wait();
        }
    }

    private void RunForMs(int milliseconds)
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            // Rather arbitrary numnbers here, just to get the profiler to capture something.
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

    [Benchmark]
    public void DiagnosticsClientNew()
    {
        var client = new DiagnosticsClient(Process.GetCurrentProcess().Id);
    }

    [Benchmark]
    public void SessionStartStop()
    {
        var client = new DiagnosticsClient(Process.GetCurrentProcess().Id);
        var session = client.StartEventPipeSession(
            SampleProfilerSession.Providers,
            SampleProfilerSession.RequestRundown,
            SampleProfilerSession.CircularBufferMB
        );
        session.EventStream.Dispose();
        session.Dispose();
    }
}
