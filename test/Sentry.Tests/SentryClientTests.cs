using System.Net.Http;
using Sentry.Internal.Http;
using Sentry.Testing;

#pragma warning disable CS0618

namespace Sentry.Tests;

[UsesVerify]
public class SentryClientTests
{
    private class Fixture
    {
        public SentryOptions SentryOptions { get; set; } = new();
        public IBackgroundWorker BackgroundWorker { get; set; } = Substitute.For<IBackgroundWorker, IDisposable>();
        public IClientReportRecorder ClientReportRecorder { get; set; } = Substitute.For<IClientReportRecorder>();

        public Fixture()
        {
            SentryOptions.ClientReportRecorder = ClientReportRecorder;
        }

        public SentryClient GetSut() => new(SentryOptions, BackgroundWorker);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void CaptureEvent_ExceptionFiltered_EmptySentryId()
    {
        _fixture.SentryOptions.AddExceptionFilterForType<SystemException>();
        _ = _fixture.BackgroundWorker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(true);

        var sut = _fixture.GetSut();

        // Filtered out for it's the exact filtered type
        Assert.Equal(default, sut.CaptureException(new SystemException()));
        _ = _fixture.BackgroundWorker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());

        // Filtered for it's a derived type
        Assert.Equal(default, sut.CaptureException(new ArithmeticException()));
        _ = _fixture.BackgroundWorker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());

        // Not filtered since it's not in the inheritance chain
        Assert.NotEqual(default, sut.CaptureException(new()));
        _ = _fixture.BackgroundWorker.Received(1).EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureEvent_IdReturnedToString_NoDashes()
    {
        var sut = _fixture.GetSut();

        var evt = new SentryEvent(new());

        var actual = sut.CaptureEvent(evt);

        var hasDashes = actual.ToString().Contains('-');
        Assert.False(hasDashes);
    }

    [Fact]
    public void CaptureEvent_ExceptionProcessorsOnOptions_Invoked()
    {
        var exceptionProcessor = Substitute.For<ISentryEventExceptionProcessor>();
        _fixture.SentryOptions.AddExceptionProcessorProvider(() => new[] { exceptionProcessor });
        var sut = _fixture.GetSut();

        var evt = new SentryEvent(new());

        _ = sut.CaptureEvent(evt);

        exceptionProcessor.Received(1).Process(evt.Exception!, evt);
    }

    [Fact]
    public void CaptureEvent_ExceptionProcessorsOnScope_Invoked()
    {
        var exceptionProcessor = Substitute.For<ISentryEventExceptionProcessor>();
        var scope = new Scope();
        scope.AddExceptionProcessor(exceptionProcessor);

        var sut = _fixture.GetSut();

        var evt = new SentryEvent(new());

        _ = sut.CaptureEvent(evt, scope);

        exceptionProcessor.Received(1).Process(evt.Exception!, evt);
    }

    [Fact]
    public void CaptureEvent_NullEventWithScope_EmptyGuid()
    {
        var sut = _fixture.GetSut();
        Assert.Equal(default, sut.CaptureEvent(null, new(_fixture.SentryOptions)));
    }

    [Fact]
    public void CaptureEvent_NullEvent_EmptyGuid()
    {
        var sut = _fixture.GetSut();
        Assert.Equal(default, sut.CaptureEvent(null));
    }

    [Fact]
    public void CaptureEvent_NullScope_QueuesEvent()
    {
        var expectedId = Guid.NewGuid();
        var expectedEvent = new SentryEvent(eventId: expectedId);
        _ = _fixture.BackgroundWorker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(true);

        var sut = _fixture.GetSut();

        var actualId = sut.CaptureEvent(expectedEvent);
        Assert.Equal(expectedId, (Guid)actualId);
    }

    [Fact]
    public void CaptureEvent_EventAndScope_QueuesEvent()
    {
        var expectedId = Guid.NewGuid();
        var expectedEvent = new SentryEvent(eventId: expectedId);
        _ = _fixture.BackgroundWorker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(true);

        var sut = _fixture.GetSut();

        var actualId = sut.CaptureEvent(expectedEvent, new(_fixture.SentryOptions));
        Assert.Equal(expectedId, (Guid)actualId);
    }

    [Fact]
    public void CaptureEvent_EventAndScope_EvaluatesScope()
    {
        var scope = new Scope(_fixture.SentryOptions);
        var sut = _fixture.GetSut();

        var evaluated = false;
        object actualSender = null;
        scope.OnEvaluating += (sender, _) =>
        {
            actualSender = sender;
            evaluated = true;
        };

        _ = sut.CaptureEvent(new(), scope);

        Assert.True(evaluated);
        Assert.Same(scope, actualSender);
    }

