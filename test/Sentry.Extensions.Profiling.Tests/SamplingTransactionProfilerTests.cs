
namespace Sentry.Extensions.Profiling.Tests;

public class SamplingTransactionProfilerTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SamplingTransactionProfilerTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    private void ValidateProfile(SampleProfile profile, ulong maxTimestampNs)
    {
        profile.Samples.Should().NotBeEmpty();
        profile.Frames.Should().NotBeEmpty();
        profile.Stacks.Should().NotBeEmpty();

        var threadIds = profile.Threads.Keys();

        foreach (var sample in profile.Samples)
        {
            sample.Timestamp.Should().BeInRange(0, maxTimestampNs);
            sample.StackId.Should().BeInRange(0, profile.Stacks.Count);
            sample.ThreadId.Should().BeOneOf(threadIds);
        }

        profile.Threads.Foreach((i, thread) =>
        {
            thread.Name.Should().NotBeNullOrEmpty();
        });

        // We can't check that all Frame names are filled because there may be native frames which we currently don't filter out.
        // Let's just check there are some frames with names...
        profile.Frames.Where((frame) => frame.Function is not null).Should().NotBeEmpty();
    }

    [Fact]
    public async void Profiler_StartedNormally_Works()
    {
        var hub = Substitute.For<IHub>();
        var transactionTracer = new TransactionTracer(hub, "test", "");

        var factory = new SamplingTransactionProfilerFactory();
        var clock = SentryStopwatch.StartNew();
        var sut = factory.OnTransactionStart(transactionTracer, clock.CurrentDateTimeOffset, CancellationToken.None);
        transactionTracer.TransactionProfiler = sut;
        for (int i = 0; i < 10; i++)
        {
            _testOutputLogger.LogDebug("sleeping...");
            Thread.Sleep(20);
        }
        sut.OnTransactionFinish(clock.CurrentDateTimeOffset);
        var elapsedNanoseconds = (ulong)((clock.CurrentDateTimeOffset - clock.StartDateTimeOffset).TotalMilliseconds * 1_000_000);

        var transaction = new Transaction(transactionTracer);
        var profileInfo = await sut.Collect(transaction);
        Assert.NotNull(profileInfo);
        ValidateProfile(profileInfo.Profile, elapsedNanoseconds);
    }

    [Fact]
    public async void Profiler_AfterTimeout_Stops()
    {
        var hub = Substitute.For<IHub>();

        var clock = SentryStopwatch.StartNew();
        var limitMs = 50;
        var sut = new SamplingTransactionProfiler(clock.CurrentDateTimeOffset, CancellationToken.None, limitMs);
        for (int i = 0; i < 10; i++)
        {
            _testOutputLogger.LogDebug("sleeping...");
            Thread.Sleep(20);
        }
        clock.Elapsed.TotalMilliseconds.Should().BeGreaterThan(limitMs * 4);
        sut.OnTransactionFinish(clock.CurrentDateTimeOffset);

        var profileInfo = await sut.Collect(new Transaction("foo", "bar"));

        Assert.NotNull(profileInfo);
        ValidateProfile(profileInfo.Profile, (ulong)(limitMs * 1_000_000));
    }
}
