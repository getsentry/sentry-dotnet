using Sentry.Http;
using Sentry.Internal;
using Sentry.Protocol.Envelopes;
using Sentry.Testing;

namespace Sentry.Tests;

public partial class SentryClientTests
{
    [Fact]
    public void CaptureEvent_WithStreamAttachment_SpotlightEnabled_MainEnvelopeRetainsAttachment()
    {
        // Arrange — a stream-backed attachment shares a single-use stream between the Spotlight
        // envelope and the main pipeline envelope. Spotlight must not consume/dispose it.
        var spotlightTransport = Substitute.For<ISpotlightTransport>();
        _fixture.SentryOptions.SpotlightTransport = spotlightTransport;

        const string attachmentContent = "SPOTLIGHT-ATTACHMENT-CONTENT";
        var scope = new Scope(_fixture.SentryOptions);
        scope.AddAttachment(new SentryAttachment(
            AttachmentType.Default,
            new StreamAttachmentContent(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(attachmentContent))),
            "attachment.txt",
            null));

        var sut = _fixture.GetSut();
        Envelope envelope = null;
        sut.Worker.EnqueueEnvelope(Arg.Do<Envelope>(e => envelope = e));

        // Act
        sut.CaptureEvent(new SentryEvent(), scope);

        // Assert — the envelope the main pipeline sends still contains the attachment bytes intact
        Assert.NotNull(envelope);
        using var ms = new MemoryStream();
        envelope.Serialize(ms, null);
        var serialized = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        Assert.Contains(attachmentContent, serialized);
    }

    [Fact]
    public void CaptureEvent_DroppedByBeforeSend_StillSentToSpotlight()
    {
        // Arrange
        var spotlightTransport = Substitute.For<ISpotlightTransport>();
        _fixture.SentryOptions.SpotlightTransport = spotlightTransport;
        _fixture.SentryOptions.SetBeforeSend((_, _) => null); // drop all events

        var sut = _fixture.GetSut();

        // Act
        var id = sut.CaptureEvent(new SentryEvent());

        // Assert — main pipeline dropped the event
        Assert.Equal(default, id);
        _ = _fixture.BackgroundWorker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());

        // Spotlight received the serialized envelope
        spotlightTransport.Received(1).SendAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CaptureEvent_DroppedByEventProcessor_StillSentToSpotlight()
    {
        // Arrange
        var spotlightTransport = Substitute.For<ISpotlightTransport>();
        _fixture.SentryOptions.SpotlightTransport = spotlightTransport;

        var processor = Substitute.For<ISentryEventProcessor>();
        processor.Process(Arg.Any<SentryEvent>()).ReturnsNull();
        _fixture.SentryOptions.AddEventProcessor(processor);

        var sut = _fixture.GetSut();

        // Act
        var id = sut.CaptureEvent(new SentryEvent());

        // Assert — main pipeline dropped the event
        Assert.Equal(default, id);

        // Spotlight received the serialized envelope
        spotlightTransport.Received(1).SendAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CaptureEvent_DroppedBySampling_StillSentToSpotlight()
    {
        // Arrange
        var spotlightTransport = Substitute.For<ISpotlightTransport>();
        _fixture.SentryOptions.SpotlightTransport = spotlightTransport;
        _fixture.SentryOptions.SampleRate = 0.01f; // lowest valid rate
        // Always return 0.99 so the event is always sampled out (0.99 >= 0.01)
        _fixture.RandomValuesFactory = new FixedRandomValuesFactory(0.99);

        var sut = _fixture.GetSut();

        // Act
        var id = sut.CaptureEvent(new SentryEvent());

        // Assert — main pipeline dropped the event
        Assert.Equal(default, id);

        // Spotlight received the serialized envelope
        spotlightTransport.Received(1).SendAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CaptureTransaction_SampledOut_StillSentToSpotlight()
    {
        // Arrange
        var spotlightTransport = Substitute.For<ISpotlightTransport>();
        _fixture.SentryOptions.SpotlightTransport = spotlightTransport;

        var hub = Substitute.For<IHub>();
        var transaction = new TransactionTracer(hub, "test name", "test operation")
        {
            IsSampled = false
        };
        transaction.EndTimestamp = DateTimeOffset.Now;

        var sut = _fixture.GetSut();

        // Act
        sut.CaptureTransaction(new SentryTransaction(transaction));

        // Assert — main pipeline dropped the transaction
        _ = _fixture.BackgroundWorker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());

        // Spotlight received the serialized envelope
        spotlightTransport.Received(1).SendAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CaptureTransaction_DroppedByBeforeSendTransaction_StillSentToSpotlight()
    {
        // Arrange
        var spotlightTransport = Substitute.For<ISpotlightTransport>();
        _fixture.SentryOptions.SpotlightTransport = spotlightTransport;
        _fixture.SentryOptions.SetBeforeSendTransaction((_, _) => null); // drop all transactions

        var transaction = new SentryTransaction("test name", "test operation")
        {
            IsSampled = true,
            EndTimestamp = DateTimeOffset.Now
        };

        var sut = _fixture.GetSut();

        // Act
        sut.CaptureTransaction(transaction);

        // Assert — main pipeline dropped the transaction
        _ = _fixture.BackgroundWorker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());

        // Spotlight received the serialized envelope
        spotlightTransport.Received(1).SendAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CaptureEvent_NoSpotlightTransport_DoesNotThrow()
    {
        // Arrange — no SpotlightTransport set (default null)
        _fixture.SentryOptions.SpotlightTransport = null;

        var sut = _fixture.GetSut();

        // Act & Assert — should not throw
        var id = sut.CaptureEvent(new SentryEvent());

        Assert.NotEqual(default, id);
    }

    [Fact]
    public void CaptureEvent_SpotlightFailure_DoesNotAffectMainPipeline()
    {
        // Arrange
        var spotlightTransport = Substitute.For<ISpotlightTransport>();
        spotlightTransport.SendAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Spotlight is down")));
        _fixture.SentryOptions.SpotlightTransport = spotlightTransport;

        var sut = _fixture.GetSut();

        // Act — should not throw even though Spotlight fails
        var id = sut.CaptureEvent(new SentryEvent());

        // Assert — main pipeline still processed the event
        Assert.NotEqual(default, id);
        _ = _fixture.BackgroundWorker.Received(1).EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureEvent_SpotlightData_DoesNotContainBeforeSendMutation()
    {
        // Arrange — capture the bytes Spotlight receives
        byte[] capturedBytes = null;
        var spotlightTransport = Substitute.For<ISpotlightTransport>();
        spotlightTransport.SendAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => capturedBytes = ci.Arg<byte[]>());

        _fixture.SentryOptions.SpotlightTransport = spotlightTransport;
        _fixture.SentryOptions.SetBeforeSend((e, _) =>
        {
            e.SetTag("mutated", "true");
            return e;
        });

        var sut = _fixture.GetSut();

        // Act
        sut.CaptureEvent(new SentryEvent());

        // Assert — Spotlight received data that does NOT contain the BeforeSend mutation
        Assert.NotNull(capturedBytes);
        var payload = System.Text.Encoding.UTF8.GetString(capturedBytes);
        Assert.DoesNotContain("mutated", payload);
    }

    [Fact]
    public void CaptureEvent_SpotlightData_DoesNotContainEventProcessorMutation()
    {
        // Arrange — capture the bytes Spotlight receives
        byte[] capturedBytes = null;
        var spotlightTransport = Substitute.For<ISpotlightTransport>();
        spotlightTransport.SendAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => capturedBytes = ci.Arg<byte[]>());

        _fixture.SentryOptions.SpotlightTransport = spotlightTransport;

        // Processor that adds a tag
        var processor = Substitute.For<ISentryEventProcessor>();
        processor.Process(Arg.Any<SentryEvent>()).Returns(ci =>
        {
            var evt = ci.Arg<SentryEvent>();
            evt.SetTag("processor-tag", "was-here");
            return evt;
        });
        _fixture.SentryOptions.AddEventProcessor(processor);

        var sut = _fixture.GetSut();

        // Act
        sut.CaptureEvent(new SentryEvent());

        // Assert — Spotlight received data that does NOT contain the processor mutation
        Assert.NotNull(capturedBytes);
        var payload = System.Text.Encoding.UTF8.GetString(capturedBytes);
        Assert.DoesNotContain("processor-tag", payload);
    }

    [Fact]
    public void CaptureTransaction_SpotlightData_DoesNotContainProcessorMutation()
    {
        // Arrange — capture the bytes Spotlight receives
        byte[] capturedBytes = null;
        var spotlightTransport = Substitute.For<ISpotlightTransport>();
        spotlightTransport.SendAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => capturedBytes = ci.Arg<byte[]>());

        _fixture.SentryOptions.SpotlightTransport = spotlightTransport;

        // Transaction processor that adds a tag
        var processor = Substitute.For<ISentryTransactionProcessor>();
        processor.Process(Arg.Any<SentryTransaction>()).Returns(ci =>
        {
            var tx = ci.Arg<SentryTransaction>();
            tx.SetTag("tx-processor-tag", "was-here");
            return tx;
        });
        _fixture.SentryOptions.AddTransactionProcessor(processor);

        var transaction = new SentryTransaction("test name", "test operation")
        {
            IsSampled = true,
            EndTimestamp = DateTimeOffset.Now
        };

        var sut = _fixture.GetSut();

        // Act
        sut.CaptureTransaction(transaction);

        // Assert — Spotlight received data that does NOT contain the processor mutation
        Assert.NotNull(capturedBytes);
        var payload = System.Text.Encoding.UTF8.GetString(capturedBytes);
        Assert.DoesNotContain("tx-processor-tag", payload);
    }
}

file class FixedRandomValuesFactory : RandomValuesFactory
{
    private readonly double _value;

    public FixedRandomValuesFactory(double value) => _value = value;

    public override int NextInt() => (int)(_value * int.MaxValue);
    public override int NextInt(int minValue, int maxValue) => minValue;
    public override double NextDouble() => _value;
    public override void NextBytes(byte[] bytes) => Array.Fill(bytes, (byte)0);

#if !(NETSTANDARD2_0 || NET462)
    public override void NextBytes(Span<byte> bytes) => bytes.Fill(0);
#endif
}