    [Fact]
    public void CaptureEvent_EventAndScope_CopyScopeIntoEvent()
    {
        const string expectedBreadcrumb = "test";
        var scope = new Scope(_fixture.SentryOptions);
        scope.AddBreadcrumb(expectedBreadcrumb);
        var @event = new SentryEvent();

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(@event, scope);

        Assert.Equal(scope.Breadcrumbs, @event.Breadcrumbs);
    }

    [Fact]
    public void CaptureEvent_BeforeEvent_RejectEvent()
    {
        _fixture.SentryOptions.BeforeSend = _ => null;
        var expectedEvent = new SentryEvent();

        var sut = _fixture.GetSut();
        var actualId = sut.CaptureEvent(expectedEvent, new(_fixture.SentryOptions));

        Assert.Equal(default, actualId);
        _ = _fixture.BackgroundWorker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureEvent_BeforeEvent_RejectEvent_RecordsDiscard()
    {
        _fixture.SentryOptions.BeforeSend = _ => null;

        var transport = Substitute.For<ITransport>();
        _fixture.SentryOptions.Transport = transport;

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(new());

        _fixture.ClientReportRecorder.Received(1)
            .RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Error);
    }

    [Fact]
    public void CaptureEvent_EventProcessor_RejectEvent_RecordsDiscard()
    {
        var processor = Substitute.For<ISentryEventProcessor>();
        processor.Process(Arg.Any<SentryEvent>()).ReturnsNull();

        _fixture.SentryOptions.AddEventProcessor(processor);

        var transport = Substitute.For<ITransport>();
        _fixture.SentryOptions.Transport = transport;

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(new());

        _fixture.ClientReportRecorder.Received(1)
            .RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
    }

    [Fact]
    public void CaptureEvent_ExceptionFilter_RecordsDiscard()
    {
        var filter = Substitute.For<IExceptionFilter>();
        filter.Filter(Arg.Any<Exception>()).Returns(true);

        _fixture.SentryOptions.AddExceptionFilter(filter);

        var transport = Substitute.For<ITransport>();
        _fixture.SentryOptions.Transport = transport;

        var sut = _fixture.GetSut();
        _ = sut.CaptureException(new());

        _fixture.ClientReportRecorder.Received(1)
            .RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
    }

    [Fact]
    public void CaptureEvent_BeforeEvent_ModifyEvent()
    {
        SentryEvent received = null;
        _fixture.SentryOptions.BeforeSend = e => received = e;

        var @event = new SentryEvent();

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(@event);

        Assert.Same(@event, received);
    }

    [Fact]
    public void CaptureEvent_LevelOnScope_OverridesLevelOnEvent()
    {
        const SentryLevel expected = SentryLevel.Fatal;
        var @event = new SentryEvent
        {
            Level = SentryLevel.Fatal
        };
        var scope = new Scope
        {
            Level = expected
        };

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(@event, scope);

        Assert.Equal(expected, @event.Level);
    }

    [Fact]
    public void CaptureEvent_SamplingLowest_DropsEvent()
    {
        // Smallest value allowed. Should always drop
        _fixture.SentryOptions.SampleRate = float.Epsilon;
        var @event = new SentryEvent();

        var sut = _fixture.GetSut();

        Assert.Equal(default, sut.CaptureEvent(@event));
    }

    [Fact]
    public void CaptureEvent_SampleDrop_RecordsDiscard()
    {
        _fixture.SentryOptions.SampleRate = float.Epsilon;

        var transport = Substitute.For<ITransport>();
        _fixture.SentryOptions.Transport = transport;

        var @event = new SentryEvent();

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(@event);

        _fixture.ClientReportRecorder.Received(1)
            .RecordDiscardedEvent(DiscardReason.SampleRate, DataCategory.Error);
    }

    [Fact]
    public void CaptureEvent_SamplingHighest_SendsEvent()
    {
        // Largest value allowed. Should always send
        _fixture.SentryOptions.SampleRate = 1;
        SentryEvent received = null;
        _fixture.SentryOptions.BeforeSend = e => received = e;

        var @event = new SentryEvent();

        var sut = _fixture.GetSut();

        _ = sut.CaptureEvent(@event);

        Assert.Same(@event, received);
    }

