#nullable enable

using System.Timers;

namespace Sentry.Tests.Internals;

public class BatchProcessorTests
{
    private readonly IHub _hub;
    private readonly FakeBatchProcessorTimer _timer;
    private readonly List<Envelope> _envelopes;

    public BatchProcessorTests()
    {
        _hub = Substitute.For<IHub>();
        _timer = new FakeBatchProcessorTimer();

        _envelopes = [];
        _hub.CaptureEnvelope(Arg.Do<Envelope>(arg => _envelopes.Add(arg)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Ctor_CountOutOfRange_Throws(int count)
    {
        var ctor = () => new BatchProcessor(_hub, count, TimeSpan.FromMilliseconds(10));

        Assert.Throws<ArgumentOutOfRangeException>(ctor);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(int.MaxValue + 1.0)]
    public void Ctor_IntervalOutOfRange_Throws(double interval)
    {
        var ctor = () => new BatchProcessor(_hub, 1, TimeSpan.FromMilliseconds(interval));

        Assert.Throws<ArgumentException>(ctor);
    }

    [Fact]
    public void Enqueue_NeitherSizeNorTimeoutReached_DoesNotCaptureEnvelope()
    {
        using var processor = new BatchProcessor(_hub, 2, _timer);

        processor.Enqueue(CreateLog("one"));

        AssertCaptureEnvelope(0);
        AssertEnvelope();
    }

    [Fact]
    public void Enqueue_SizeReached_CaptureEnvelope()
    {
        using var processor = new BatchProcessor(_hub, 2, _timer);

        processor.Enqueue(CreateLog("one"));
        processor.Enqueue(CreateLog("two"));

        AssertCaptureEnvelope(1);
        AssertEnvelope("one", "two");
    }

    [Fact]
    public void Enqueue_TimeoutReached_CaptureEnvelope()
    {
        using var processor = new BatchProcessor(_hub, 2, _timer);

        processor.Enqueue(CreateLog("one"));

        _timer.InvokeElapsed(DateTime.Now);

        AssertCaptureEnvelope(1);
        AssertEnvelope("one");
    }

    [Fact]
    public void Enqueue_BothSizeAndTimeoutReached_CaptureEnvelopeOnce()
    {
        using var processor = new BatchProcessor(_hub, 2, _timer);

        processor.Enqueue(CreateLog("one"));
        processor.Enqueue(CreateLog("two"));
        _timer.InvokeElapsed(DateTime.Now);

        AssertCaptureEnvelope(1);
        AssertEnvelope("one", "two");
    }

    [Fact]
    public void Enqueue_BothTimeoutAndSizeReached_CaptureEnvelopes()
    {
        using var processor = new BatchProcessor(_hub, 2, _timer);

        _timer.InvokeElapsed(DateTime.Now);
        processor.Enqueue(CreateLog("one"));
        _timer.InvokeElapsed(DateTime.Now);
        processor.Enqueue(CreateLog("two"));
        processor.Enqueue(CreateLog("three"));

        AssertCaptureEnvelope(2);
        AssertEnvelopes(["one"], ["two", "three"]);
    }

    private static SentryLog CreateLog(string message)
    {
        return new SentryLog(DateTimeOffset.MinValue, SentryId.Empty, SentryLogLevel.Trace, message);
    }

    private void AssertCaptureEnvelope(int requiredNumberOfCalls)
    {
        _hub.Received(requiredNumberOfCalls).CaptureEnvelope(Arg.Any<Envelope>());
    }

    private void AssertEnvelope(params string[] expected)
    {
        if (expected.Length == 0)
        {
            Assert.Empty(_envelopes);
            return;
        }

        var envelope = Assert.Single(_envelopes);
        AssertEnvelope(envelope, expected);
    }

    private void AssertEnvelopes(params string[][] expected)
    {
        if (expected.Length == 0)
        {
            Assert.Empty(_envelopes);
            return;
        }

        Assert.Equal(expected.Length, _envelopes.Count);
        for (var index = 0; index < _envelopes.Count; index++)
        {
            AssertEnvelope(_envelopes[index], expected[index]);
        }
    }

    private static void AssertEnvelope(Envelope envelope, string[] expected)
    {
        var item = Assert.Single(envelope.Items);
        var payload = Assert.IsType<JsonSerializable>(item.Payload);
        var log = payload.Source as StructuredLog;
        Assert.NotNull(log);
        Assert.Equal(expected, log.Items.ToArray().Select(static item => item.Message));
    }
}

internal sealed class FakeBatchProcessorTimer : BatchProcessorTimer
{
    public override bool Enabled { get; set; }

    public override event EventHandler<ElapsedEventArgs> Elapsed = null!;

    internal void InvokeElapsed(DateTime signalTime)
    {
#if NET9_0_OR_GREATER
        var e = new ElapsedEventArgs(signalTime);
#else
        var type = typeof(ElapsedEventArgs);
        var ctor = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance;
        var instance = Activator.CreateInstance(type, ctor, null, [signalTime], null);
        var e = (ElapsedEventArgs)instance!;
#endif
        Elapsed.Invoke(this, e);
    }
}
