using System.Diagnostics.Tracing;
using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.NETCore.Client;
using NSubstitute;
using Sentry.Internal;
using Sentry.Profiling;

namespace Sentry.Benchmarks;

extern alias BenchmarkDotNetTransientTraceEvent;

public class ProfilingBenchmarks
{
    private IHub _hub = Substitute.For<IHub>();
    private SamplingTransactionProfilerFactory _factory;
    private ITransactionProfiler _profiler;

    [GlobalSetup(Targets = new string[] { nameof(Transaction), nameof(DoHardWorkWhileProfiling) })]
    public void StartProfiler()
    {
        _factory = SamplingTransactionProfilerFactory.Create(new());
    }

    [GlobalCleanup(Targets = new string[] { nameof(Transaction), nameof(DoHardWorkWhileProfiling) })]
    public void StopProfiler()
    {
        _profiler?.Finish();
        (_profiler as SamplingTransactionProfiler)?.CollectAsync(new Transaction("", "")).Wait();
        _profiler = null;
        _factory.Dispose();
        _factory = null;
    }

    #region full transaction profiling
    public IEnumerable<object[]> TransactionBenchmarkArguments()
    {
        foreach (var runtimeMs in new[] { 25, 100, 1000, 10000 })
        {
            foreach (var collect in new[] { true, false })
            {
                yield return new object[] { runtimeMs, collect };
            }
        }
    }

    // Run a profiled transaction. Profiler starts and stops for each transaction separately.
    [Benchmark]
    [ArgumentsSource(nameof(TransactionBenchmarkArguments))]
    public long Transaction(int runtimeMs, bool collect)
    {
        var tt = new TransactionTracer(_hub, "test", "");
        tt.TransactionProfiler = _factory.Start(tt, CancellationToken.None);
        var result = RunForMs(runtimeMs);
        tt.TransactionProfiler.Finish();
        var transaction = new Transaction(tt);
        if (collect)
        {
            var collectTask = (tt.TransactionProfiler as SamplingTransactionProfiler).CollectAsync(transaction);
            collectTask.Wait();
        }
        return result;
    }
    #endregion

    #region utilities

    private long RunForMs(int milliseconds)
    {
        var clock = Stopwatch.StartNew();
        long result = 0;
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            // Rather arbitrary numnbers here, just to get the profiler to capture something.
            result += FindPrimeNumber(milliseconds);
            Thread.Sleep(milliseconds / 10);
        }
        return result;
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
    #endregion

    #region Profiling session, DiagnosticsClient, etc.
    // Disabled because it skews the result table because it's in nanoseconds so everything else is printed as ns.
    // [Benchmark]
    public DiagnosticsClient DiagnosticsClientNew()
    {
        return new DiagnosticsClient(Process.GetCurrentProcess().Id);
    }

    [Benchmark]
    public void DiagnosticsSessionStartStop()
    {
        var session = DiagnosticsClientNew().StartEventPipeSession(SampleProfilerSession.Providers, true, SampleProfilerSession.CircularBufferMB);
        session.EventStream.Dispose();
        session.Dispose();
    }

    public IEnumerable<object[]> SessionArguments()
    {
        foreach (var rundown in new[] { true, false })
        {
            // Note (ID): different buffer size doesn't make any difference in performance.
            foreach (var provider in new[] { "runtime", "sample", "tpl", "all" })
            {
                yield return new object[] { rundown, provider };
            }
        }
    }

    // Explore how different providers impact session startup time (manifests when EventStream.CopyToAsync() is added).
    [Benchmark]
    [ArgumentsSource(nameof(SessionArguments))]
    public void DiagnosticsSessionStartCopyStop(bool rundown, string provider)
    {
        EventPipeProvider[] providers = provider switch
        {
            "runtime" => new[] { new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)BenchmarkDotNetTransientTraceEvent::Microsoft.Diagnostics.Tracing.Parsers.ClrTraceEventParser.Keywords.Default) },
            "sample" => new[] { new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational) },
            "tpl" => new[] { new EventPipeProvider("System.Threading.Tasks.TplEventSource", EventLevel.Informational, (long)BenchmarkDotNetTransientTraceEvent::Microsoft.Diagnostics.Tracing.Parsers.TplEtwProviderTraceEventParser.Keywords.Default) },
            "all" => SampleProfilerSession.Providers,
            _ => throw new InvalidEnumArgumentException(nameof(provider))
        };
        var session = DiagnosticsClientNew().StartEventPipeSession(providers, rundown, SampleProfilerSession.CircularBufferMB);
        var stream = new MemoryStream();
        var copyTask = session.EventStream.CopyToAsync(stream);
        session.Stop();
        copyTask.Wait();
        session.Dispose();
    }

    [Benchmark]
    public void SampleProfilerSessionStartStop()
    {
        using var session = SampleProfilerSession.StartNew();
        session.Stop();
    }
    #endregion

    #region Measure overhead of having a profiler enabled while doing work.
    public int[] OverheadRunArguments { get; } = new[] { 10_000, 100_000 };

    [BenchmarkCategory("overhead"), Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(OverheadRunArguments))]
    public long DoHardWork(int n)
    {
        return FindPrimeNumber(n);
    }

    [BenchmarkCategory("overhead"), Benchmark]
    [ArgumentsSource(nameof(OverheadRunArguments))]
    public long DoHardWorkWhileProfiling(int n)
    {
        _profiler ??= _factory.Start(new TransactionTracer(_hub, "", ""), CancellationToken.None);
        return FindPrimeNumber(n);
    }
    #endregion
}