    [Fact]
    public void CaptureEvent_SamplingNull_DropsEvent()
    {
        _fixture.SentryOptions.SampleRate = null;
        SentryEvent received = null;
        _fixture.SentryOptions.BeforeSend = e => received = e;

        var @event = new SentryEvent();

        var sut = _fixture.GetSut();

        _ = sut.CaptureEvent(@event);

        Assert.Same(@event, received);
    }

    [Fact]
    public void CaptureEvent_Sampling50Percent_EqualDistribution()
    {
        // 15% deviation is ok
        const double allowedRelativeDeviation = 0.15;

        // Arrange
        var client = new SentryClient(new()
        {
            Dsn = ValidDsn,
            SampleRate = 0.5f,
            MaxQueueItems = int.MaxValue
        });

        // Act
        var eventIds = Enumerable
            .Range(0, 1_000)
            .Select(i => client.CaptureEvent(new()
                { Message = $"Test[{i}]" }))
            .ToArray();

        var sampledInEventsCount = eventIds.Count(e => e != SentryId.Empty);
        var sampledOutEventsCount = eventIds.Count(e => e == SentryId.Empty);

        // Assert
        sampledInEventsCount.Should().BeCloseTo(
            (int)(0.5 * eventIds.Length),
            (uint)(allowedRelativeDeviation * eventIds.Length));

        sampledOutEventsCount.Should().BeCloseTo(
            (int)(0.5 * eventIds.Length),
            (uint)(allowedRelativeDeviation * eventIds.Length));
    }

    [Fact]
    public void CaptureEvent_Sampling25Percent_AppropriateDistribution()
    {
        // 15% deviation is ok
        const double allowedRelativeDeviation = 0.15;

        // Arrange
        var client = new SentryClient(new()
        {
            Dsn = ValidDsn,
            SampleRate = 0.25f,
            MaxQueueItems = int.MaxValue
        });

        // Act
        var eventIds = Enumerable
            .Range(0, 1_000)
            .Select(i => client.CaptureEvent(new()
                { Message = $"Test[{i}]" }))
            .ToArray();

        var sampledInEventsCount = eventIds.Count(e => e != SentryId.Empty);
        var sampledOutEventsCount = eventIds.Count(e => e == SentryId.Empty);

        // Assert
        sampledInEventsCount.Should().BeCloseTo(
            (int)(0.25 * eventIds.Length),
            (uint)(allowedRelativeDeviation * eventIds.Length));

        sampledOutEventsCount.Should().BeCloseTo(
            (int)(0.75 * eventIds.Length),
            (uint)(allowedRelativeDeviation * eventIds.Length));
    }

    [Fact]
    public void CaptureEvent_Sampling75Percent_AppropriateDistribution()
    {
        // 15% deviation is ok
        const double allowedRelativeDeviation = 0.15;

        // Arrange
        var client = new SentryClient(new()
        {
            Dsn = ValidDsn,
            SampleRate = 0.75f,
            MaxQueueItems = int.MaxValue
        });

        // Act
        var eventIds = Enumerable
            .Range(0, 1_000)
            .Select(i => client.CaptureEvent(new()
                { Message = $"Test[{i}]" }))
            .ToArray();

        var sampledInEventsCount = eventIds.Count(e => e != SentryId.Empty);
        var sampledOutEventsCount = eventIds.Count(e => e == SentryId.Empty);

        // Assert
        sampledInEventsCount.Should().BeCloseTo(
            (int)(0.75 * eventIds.Length),
            (uint)(allowedRelativeDeviation * eventIds.Length));

        sampledOutEventsCount.Should().BeCloseTo(
            (int)(0.25 * eventIds.Length),
            (uint)(allowedRelativeDeviation * eventIds.Length));
    }

    [Fact]
    public Task CaptureEvent_BeforeEventThrows_ErrorToEventBreadcrumb()
    {
        var error = new Exception("Exception message!");
        _fixture.SentryOptions.BeforeSend = _ => throw error;

        var @event = new SentryEvent();

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(@event);

        return Verifier.Verify(@event.Breadcrumbs);
    }

    [Fact]
    public void CaptureEvent_Release_SetFromOptions()
    {
        const string expectedRelease = "release number";
        _fixture.SentryOptions.Release = expectedRelease;

        var @event = new SentryEvent();

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(@event);

        Assert.Equal(expectedRelease, @event.Release);
    }

    [Fact]
    public void CaptureEvent_Distribution_SetFromOptions()
    {
        const string expectedDistribution = "some distribution";
        _fixture.SentryOptions.Distribution = expectedDistribution;

        var @event = new SentryEvent();

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(@event);

        Assert.Equal(expectedDistribution, @event.Distribution);
    }

