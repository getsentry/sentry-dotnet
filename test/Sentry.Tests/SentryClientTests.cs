using Sentry.Internal.Http;
using BackgroundWorker = Sentry.Internal.BackgroundWorker;

namespace Sentry.Tests;

public partial class SentryClientTests
{
    private class Fixture
    {
        public SentryOptions SentryOptions { get; set; } = new()
        {
            Dsn = ValidDsn,
            AttachStacktrace = false,
            AutoSessionTracking = false
        };

        public IBackgroundWorker BackgroundWorker { get; set; } = Substitute.For<IBackgroundWorker, IDisposable>();
        public IClientReportRecorder ClientReportRecorder { get; } = Substitute.For<IClientReportRecorder>();
        public ISessionManager SessionManager { get; set; } = Substitute.For<ISessionManager>();

        public Fixture()
        {
            SentryOptions.ClientReportRecorder = ClientReportRecorder;
            BackgroundWorker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(true);
        }

        public SentryClient GetSut()
        {
            var randomValuesFactory = new IsolatedRandomValuesFactory();
            return new SentryClient(SentryOptions, BackgroundWorker, randomValuesFactory, SessionManager);
        }
    }

    private readonly Fixture _fixture = new();
    private readonly ITestOutputHelper _output;

    public SentryClientTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetExceptionFilterTestCases))]
    public void CaptureEvent_ExceptionFilteredForType(bool shouldFilter, Exception exception, params IExceptionFilter[] filters)
    {
        foreach (var filter in filters)
        {
            _fixture.SentryOptions.AddExceptionFilter(filter);
        }

        var sut = _fixture.GetSut();
        var result = sut.CaptureException(exception);

        Assert.Equal(shouldFilter, result == default);
        _fixture.BackgroundWorker.Received(result == default ? 0 : 1).EnqueueEnvelope(Arg.Any<Envelope>());
    }

    public static IEnumerable<object[]> GetExceptionFilterTestCases()
    {
        var systemExceptionFilter = new ExceptionTypeFilter<SystemException>();
        var applicationExceptionFilter = new ExceptionTypeFilter<ApplicationException>();
        var aggregateExceptionFilter = new ExceptionTypeFilter<AggregateException>();

        // Filtered out for it's the exact filtered type
        yield return new object[]
        {
            true,
            new SystemException(),
            systemExceptionFilter
        };

        // Filtered for it's a derived type
        yield return new object[]
        {
            true,
            new ArithmeticException(),
            systemExceptionFilter
        };

        // Not filtered since it's not in the inheritance chain
        yield return new object[]
        {
            false,
            new Exception(),
            systemExceptionFilter
        };

        // Filtered because it's the only exception under an aggregate exception
        yield return new object[]
        {
            true,
            new AggregateException(new SystemException()),
            systemExceptionFilter
        };

        // Filtered because all exceptions under the aggregate exception are the filtered or derived type
        yield return new object[]
        {
            true,
            new AggregateException(new SystemException(), new ArithmeticException()),
            systemExceptionFilter
        };

        // Filtered because all exceptions under the aggregate exception are covered by all of the filters
        yield return new object[]
        {
            true,
            new AggregateException(new SystemException(), new ApplicationException()),
            systemExceptionFilter,
            applicationExceptionFilter
        };

        // Not filtered because there's an exception under the aggregate not covered by the filters
        yield return new object[]
        {
            false,
            new AggregateException(new SystemException(), new Exception()),
            systemExceptionFilter
        };

        // Filtered because we're specifically filtering out aggregate exceptions (strange, but should work)
        yield return new object[]
        {
            true,
            new AggregateException(),
            aggregateExceptionFilter
        };
    }

    [Fact]
    public void CaptureEvent_IdReturnedToString_NoDashes()
    {
        var sut = _fixture.GetSut();

        var evt = new SentryEvent(new Exception());

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

        var evt = new SentryEvent(new Exception());

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

        var evt = new SentryEvent(new Exception());

        _ = sut.CaptureEvent(evt, scope);

        exceptionProcessor.Received(1).Process(evt.Exception!, evt);
    }

    [Fact]
    public void CaptureEvent_NullEventWithScope_EmptyGuid()
    {
        var sut = _fixture.GetSut();
        Assert.Equal(default, sut.CaptureEvent(null, new Scope(_fixture.SentryOptions)));
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

        var sut = _fixture.GetSut();

        var actualId = sut.CaptureEvent(expectedEvent);
        Assert.Equal(expectedId, (Guid)actualId);
    }

    [Fact]
    public void CaptureEvent_EventAndScope_QueuesEvent()
    {
        var expectedId = Guid.NewGuid();
        var expectedEvent = new SentryEvent(eventId: expectedId);

        var sut = _fixture.GetSut();

        var actualId = sut.CaptureEvent(expectedEvent, new Scope(_fixture.SentryOptions));
        Assert.Equal(expectedId, (Guid)actualId);
    }

    [Fact]
    public void CaptureEvent_EventAndScope_EvaluatesScope()
    {
        var scope = new Scope(_fixture.SentryOptions);
        var sut = _fixture.GetSut();

        var evaluated = false;
        object actualSender = null;
        object actualScope = null;
        scope.OnEvaluating += (sender, activeScope) =>
        {
            actualSender = sender;
            actualScope = activeScope;
            evaluated = true;
        };

        _ = sut.CaptureEvent(new SentryEvent(), scope);

        Assert.True(evaluated);
        Assert.Same(scope, actualSender);
        Assert.Same(scope, actualScope);
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
    public void CaptureEvent_Redact_Breadcrumbs()
    {
        // Act
        var scope = new Scope(_fixture.SentryOptions);
        scope.AddBreadcrumb("Visited https://user@sentry.io in session");
        var @event = new SentryEvent();

        // Act
        Envelope envelope = null;
        var sut = _fixture.GetSut();
        sut.Worker.EnqueueEnvelope(Arg.Do<Envelope>(e => envelope = e));
        _ = sut.CaptureEvent(@event, scope);

        // Assert
        envelope.Should().NotBeNull();
        envelope.Items.Count.Should().Be(1);
        var actual = (SentryEvent)(envelope.Items[0].Payload as JsonSerializable)?.Source;
        actual.Should().NotBeNull();
        actual?.Breadcrumbs.Count.Should().Be(1);
        actual?.Breadcrumbs.ToArray()[0].Message.Should().Be($"Visited https://{PiiExtensions.RedactedText}@sentry.io in session");
    }

    [Fact]
    public void CaptureEvent_BeforeEvent_RejectEvent()
    {
        _fixture.SentryOptions.SetBeforeSend((_, _) => null);
        var expectedEvent = new SentryEvent();

        var sut = _fixture.GetSut();
        var actualId = sut.CaptureEvent(expectedEvent, new Scope(_fixture.SentryOptions));

        Assert.Equal(default, actualId);
        _ = _fixture.BackgroundWorker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureEvent_BeforeEvent_RejectEvent_RecordsDiscard()
    {
        _fixture.SentryOptions.SetBeforeSend((_, _) => null);

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(new SentryEvent());

        _fixture.ClientReportRecorder.Received(1)
            .RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Error);
    }

    [Fact]
    public void CaptureEvent_EventProcessor_RejectEvent_RecordsDiscard()
    {
        var processor = Substitute.For<ISentryEventProcessor>();
        processor.Process(Arg.Any<SentryEvent>()).ReturnsNull();

        _fixture.SentryOptions.AddEventProcessor(processor);

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(new SentryEvent());

        _fixture.ClientReportRecorder.Received(1)
            .RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
    }

    [Fact]
    public void CaptureEvent_ExceptionFilter_RecordsDiscard()
    {
        var filter = Substitute.For<IExceptionFilter>();
        filter.Filter(Arg.Any<Exception>()).Returns(true);

        _fixture.SentryOptions.AddExceptionFilter(filter);

        var sut = _fixture.GetSut();
        _ = sut.CaptureException(new Exception());

        _fixture.ClientReportRecorder.Received(1)
            .RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
    }

    [Fact]
    public void CaptureEvent_BeforeSend_GetsHint()
    {
        SentryHint received = null;
        _fixture.SentryOptions.SetBeforeSend((e, h) =>
        {
            received = h;
            return e;
        });

        var @event = new SentryEvent();
        var hint = new SentryHint();

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(@event, hint: hint);

        Assert.Same(hint, received);
    }

    [Fact]
    public void CaptureEvent_BeforeSend_Gets_ScopeAttachments()
    {
        // Arrange
        SentryHint hint = null;
        _fixture.SentryOptions.SetBeforeSend((e, h) =>
        {
            hint = h;
            return e;
        });
        var scope = new Scope(_fixture.SentryOptions);
        scope.AddAttachment(AttachmentHelper.FakeAttachment("foo.txt"));
        scope.AddAttachment(AttachmentHelper.FakeAttachment("bar.txt"));

        var sut = _fixture.GetSut();

        // Act
        _ = sut.CaptureEvent(new SentryEvent(), scope);

        // Assert
        hint.Should().NotBeNull();
        hint.Attachments.Should().Contain(scope.Attachments);
    }

    [Fact]
    public void CaptureEvent_BeforeSendAddsAttachment_EnvelopeContainsAttachment()
    {
        // Arrange
        _fixture.SentryOptions.SetBeforeSend((e, h) =>
        {
            h.Attachments.Add(AttachmentHelper.FakeAttachment("foo.txt"));
            return e;
        });

        var sut = _fixture.GetSut();
        Envelope envelope = null;
        sut.Worker.EnqueueEnvelope(Arg.Do<Envelope>(e => envelope = e));

        // Act
        _ = sut.CaptureEvent(new SentryEvent());

        // Assert
        envelope.Should().NotBeNull();
        envelope.Items.Count.Should().Be(2);
        Assert.True(envelope.Items[1].Header.ContainsKey("filename"));
        Assert.True((string)envelope.Items[1].Header["filename"] == "foo.txt");
    }

    [Fact]
    public void CaptureEvent_EventProcessor_Gets_Hint()
    {
        // Arrange
        var processor = Substitute.For<ISentryEventProcessorWithHint>();
        processor.Process(Arg.Any<SentryEvent>(), Arg.Any<SentryHint>()).Returns(new SentryEvent());
        _fixture.SentryOptions.AddEventProcessor(processor);

        // Act
        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(new SentryEvent());

        // Assert
        processor.Received(1).Process(Arg.Any<SentryEvent>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void CaptureEvent_EventProcessor_Gets_ScopeAttachments()
    {
        // Arrange
        var processor = Substitute.For<ISentryEventProcessorWithHint>();
        SentryHint hint = null;
        processor.Process(Arg.Any<SentryEvent>(), Arg.Do<SentryHint>(h => hint = h)).Returns(new SentryEvent());
        _fixture.SentryOptions.AddEventProcessor(processor);

        var scope = new Scope(_fixture.SentryOptions);
        scope.AddAttachment(AttachmentHelper.FakeAttachment("foo.txt"));

        // Act
        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(new SentryEvent(), scope);

        // Assert
        hint.Should().NotBeNull();
        hint.Attachments.Should().Contain(scope.Attachments);
    }

    [Fact]
    public void CaptureEvent_Gets_ScopeAttachments()
    {
        // Arrange
        var scope = new Scope(_fixture.SentryOptions);
        scope.AddAttachment(AttachmentHelper.FakeAttachment("foo.txt"));
        scope.AddAttachment(AttachmentHelper.FakeAttachment("bar.txt"));

        var sut = _fixture.GetSut();

        // Act
        sut.CaptureEvent(new SentryEvent(), scope);

        // Assert
        sut.Worker.Received(1).EnqueueEnvelope(Arg.Is<Envelope>(envelope =>
            envelope.Items.Count(item => item.TryGetType() == "attachment") == 2));
    }

    [Fact]
    public void CaptureEvent_Gets_HintAttachments()
    {
        // Arrange
        var scope = new Scope(_fixture.SentryOptions);
        _fixture.SentryOptions.SetBeforeSend((e, h) =>
        {
            h.Attachments.Add(AttachmentHelper.FakeAttachment("foo.txt"));
            h.Attachments.Add(AttachmentHelper.FakeAttachment("bar.txt"));
            return e;
        });

        var sut = _fixture.GetSut();

        // Act
        sut.CaptureEvent(new SentryEvent(), scope);

        // Assert
        sut.Worker.Received(1).EnqueueEnvelope(Arg.Is<Envelope>(envelope =>
            envelope.Items.Count(item => item.TryGetType() == "attachment") == 2));
    }

    [Fact]
    public void CaptureEvent_Gets_ScopeAndHintAttachments()
    {
        // Arrange
        var scope = new Scope(_fixture.SentryOptions);
        scope.AddAttachment(AttachmentHelper.FakeAttachment("foo.txt"));
        _fixture.SentryOptions.SetBeforeSend((e, h) =>
        {
            h.Attachments.Add(AttachmentHelper.FakeAttachment("bar.txt"));
            return e;
        });

        var sut = _fixture.GetSut();

        // Act
        sut.CaptureEvent(new SentryEvent(), scope);

        // Assert
        sut.Worker.Received(1).EnqueueEnvelope(Arg.Is<Envelope>(envelope =>
            envelope.Items.Count(item => item.TryGetType() == "attachment") == 2));
    }

    [Fact]
    public void CaptureEvent_CanRemove_ScopetAttachment()
    {
        // Arrange
        var scope = new Scope(_fixture.SentryOptions);
        scope.AddAttachment(AttachmentHelper.FakeAttachment("foo.txt"));
        scope.AddAttachment(AttachmentHelper.FakeAttachment("bar.txt"));
        _fixture.SentryOptions.SetBeforeSend((e, h) =>
        {
            var attachment = h.Attachments.FirstOrDefault(a => a.FileName == "bar.txt");
            h.Attachments.Remove(attachment);

            return e;
        });

        var sut = _fixture.GetSut();

        // Act
        sut.CaptureEvent(new SentryEvent(), scope);

        // Assert
        sut.Worker.Received(1).EnqueueEnvelope(Arg.Is<Envelope>(envelope =>
            envelope.Items.Count(item => item.TryGetType() == "attachment") == 1));
    }

    [Fact]
    public void CaptureEvent_BeforeSend_ModifyEvent()
    {
        SentryEvent received = null;
        _fixture.SentryOptions.SetBeforeSend((e, _) => received = e);

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
        _fixture.SentryOptions.SetBeforeSend((e, _) => received = e);

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
        _fixture.SentryOptions.SetBeforeSend((e, _) => received = e);

        var @event = new SentryEvent();

        var sut = _fixture.GetSut();

        _ = sut.CaptureEvent(@event);

        Assert.Same(@event, received);
    }

    [Theory]
    [InlineData(0.25f)]
    [InlineData(0.50f)]
    [InlineData(0.75f)]
    public void CaptureEvent_WithSampleRate_AppropriateDistribution(float sampleRate)
    {
        // Arrange
        const int numEvents = 1000;
        const double allowedRelativeDeviation = 0.15;
        const uint allowedDeviation = (uint)(allowedRelativeDeviation * numEvents);
        var expectedSampled = (int)(sampleRate * numEvents);
        _fixture.SentryOptions.SampleRate = sampleRate;

        // This test expects an approximate uniform distribution of random numbers, so we'll retry a few times.
        TestHelpers.RetryTest(maxAttempts: 3, _output, () =>
        {
            // Act
            var client = _fixture.GetSut();
            var countSampled = 0;
            for (var i = 0; i < numEvents; i++)
            {
                var id = client.CaptureMessage($"Test[{i}]");
                if (id != SentryId.Empty)
                {
                    countSampled++;
                }
            }

            // Assert
            countSampled.Should().BeCloseTo(expectedSampled, allowedDeviation);
        });
    }

    [Fact]
    public void CaptureEvent_Processing_Order()
    {
        // Arrange
        var @event = new SentryEvent(new Exception());
        var processingOrder = new List<string>();

        var exceptionFilter = Substitute.For<IExceptionFilter>();
        exceptionFilter.Filter(Arg.Do<Exception>(_ =>
            processingOrder.Add("exceptionFilter")
            )).Returns(false);
        _fixture.SentryOptions.ExceptionFilters.Add(exceptionFilter);

        var exceptionProcessor = Substitute.For<ISentryEventExceptionProcessor>();
        exceptionProcessor
            .When(x => x.Process(Arg.Any<Exception>(), Arg.Any<SentryEvent>()))
            .Do(_ => processingOrder.Add("exceptionProcessor"));
        var scope = new Scope(_fixture.SentryOptions);
        scope.ExceptionProcessors.Add(exceptionProcessor);

        var eventProcessor = Substitute.For<ISentryEventProcessor>();
        eventProcessor.Process(default).ReturnsForAnyArgs(_ =>
        {
            processingOrder.Add("eventProcessor");
            return @event;
        });
        _fixture.SentryOptions.AddEventProcessor(eventProcessor);

        _fixture.SentryOptions.SetBeforeSend((e, _) =>
        {
            processingOrder.Add("SetBeforeSend");
            return e;
        });

        _fixture.SessionManager.When(x => x.ReportError())
            .Do(_ => processingOrder.Add("UpdateSession"));

        var logger = Substitute.For<IDiagnosticLogger>();
        logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
        logger.When(x => x.Log(Arg.Any<SentryLevel>(), Arg.Is("Event not sampled.")))
            .Do(_ => processingOrder.Add("SampleRate"));
        _fixture.SentryOptions.DiagnosticLogger = logger;
        _fixture.SentryOptions.Debug = true;

        // Act
        var client = _fixture.GetSut();
        client.CaptureEvent(@event, scope);

        // Assert
        // See https://github.com/getsentry/sentry-dotnet/issues/1599
        var expectedOrder = new List<string>()
        {
            "exceptionFilter",
            "exceptionProcessor",
            "eventProcessor",
            "SetBeforeSend",
            "UpdateSession",
            "SampleRate"
        };
        processingOrder.Should().Equal(expectedOrder);
    }

    [Fact]
    public void CaptureEvent_SessionRunningAndHasException_ReportsErrorButDoesNotEndSession()
    {
        _fixture.BackgroundWorker.EnqueueEnvelope(Arg.Do<Envelope>(envelope =>
        {
            var sessionItems = envelope.Items.Where(x => x.TryGetType() == "session");
            foreach (var item in sessionItems)
            {
                var session = (SessionUpdate)((JsonSerializable)item.Payload).Source;
                Assert.Equal(1, session.ErrorCount);
                Assert.Null(session.EndStatus);
            }
        }));
        _fixture.SessionManager = new GlobalSessionManager(_fixture.SentryOptions);
        _fixture.SessionManager.StartSession();

        _fixture.GetSut().CaptureEvent(new SentryEvent(new Exception("test exception")));
    }

    [Fact]
    public void CaptureEvent_SessionRunningAndHasTerminalException_ReportsErrorAndEndsSessionAsCrashed()
    {
        _fixture.BackgroundWorker.EnqueueEnvelope(Arg.Do<Envelope>(envelope =>
        {
            var sessionItems = envelope.Items.Where(x => x.TryGetType() == "session");
            foreach (var item in sessionItems)
            {
                var session = (SessionUpdate)((JsonSerializable)item.Payload).Source;
                Assert.Equal(1, session.ErrorCount);
                Assert.NotNull(session.EndStatus);
                Assert.Equal(SessionEndStatus.Crashed, session.EndStatus);
            }
        }));
        _fixture.SessionManager = new GlobalSessionManager(_fixture.SentryOptions);
        _fixture.SessionManager.StartSession();

        var exception = new Exception("test exception");
        exception.SetSentryMechanism("test mechanism", handled: false);
        _fixture.GetSut().CaptureEvent(new SentryEvent(exception));
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
            new UserFeedback(SentryId.Empty, "name", "email", "comment"));

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
            new UserFeedback(SentryId.Parse("4eb98e5f861a41019f270a7a27e84f02"), "name", "email", "comment"));

        //Assert
        _ = sut.Worker.Received(1).EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureUserFeedback_EventIdEmpty_FeedbackIgnored()
    {
        //Arrange
        var sut = _fixture.GetSut();

        //Act
        sut.CaptureUserFeedback(new UserFeedback(SentryId.Empty, "name", "email", "comment"));

        //Assert
        _ = sut.Worker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
    }
    [Fact]
    public void Dispose_should_only_flush()
    {
        // Arrange
        var client = _fixture.GetSut();

        // Act
        client.Dispose();

        //Assert is still usable
        client.CaptureEvent(new SentryEvent { Message = "Test" });
    }

    [Fact]
    public void CaptureUserFeedback_DisposedClient_DoesNotThrow()
    {
        var sut = _fixture.GetSut();
        sut.Dispose();
        sut.CaptureUserFeedback(new UserFeedback(SentryId.Empty, "name", "email", "comment"));
    }

    [Fact]
    public void CaptureTransaction_SampledOut_Dropped()
    {
        // Arrange
        var client = _fixture.GetSut();

        // Act
        client.CaptureTransaction(new SentryTransaction(
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
            new SentryTransaction(
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

        var transaction = new SentryTransaction(
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
            new SentryTransaction(
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
            new SentryTransaction(
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
            new SentryTransaction(
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
            new SentryTransaction(
                "test name",
                "test operation")
            {
                IsSampled = true,
                EndTimestamp = null // not finished
            });
    }

    [Fact]
    public void CaptureTransaction_Redact_Description()
    {
        // Arrange
        _fixture.SentryOptions.SendDefaultPii = false;
        var client = _fixture.GetSut();
        var original = new SentryTransaction(
            "test name",
            "test operation"
        )
        {
            IsSampled = true,
            Description = "The URL: https://user@sentry.io has PII data in it",
            EndTimestamp = DateTimeOffset.Now // finished
        };

        // Act
        Envelope envelope = null;
        client.Worker.EnqueueEnvelope(Arg.Do<Envelope>(e => envelope = e));
        client.CaptureTransaction(original);

        // Assert
        envelope.Should().NotBeNull();
        envelope.Items.Count.Should().Be(1);
        var actual = (envelope.Items[0].Payload as JsonSerializable)?.Source as SentryTransaction;
        actual?.Name.Should().Be(original.Name);
        actual?.Operation.Should().Be(original.Operation);
        actual?.Description.Should().Be(original.Description.RedactUrl()); // Should be redacted
    }

    [Fact]
    public void CaptureTransaction_BeforeSendTransaction_RejectEvent()
    {
        _fixture.SentryOptions.SetBeforeSendTransaction((_, _) => null);

        var sut = _fixture.GetSut();
        sut.CaptureTransaction(
            new SentryTransaction("test name", "test operation")
            {
                IsSampled = true,
                EndTimestamp = DateTimeOffset.Now // finished
            });

        _ = _fixture.BackgroundWorker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureTransaction_ScopeContainsAttachments_GetAppliedToHint()
    {
        // Arrange
        var transaction = new SentryTransaction("name", "operation")
        {
            IsSampled = true,
            EndTimestamp = DateTimeOffset.Now // finished
        };
        var attachments = new List<SentryAttachment> {
            AttachmentHelper.FakeAttachment("foo"),
            AttachmentHelper.FakeAttachment("bar")
        };
        var scope = new Scope(_fixture.SentryOptions);
        scope.AddAttachment(attachments[0]);
        scope.AddAttachment(attachments[1]);

        SentryHint hint = null;
        _fixture.SentryOptions.SetBeforeSendTransaction((e, h) =>
        {
            hint = h;
            return e;
        });

        // Act
        _fixture.GetSut().CaptureTransaction(transaction, scope, hint);

        // Assert
        hint.Should().NotBeNull();
        hint.Attachments.Should().Contain(attachments);
    }

    [Fact]
    public void CaptureTransaction_AddedTransactionProcessor_ReceivesHint()
    {
        // Arrange
        var processor = Substitute.For<ISentryTransactionProcessorWithHint>();
        processor.Process(Arg.Any<SentryTransaction>(), Arg.Any<SentryHint>()).Returns(new SentryTransaction("name", "operation"));
        _fixture.SentryOptions.AddTransactionProcessor(processor);

        var transaction = new SentryTransaction("name", "operation")
        {
            IsSampled = true,
            EndTimestamp = DateTimeOffset.Now // finished
        };

        // Act
        _fixture.GetSut().CaptureTransaction(transaction);

        // Assert
        processor.Received(1).Process(Arg.Any<SentryTransaction>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void CaptureTransaction_TransactionProcessor_ReceivesScopeAttachments()
    {
        // Arrange
        var transaction = new SentryTransaction("name", "operation")
        {
            IsSampled = true,
            EndTimestamp = DateTimeOffset.Now // finished
        };

        var processor = Substitute.For<ISentryTransactionProcessorWithHint>();
        SentryHint hint = null;
        processor.Process(
            Arg.Any<SentryTransaction>(),
            Arg.Do<SentryHint>(h => hint = h))
            .Returns(new SentryTransaction("name", "operation"));
        _fixture.SentryOptions.AddTransactionProcessor(processor);

        var attachments = new List<SentryAttachment> { AttachmentHelper.FakeAttachment("foo.txt") };
        var scope = new Scope(_fixture.SentryOptions);
        scope.AddAttachment(attachments[0]);

        // Act
        var client = _fixture.GetSut();
        client.CaptureTransaction(transaction, scope, null);

        // Assert
        hint.Should().NotBeNull();
        hint.Attachments.Should().Contain(attachments);
    }

    [Fact]
    public void CaptureTransaction_BeforeSendTransaction_GetsHint()
    {
        SentryHint received = null;
        _fixture.SentryOptions.SetBeforeSendTransaction((tx, h) =>
        {
            received = h;
            return tx;
        });

        var transaction = new SentryTransaction("test name", "test operation")
        {
            IsSampled = true,
            EndTimestamp = DateTimeOffset.Now // finished
        };

        var sut = _fixture.GetSut();
        var hint = new SentryHint();
        sut.CaptureTransaction(transaction, null, hint);

        Assert.Same(hint, received);
    }

    [Fact]
    public void CaptureTransaction_BeforeSendTransaction_ModifyEvent()
    {
        SentryTransaction received = null;
        _fixture.SentryOptions.SetBeforeSendTransaction((tx, _) => received = tx);

        var transaction = new SentryTransaction("test name", "test operation")
        {
            IsSampled = true,
            EndTimestamp = DateTimeOffset.Now // finished
        };

        var sut = _fixture.GetSut();
        sut.CaptureTransaction(transaction);

        Assert.Same(transaction, received);
    }

    [Fact]
    public void CaptureTransaction_BeforeSendTransaction_replaced_transaction_captured()
    {
        SentryTransaction received = null;
        _fixture.SentryOptions.SetBeforeSendTransaction((_, _) =>
        {
            received = new SentryTransaction("name2", "operation2")
            {
                IsSampled = true,
                EndTimestamp = DateTimeOffset.Now,
                Description = "modified transaction"
            };

            return received;
        });

        var transaction = new SentryTransaction("name", "operation")
        {
            IsSampled = true,
            EndTimestamp = DateTimeOffset.Now // finished
        };

        Envelope captured = null;
        _fixture.BackgroundWorker.EnqueueEnvelope(Arg.Do<Envelope>(x => captured = x));

        var sut = _fixture.GetSut();
        sut.CaptureTransaction(transaction);

        Assert.NotSame(transaction, received);

        var capturedEnvelopedTransaction = captured.Items[0].Payload.As<JsonSerializable>().Source.As<SentryTransaction>();
        Assert.Same(received.Description, capturedEnvelopedTransaction.Description);
    }

    [Fact]
    public void CaptureTransaction_BeforeSendTransaction_SamplingNull_DropsEvent()
    {
        _fixture.SentryOptions.SampleRate = null;

        SentryTransaction received = null;
        _fixture.SentryOptions.SetBeforeSendTransaction((e, _) => received = e);

        var transaction = new SentryTransaction("test name", "test operation")
        {
            IsSampled = true,
            EndTimestamp = DateTimeOffset.Now // finished
        };

        var sut = _fixture.GetSut();

        sut.CaptureTransaction(transaction);

        Assert.Same(transaction, received);
    }

    [Fact]
    public void CaptureTransaction_BeforeSendTransaction_RejectEvent_RecordsDiscard()
    {
        _fixture.SentryOptions.SetBeforeSendTransaction((_, _) => null);

        var sut = _fixture.GetSut();
        sut.CaptureTransaction(new SentryTransaction("test name", "test operation")
        {
            IsSampled = true,
            EndTimestamp = DateTimeOffset.Now // finished
        });

        _fixture.ClientReportRecorder.Received(1)
            .RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Transaction);
    }

    [Fact]
    public void CaptureCheckIn_CheckInHasReleaseAndEnvironment()
    {
        _fixture.SentryOptions.Release = "my-test-release";
        _fixture.SentryOptions.Environment = "my-test-environment";
        Envelope envelope = null;
        var sut = _fixture.GetSut();
        sut.Worker.EnqueueEnvelope(Arg.Do<Envelope>(e => envelope = e));

        sut.CaptureCheckIn("my-monitor", CheckInStatus.InProgress);

        var actualCheckIn = (SentryCheckIn)(envelope.Items[0].Payload as JsonSerializable)?.Source;
        Assert.NotNull(actualCheckIn);
        Assert.Equal(_fixture.SentryOptions.Release, actualCheckIn.Release);
        Assert.Equal(_fixture.SentryOptions.Environment, actualCheckIn.Environment);
    }

    [Fact]
    public void CaptureCheckIn_DurationProvided_CheckInHasDuration()
    {
        Envelope envelope = null;
        var sut = _fixture.GetSut();
        sut.Worker.EnqueueEnvelope(Arg.Do<Envelope>(e => envelope = e));
        var duration = TimeSpan.FromSeconds(5);

        sut.CaptureCheckIn("my-monitor", CheckInStatus.InProgress, duration: duration);

        var actualCheckIn = (SentryCheckIn)(envelope.Items[0].Payload as JsonSerializable)?.Source;
        Assert.NotNull(actualCheckIn);
        Assert.Equal(duration, actualCheckIn.Duration);
    }

    [Fact]
    public void CaptureCheckIn_ScopeProvided_CheckInHasTraceIdFromScope()
    {
        var scope = new Scope(null, new SentryPropagationContext());
        Envelope envelope = null;
        var sut = _fixture.GetSut();
        sut.Worker.EnqueueEnvelope(Arg.Do<Envelope>(e => envelope = e));

        sut.CaptureCheckIn("my-monitor", CheckInStatus.InProgress, scope: scope);

        var actualCheckIn = (SentryCheckIn)(envelope.Items[0].Payload as JsonSerializable)?.Source;
        Assert.NotNull(actualCheckIn);
        Assert.Equal(scope.PropagationContext.TraceId, actualCheckIn.TraceId);
    }

    [Fact]
    public void CaptureCheckIn_ScopeHasSpan_CheckInHasTraceIdFromSpan()
    {
        var traceId = new SentryId();
        var transaction = Substitute.For<ITransactionTracer>();
        transaction.TraceId.Returns(traceId);
        var scope = new Scope(null, new SentryPropagationContext()) { Transaction = transaction };

        Envelope envelope = null;
        var sut = _fixture.GetSut();
        sut.Worker.EnqueueEnvelope(Arg.Do<Envelope>(e => envelope = e));

        sut.CaptureCheckIn("my-monitor", CheckInStatus.InProgress, scope: scope);

        var actualCheckIn = (SentryCheckIn)(envelope.Items[0].Payload as JsonSerializable)?.Source;
        Assert.NotNull(actualCheckIn);
        Assert.Equal(traceId, actualCheckIn.TraceId);
    }

    [Fact]
    public void CaptureCheckIn_CheckInOptionsProvided_CheckInHasOptions()
    {
        Envelope envelope = null;
        var sut = _fixture.GetSut();
        sut.Worker.EnqueueEnvelope(Arg.Do<Envelope>(e => envelope = e));

        var monitorOptions = new SentryMonitorOptions
        {
            CheckInMargin = TimeSpan.FromMinutes(1),
            MaxRuntime = TimeSpan.FromMinutes(1),
            FailureIssueThreshold = 1,
            RecoveryThreshold = 1,
            TimeZone = "America/Los_Angeles",
            Owner = "test-owner"
        };

        sut.CaptureCheckIn("my-monitor", CheckInStatus.InProgress, configureMonitorOptions: options =>
        {
            options.CheckInMargin = monitorOptions.CheckInMargin;
            options.MaxRuntime = monitorOptions.MaxRuntime;
            options.FailureIssueThreshold = monitorOptions.FailureIssueThreshold;
            options.RecoveryThreshold = monitorOptions.RecoveryThreshold;
            options.TimeZone = monitorOptions.TimeZone;
            options.Owner = monitorOptions.Owner;
        });

        var actualCheckIn = (SentryCheckIn)(envelope.Items[0].Payload as JsonSerializable)?.Source;
        Assert.NotNull(actualCheckIn);
        Assert.NotNull(actualCheckIn.MonitorOptions);
        Assert.Equal(actualCheckIn.MonitorOptions.CheckInMargin, monitorOptions.CheckInMargin);
        Assert.Equal(actualCheckIn.MonitorOptions.MaxRuntime, monitorOptions.MaxRuntime);
        Assert.Equal(actualCheckIn.MonitorOptions.FailureIssueThreshold, monitorOptions.FailureIssueThreshold);
        Assert.Equal(actualCheckIn.MonitorOptions.RecoveryThreshold, monitorOptions.RecoveryThreshold);
        Assert.Equal(actualCheckIn.MonitorOptions.TimeZone, monitorOptions.TimeZone);
        Assert.Equal(actualCheckIn.MonitorOptions.Owner, monitorOptions.Owner);
    }

    [Fact]
    public void CaptureCheckIn_IntervalSetMoreThanOnce_Throws()
    {
        Assert.Throws<ArgumentException>(() => _fixture.GetSut().CaptureCheckIn("my-monitor", CheckInStatus.InProgress,
            configureMonitorOptions: options =>
            {
                options.Interval(1, SentryMonitorInterval.Month);
                options.Interval(2, SentryMonitorInterval.Day);
            }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a crontab")]
    [InlineData("* * a * *")]
    [InlineData("60 * * * *")]
    [InlineData("* 24 * * *")]
    [InlineData("* * 32 * *")]
    [InlineData("* * * 13 *")]
    [InlineData("* * * * 8")]
    public void CaptureCheckIn_InvalidCrontabSet_Throws(string crontab)
    {
        Assert.Throws<ArgumentException>(() => _fixture.GetSut().CaptureCheckIn("my-monitor", CheckInStatus.InProgress,
            configureMonitorOptions: options => options.Interval(crontab)));
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
    public async Task HttpOptionsCallback_InvokedConfigureClient_when_sending_envelope()
    {
        var invoked = false;
        _fixture.BackgroundWorker = null;
        _fixture.SentryOptions.Dsn = ValidDsn;
        _fixture.SentryOptions.ConfigureClient = _ => invoked = true;

        using (_fixture.GetSut())
        {
            await _fixture.SentryOptions.Transport!.SendEnvelopeAsync(new Envelope(new Dictionary<string, object>(), new List<EnvelopeItem>()));
            Assert.True(invoked);
        }
    }

    [Fact]
    public async Task CreateHttpClientHandler_InvokedConfigureHandler_when_sending_envelope()
    {
        var invoked = false;
        _fixture.BackgroundWorker = null;
        _fixture.SentryOptions.Dsn = ValidDsn;
        _fixture.SentryOptions.CreateHttpMessageHandler = () =>
        {
            invoked = true;
            return Substitute.For<HttpClientHandler>();
        };

        using (_fixture.GetSut())
        {
            await _fixture.SentryOptions.Transport!.SendEnvelopeAsync(new Envelope(new Dictionary<string, object>(), new List<EnvelopeItem>()));
            Assert.True(invoked);
        }
    }

    [Fact]
    public void Ctor_NullBackgroundWorker_ConcreteBackgroundWorker()
    {
        _fixture.SentryOptions.Dsn = ValidDsn;
        _fixture.SentryOptions.Transport = Substitute.For<ITransport>();

        using var sut = new SentryClient(_fixture.SentryOptions);
        _ = Assert.IsType<BackgroundWorker>(sut.Worker);
    }

    [Fact]
    public void Ctor_SetsTransportOnOptions()
    {
        _fixture.SentryOptions.Dsn = ValidDsn;

        using var sut = new SentryClient(_fixture.SentryOptions);

        _ = Assert.IsType<LazyHttpTransport>(_fixture.SentryOptions.Transport);
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
    public void CaptureEvent_Exception_ReportsError()
    {
        // Arrange
        var hub = _fixture.GetSut();

        // Act
        hub.CaptureEvent(new SentryEvent(new Exception()));

        // Assert
        _fixture.SessionManager.Received(1).ReportError();
    }

    [Fact]
    public void CaptureEvent_ActiveSession_UnhandledExceptionSessionEndedAsCrashed()
    {
        // Arrange
        var client = _fixture.GetSut();

        // Act
        client.CaptureEvent(new SentryEvent()
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Mechanism = new()
                    {
                        Handled = false
                    }
                }
            }
        });

        // Assert
        _fixture.SessionManager.Received().EndSession(SessionEndStatus.Crashed);
    }
}