    [Fact]
    public void CaptureEvent_DisposedClient_DoesNotThrow()
    {
        var sut = _fixture.GetSut();
        sut.Dispose();
        var @event = new SentryEvent();
        sut.CaptureEvent(@event);
    }

    [Fact]
    public void CaptureUserFeedback_EventIdEmpty_IgnoreUserFeedback()
    {
        //Arrange
        var sut = _fixture.GetSut();

        //Act
        sut.CaptureUserFeedback(
            new(SentryId.Empty, "name", "email", "comment"));

        //Assert
        _ = sut.Worker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureUserFeedback_ValidUserFeedback_FeedbackRegistered()
    {
        //Arrange
        var sut = _fixture.GetSut();

        //Act
        sut.CaptureUserFeedback(
            new(SentryId.Parse("4eb98e5f861a41019f270a7a27e84f02"), "name", "email", "comment"));

        //Assert
        _ = sut.Worker.Received(1).EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureUserFeedback_EventIdEmpty_FeedbackIgnored()
    {
        //Arrange
        var sut = _fixture.GetSut();

        //Act
        sut.CaptureUserFeedback(new(SentryId.Empty, "name", "email", "comment"));

        //Assert
        _ = sut.Worker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
    }
    [Fact]
    public void Dispose_should_only_flush()
    {
        // Arrange
        var client = new SentryClient(new()
        {
            Dsn = ValidDsn,
        });

        // Act
        client.Dispose();

        //Assert is still usable
        client.CaptureEvent(new()
            { Message = "Test" });
    }

    [Fact]
    public void CaptureUserFeedback_DisposedClient_DoesNotThrow()
    {
        var sut = _fixture.GetSut();
        sut.Dispose();
        sut.CaptureUserFeedback(new(SentryId.Empty, "name", "email", "comment"));
    }

    [Fact]
    public void CaptureTransaction_SampledOut_Dropped()
    {
        // Arrange
        var client = _fixture.GetSut();

        // Act
        client.CaptureTransaction(new(
            "test name",
            "test operation"
        )
        {
            IsSampled = false,
            EndTimestamp = DateTimeOffset.Now // finished
        });

        // Assert
        _ = client.Worker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureTransaction_ValidTransaction_Sent()
    {
        // Arrange
        var client = _fixture.GetSut();

        // Act
        client.CaptureTransaction(
            new(
                "test name",
                "test operation"
            )
            {
                IsSampled = true,
                EndTimestamp = DateTimeOffset.Now // finished
            });

        // Assert
        _ = client.Worker.Received(1).EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureTransaction_NoSpanId_Ignored()
    {
        // Arrange
        var client = _fixture.GetSut();

        var transaction = new Transaction(
            "test name",
            "test operation"
        )
        {
            IsSampled = true,
            EndTimestamp = DateTimeOffset.Now // finished
        };

        transaction.Contexts.Trace.SpanId = SpanId.Empty;

        // Act
        client.CaptureTransaction(transaction);

        // Assert
        _ = client.Worker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureTransaction_NoName_Ignored()
    {
        // Arrange
        var client = _fixture.GetSut();

        // Act
        client.CaptureTransaction(
            new(
                null!,
                "test operation"
            )
            {
                IsSampled = true,
                EndTimestamp = DateTimeOffset.Now // finished
            });

        // Assert
        _ = client.Worker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureTransaction_NoOperation_Ignored()
    {
        // Arrange
        var client = _fixture.GetSut();

        // Act
        client.CaptureTransaction(
            new(
                "test name",
                null!
            )
            {
                IsSampled = true,
                EndTimestamp = DateTimeOffset.Now // finished
            });

        // Assert
        _ = client.Worker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureTransaction_NotFinished_Sent()
    {
        // Arrange
        var client = _fixture.GetSut();

        // Act
        client.CaptureTransaction(
            new(
                "test name",
                "test operation"
            )
            {
                IsSampled = true,
                EndTimestamp = null // not finished
            });

        // Assert
        _ = client.Worker.Received(1).EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureTransaction_DisposedClient_DoesNotThrow()
    {
        var sut = _fixture.GetSut();
        sut.Dispose();
        sut.CaptureTransaction(
            new(
                "test name",
                "test operation")
            {
                IsSampled = true,
                EndTimestamp = null // not finished
            });
    }

    [Fact]
    public void Dispose_Worker_FlushCalled()
    {
        var client = _fixture.GetSut();
        client.Dispose();
        _fixture.BackgroundWorker?.Received(1).FlushAsync(_fixture.SentryOptions.ShutdownTimeout);
    }

    [Fact]
    public void Dispose_MultipleCalls_WorkerFlushedTwice()
    {
        var sut = _fixture.GetSut();
        sut.Dispose();
        sut.Dispose();
        _fixture.BackgroundWorker?.Received(2).FlushAsync(_fixture.SentryOptions.ShutdownTimeout);
    }

    [Fact]
    public void IsEnabled_AlwaysTrue()
    {
        var sut = _fixture.GetSut();
        Assert.True(sut.IsEnabled);
    }

    [Fact]
    public void Ctor_NullSentryOptions_ThrowsArgumentNullException()
    {
        _fixture.SentryOptions = null;
        var ex = Assert.Throws<ArgumentNullException>(() => _fixture.GetSut());
        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Ctor_HttpOptionsCallback_InvokedConfigureClient()
    {
        var invoked = false;
        _fixture.BackgroundWorker = null;
        _fixture.SentryOptions.Dsn = ValidDsn;
        _fixture.SentryOptions.ConfigureClient = _ => invoked = true;

        using (_fixture.GetSut())
        {
            Assert.True(invoked);
        }
    }

    [Fact]
    public void Ctor_CreateHttpClientHandler_InvokedConfigureHandler()
    {
        var invoked = false;
        _fixture.BackgroundWorker = null;
        _fixture.SentryOptions.Dsn = ValidDsn;
        _fixture.SentryOptions.CreateHttpClientHandler = () =>
        {
            invoked = true;
            return Substitute.For<HttpClientHandler>();
        };

        using (_fixture.GetSut())
        {
            Assert.True(invoked);
        }
    }

    [Fact]
    public void Ctor_NullBackgroundWorker_ConcreteBackgroundWorker()
    {
        _fixture.SentryOptions.Dsn = ValidDsn;

        using var sut = new SentryClient(_fixture.SentryOptions);
        _ = Assert.IsType<BackgroundWorker>(sut.Worker);
    }

    [Fact]
    public void Ctor_SetsTransportOnOptions()
    {
        _fixture.SentryOptions.Dsn = ValidDsn;

        using var sut = new SentryClient(_fixture.SentryOptions);

        _ = Assert.IsType<HttpTransport>(_fixture.SentryOptions.Transport);
    }

    [Fact]
    public void Ctor_KeepsCustomTransportOnOptions()
    {
        _fixture.SentryOptions.Dsn = ValidDsn;
        _fixture.SentryOptions.Transport = new FakeTransport();

        using var sut = new SentryClient(_fixture.SentryOptions);

        _ = Assert.IsType<FakeTransport>(_fixture.SentryOptions.Transport);
    }

    [Fact]
    public void Ctor_WrapsCustomTransportWhenCachePathOnOptions()
    {
        var fileSystem = new FakeFileSystem();
        using var cacheDirectory = new TempDirectory(fileSystem);
        _fixture.SentryOptions.CacheDirectoryPath = cacheDirectory.Path;
        _fixture.SentryOptions.FileSystem = fileSystem;
        _fixture.SentryOptions.Dsn = ValidDsn;
        _fixture.SentryOptions.Transport = new FakeTransport();

        using var sut = new SentryClient(_fixture.SentryOptions);

        var cachingTransport = Assert.IsType<CachingTransport>(_fixture.SentryOptions.Transport);
        _ = Assert.IsType<FakeTransport>(cachingTransport.InnerTransport);
    }

    [Fact]
    public async Task SentryClient_WithCachingTransport_RecordsDiscardedEvents()
    {
        var fileSystem = new FakeFileSystem();
        using var cacheDirectory = new TempDirectory(fileSystem);
        _fixture.SentryOptions.CacheDirectoryPath = cacheDirectory.Path;
        _fixture.SentryOptions.FileSystem = fileSystem;
        _fixture.SentryOptions.Dsn = ValidDsn;

        var innerTransport = Substitute.For<ITransport>();
        var cachingTransport = CachingTransport.Create(innerTransport, _fixture.SentryOptions);
        _fixture.SentryOptions.Transport = cachingTransport;
        await cachingTransport.StopWorkerAsync();

        // This will drop the event and record a discard
        _fixture.SentryOptions.BeforeSend = _ => null;

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(new());
        await cachingTransport.FlushAsync();

        _fixture.ClientReportRecorder.Received(1)
            .RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Error);
    }
}
