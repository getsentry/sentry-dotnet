using Sentry.Http;
using Sentry.Internal.Http;
using Sentry.Tests.Helpers;

namespace Sentry.Tests.Internals.Http;

public partial class HttpTransportTests
{
    private readonly IDiagnosticLogger _testOutputLogger;
    private readonly ISystemClock _fakeClock;

    public HttpTransportTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);

        _fakeClock = new MockClock(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task SendEnvelopeAsync_CancellationToken_PassedToClient()
    {
        // Arrange
        using var source = new CancellationTokenSource();
        source.Cancel();
        var token = source.Token;

        var httpHandler = Substitute.For<MockableHttpMessageHandler>();

        httpHandler.VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ => SentryResponses.GetOkResponse());

        var httpTransport = new HttpTransport(
            new SentryOptions { Dsn = ValidDsn },
            new HttpClient(httpHandler));

        var envelope = Envelope.FromEvent(
            new SentryEvent(eventId: SentryResponses.ResponseId));

#if NET5_0_OR_GREATER
        await Assert.ThrowsAsync<TaskCanceledException>(() => httpTransport.SendEnvelopeAsync(envelope, token));
#else
        // Act
        await httpTransport.SendEnvelopeAsync(envelope, token);

        // Assert
        await httpHandler
            .Received(1)
            .VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Is<CancellationToken>(c => c.IsCancellationRequested));
#endif
    }

    [Fact]
    public async Task SendEnvelopeAsync_ResponseNotOkWithJsonMessage_LogsError()
    {
        // Arrange
        const HttpStatusCode expectedCode = HttpStatusCode.BadGateway;
        const string expectedMessage = "Bad Gateway!";
        var expectedCauses = new[] { "invalid file", "wrong arguments" };
        var expectedCausesFormatted = string.Join(", ", expectedCauses);

        var httpHandler = Substitute.For<MockableHttpMessageHandler>();

        httpHandler.VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ => SentryResponses.GetJsonErrorResponse(expectedCode, expectedMessage, expectedCauses));

        var logger = new InMemoryDiagnosticLogger();

        var httpTransport = new HttpTransport(
            new SentryOptions
            {
                Dsn = ValidDsn,
                Debug = true,
                DiagnosticLogger = logger
            },
            new HttpClient(httpHandler));

        var envelope = Envelope.FromEvent(new SentryEvent());

        // Act
        await httpTransport.SendEnvelopeAsync(envelope);

        // Assert
        logger.Entries.Any(e =>
            e.Level == SentryLevel.Error &&
            e.Message == "{0}: Sentry rejected the envelope '{1}'. Status code: {2}. Error detail: {3}. Error causes: {4}." &&
            e.Exception == null &&
            e.Args[0].ToString() == "HttpTransport" &&
            e.Args[1].ToString() == envelope.TryGetEventId().ToString() &&
            e.Args[2].ToString() == expectedCode.ToString() &&
            e.Args[3].ToString() == expectedMessage &&
            e.Args[4].ToString() == expectedCausesFormatted
        ).Should().BeTrue();
    }

    [Fact]
    public async Task SendEnvelopeAsync_ResponseRequestEntityTooLargeWithPathDefined_StoresFile()
    {
        // Arrange
        var httpHandler = Substitute.For<MockableHttpMessageHandler>();

        httpHandler.VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ => SentryResponses.GetJsonErrorResponse(HttpStatusCode.RequestEntityTooLarge, ""));

        var logger = new InMemoryDiagnosticLogger();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            Debug = true,
            DiagnosticLogger = logger
        };

        var path = Path.GetTempPath();
        const string expectedEnvVar = "SENTRY_KEEP_LARGE_ENVELOPE_PATH";
        options.FakeSettings().EnvironmentVariables[expectedEnvVar] = path;

        var httpTransport = new HttpTransport(
            options,
            new HttpClient(httpHandler));

        var envelope = Envelope.FromEvent(new SentryEvent());

        // Act
        await httpTransport.SendEnvelopeAsync(envelope);

        // Assert
        logger.Entries.Any(e =>
                e.Level == SentryLevel.Debug &&
                e.Message == "{0}: Environment variable '{1}' set. Writing envelope to {2}" &&
                e.Exception == null &&
                e.Args[0].ToString() == "HttpTransport" &&
                e.Args[1].ToString() == expectedEnvVar &&
                e.Args[2].ToString() == path)
            .Should()
            .BeTrue();

        var fileStoredLogEntry = logger.Entries.FirstOrDefault(e =>
            e.Level == SentryLevel.Info &&
            e.Message == "{0}: Envelope's {1} bytes written to: {2}");

        Assert.NotNull(fileStoredLogEntry);
        var expectedFile = new FileInfo(fileStoredLogEntry.Args[2].ToString()!);
        Assert.True(expectedFile.Exists);
        try
        {
            Assert.Null(fileStoredLogEntry.Exception);
            // // Path is based on the provided path:
            Assert.Contains(path, fileStoredLogEntry.Args[2] as string);
            // // Path contains the envelope id in its name:
            Assert.Contains(envelope.TryGetEventId().ToString(), fileStoredLogEntry.Args[2] as string);
            Assert.Equal(expectedFile.Length, (long)fileStoredLogEntry.Args[1]);
        }
        finally
        {
            // It's in the temp folder but just to keep things tidy:
            expectedFile.Delete();
        }
    }

    [Fact]
    public async Task SendEnvelopeAsync_ResponseRequestEntityTooLargeWithoutPathDefined_DoesNotStoreFile()
    {
        // Arrange
        var httpHandler = Substitute.For<MockableHttpMessageHandler>();

        httpHandler.VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ => SentryResponses.GetJsonErrorResponse(HttpStatusCode.RequestEntityTooLarge, ""));

        var logger = new InMemoryDiagnosticLogger();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            Debug = true,
            DiagnosticLogger = logger
        };

        const string expectedEnvVar = "SENTRY_KEEP_LARGE_ENVELOPE_PATH";
        options.FakeSettings().EnvironmentVariables[expectedEnvVar] = null; // explicitly for this test

        var httpTransport = new HttpTransport(
            options,
            new HttpClient(httpHandler));

        // Act
        await httpTransport.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Assert
        logger.Entries.Any(e => e.Message == "Environment variable '{0}' set. Writing envelope to {1}")
            .Should()
            .BeFalse();

        logger.Entries.Any(e => e.Message == "Envelope's {0} bytes written to: {1}")
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task SendEnvelopeAsync_ResponseNotOkWithStringMessage_LogsError()
    {
        // Arrange
        const HttpStatusCode expectedCode = HttpStatusCode.RequestEntityTooLarge;
        const string expectedMessage = "413 Request Entity Too Large";

        var httpHandler = Substitute.For<MockableHttpMessageHandler>();

        _ = httpHandler.VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ => SentryResponses.GetTextErrorResponse(expectedCode, expectedMessage));

        var logger = new InMemoryDiagnosticLogger();

        var httpTransport = new HttpTransport(
            new SentryOptions
            {
                Dsn = ValidDsn,
                Debug = true,
                DiagnosticLogger = logger
            },
            new HttpClient(httpHandler));

        var envelope = Envelope.FromEvent(new SentryEvent());

        // Act
        await httpTransport.SendEnvelopeAsync(envelope);

        // Assert
        _ = logger.Entries.Any(e =>
            e.Level == SentryLevel.Error &&
            e.Message == "{0}: Sentry rejected the envelope '{1}'. Status code: {2}. Error detail: {3}." &&
            e.Exception == null &&
            e.Args[0].ToString() == "HttpTransport" &&
            e.Args[1].ToString() == envelope.TryGetEventId().ToString() &&
            e.Args[2].ToString() == expectedCode.ToString() &&
            e.Args[3].ToString() == expectedMessage
        ).Should().BeTrue();
    }

    [Fact]
    public async Task SendEnvelopeAsync_ResponseNotOkNoMessage_LogsError()
    {
        // Arrange
        const HttpStatusCode expectedCode = HttpStatusCode.BadGateway;

        var httpHandler = Substitute.For<MockableHttpMessageHandler>();

        httpHandler.VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ => SentryResponses.GetJsonErrorResponse(expectedCode, null));

        var logger = new InMemoryDiagnosticLogger();

        var httpTransport = new HttpTransport(
            new SentryOptions
            {
                Dsn = ValidDsn,
                Debug = true,
                DiagnosticLogger = logger
            },
            new HttpClient(httpHandler));

        var envelope = Envelope.FromEvent(new SentryEvent());

        // Act
        await httpTransport.SendEnvelopeAsync(envelope);

        // Assert
        logger.Entries.Any(e =>
            e.Level == SentryLevel.Error &&
            e.Message == "{0}: Sentry rejected the envelope '{1}'. Status code: {2}. Error detail: {3}. Error causes: {4}." &&
            e.Exception == null &&
            e.Args[0].ToString() == "HttpTransport" &&
            e.Args[1].ToString() == envelope.TryGetEventId().ToString() &&
            e.Args[2].ToString() == expectedCode.ToString() &&
            e.Args[3].ToString() == HttpTransportBase.DefaultErrorMessage &&
            e.Args[4].ToString() == string.Empty

        ).Should().BeTrue();
    }

    [Fact]
    public async Task SendEnvelopeAsync_ItemRateLimit_DropsItem()
    {
        // Arrange
        using var httpHandler = new RecordingHttpMessageHandler(
            new FakeHttpMessageHandler(
                () => SentryResponses.GetRateLimitResponse("1234:event, 897:transaction")
            ));

        var httpTransport = new HttpTransport(
            new SentryOptions
            {
                Dsn = ValidDsn,
                DiagnosticLogger = _testOutputLogger,
                SendClientReports = false,
                Debug = true
            },
            new HttpClient(httpHandler),
            clock: _fakeClock);

        // First request always goes through
        await httpTransport.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        var envelope = new Envelope(
            new Dictionary<string, object>(),
            new[]
            {
                // Should be dropped
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "event"},
                    new EmptySerializable()),
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "event"},
                    new EmptySerializable()),
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "transaction"},
                    new EmptySerializable()),

                // Should stay
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "other"},
                    new EmptySerializable())
            });

        var expectedEnvelope = new Envelope(
            new Dictionary<string, object>(),
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "other"},
                    new EmptySerializable())
            });

        var expectedEnvelopeSerialized = await expectedEnvelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Act
        await httpTransport.SendEnvelopeAsync(envelope);

        var lastRequest = httpHandler.GetRequests().Last();
        var actualEnvelopeSerialized = await lastRequest.Content!.ReadAsStringAsync();

        // Assert
        actualEnvelopeSerialized.Should().BeEquivalentTo(expectedEnvelopeSerialized);
    }

    [Fact]
    public async Task SendEnvelopeAsync_RateLimited_CountsDiscardedEventsCorrectly()
    {
        // Arrange
        using var httpHandler = new RecordingHttpMessageHandler(
            new FakeHttpMessageHandler(
                () => SentryResponses.GetRateLimitResponse("1234:event, 897:transaction")
            ));

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _testOutputLogger,
            SendClientReports = true,
            Debug = true
        };

        var recorder = new ClientReportRecorder(options, _fakeClock);
        options.ClientReportRecorder = recorder;

        var httpTransport = new HttpTransport(
            options,
            new HttpClient(httpHandler),
            clock: _fakeClock
        );

        // First request always goes through
        await httpTransport.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        var envelope = new Envelope(
            new Dictionary<string, object>(),
            new[]
            {
                // Should be dropped
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "event"},
                    new EmptySerializable()),
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "event"},
                    new EmptySerializable()),
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "transaction"},
                    new EmptySerializable()),

                // Should stay
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "other"},
                    new EmptySerializable())
            });

        // The client report should contain rate limit discards only.
        var expectedClientReport =
            new ClientReport(_fakeClock.GetUtcNow(),
                new Dictionary<DiscardReasonWithCategory, int>
                {
                    {DiscardReason.RateLimitBackoff.WithCategory(DataCategory.Error), 2},
                    {DiscardReason.RateLimitBackoff.WithCategory(DataCategory.Transaction), 1}
                });

        var expectedEnvelope = new Envelope(
            new Dictionary<string, object>(),
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "other"},
                    new EmptySerializable()),
                EnvelopeItem.FromClientReport(expectedClientReport)
            });

        var expectedEnvelopeSerialized = await expectedEnvelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Act
        await httpTransport.SendEnvelopeAsync(envelope);

        var lastRequest = httpHandler.GetRequests().Last();
        var actualEnvelopeSerialized = await lastRequest.Content!.ReadAsStringAsync();

        // Assert
        actualEnvelopeSerialized.Should().BeEquivalentTo(expectedEnvelopeSerialized);
    }

    [Fact]
    public async Task SendEnvelopeAsync_Fails_RestoresDiscardedEventCounts()
    {
        // Arrange
        using var httpHandler = new RecordingHttpMessageHandler(
            new FakeHttpMessageHandler(
                () => new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _testOutputLogger,
            SendClientReports = true,
            Debug = true
        };

        var httpTransport = new HttpTransport(options, new HttpClient(httpHandler));

        // some arbitrary discarded events ahead of time
        var recorder = (ClientReportRecorder)options.ClientReportRecorder;
        recorder.RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Attachment);
        recorder.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
        recorder.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
        recorder.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Security);
        recorder.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Security);
        recorder.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Security);

        // Act
        await httpTransport.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Assert
        recorder.DiscardedEvents.Should().BeEquivalentTo(new Dictionary<DiscardReasonWithCategory, int>
        {
            // These are the original items recorded.  They should still be there.
            {DiscardReason.BeforeSend.WithCategory(DataCategory.Attachment), 1},
            {DiscardReason.EventProcessor.WithCategory(DataCategory.Error), 2},
            {DiscardReason.QueueOverflow.WithCategory(DataCategory.Security), 3},

            // We also expect two new items recorded, due to the forced network failure.
            {DiscardReason.NetworkError.WithCategory(DataCategory.Error), 1},  // from the event
            {DiscardReason.NetworkError.WithCategory(DataCategory.Default), 1} // from the client report
        });
    }

    [Fact]
    public async Task SendEnvelopeAsync_RateLimited_DoesNotRestoreDiscardedEventCounts()
    {
        // Arrange
        using var httpHandler = new RecordingHttpMessageHandler(
            new FakeHttpMessageHandler(
                () => new HttpResponseMessage((HttpStatusCode)429)));

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _testOutputLogger,
            SendClientReports = true,
            Debug = true
        };

        var httpTransport = new HttpTransport(options, new HttpClient(httpHandler));

        // some arbitrary discarded events ahead of time
        var recorder = (ClientReportRecorder)options.ClientReportRecorder;
        recorder.RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Attachment);
        recorder.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
        recorder.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
        recorder.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Security);
        recorder.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Security);
        recorder.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Security);

        // Act
        await httpTransport.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Assert
        var totalCounts = recorder.DiscardedEvents.Values.Sum();
        Assert.Equal(0, totalCounts);
    }

    [Fact]
    public async Task SendEnvelopeAsync_AttachmentFail_DropsItem()
    {
        // Arrange
        using var httpHandler = new RecordingHttpMessageHandler(
            new FakeHttpMessageHandler());

        var logger = new InMemoryDiagnosticLogger();

        var httpTransport = new HttpTransport(
            new SentryOptions
            {
                Dsn = ValidDsn,
                MaxAttachmentSize = 1,
                DiagnosticLogger = logger,
                Debug = true
            },
            new HttpClient(httpHandler));

        var attachment = new Attachment(
            AttachmentType.Default,
            new FileAttachmentContent("test1.txt"),
            "test1.txt",
            null);

        using var envelope = Envelope.FromEvent(
            new SentryEvent(),
            logger,
            new[] { attachment });

        // Act
        await httpTransport.SendEnvelopeAsync(envelope);

        var lastRequest = httpHandler.GetRequests().Last();
        var actualEnvelopeSerialized = await lastRequest.Content!.ReadAsStringAsync();

        // Assert
        // (the envelope should have only one item)

        logger.Entries.Should().Contain(e =>
            e.Message == "Failed to add attachment: {0}." &&
            (string)e.Args[0] == "test1.txt");

        actualEnvelopeSerialized.Should().NotContain("test1.txt");
    }

    [Fact]
    public async Task SendEnvelopeAsync_AttachmentTooLarge_DropsItem()
    {
        // Arrange
        using var httpHandler = new RecordingHttpMessageHandler(
            new FakeHttpMessageHandler());

        var logger = new InMemoryDiagnosticLogger();

        var httpTransport = new HttpTransport(
            new SentryOptions
            {
                Dsn = ValidDsn,
                MaxAttachmentSize = 1,
                DiagnosticLogger = logger,
                Debug = true
            },
            new HttpClient(httpHandler));

        var attachmentNormal = new Attachment(
            AttachmentType.Default,
            new StreamAttachmentContent(new MemoryStream(new byte[] { 1 })),
            "test1.txt",
            null);

        var attachmentTooBig = new Attachment(
            AttachmentType.Default,
            new StreamAttachmentContent(new MemoryStream(new byte[] { 1, 2, 3, 4, 5 })),
            "test2.txt",
            null);

        using var envelope = Envelope.FromEvent(
            new SentryEvent(),
            null,
            new[] { attachmentNormal, attachmentTooBig });

        // Act
        await httpTransport.SendEnvelopeAsync(envelope);

        var lastRequest = httpHandler.GetRequests().Last();
        var actualEnvelopeSerialized = await lastRequest.Content!.ReadAsStringAsync();

        // Assert
        // (the envelope should have only one item)

        logger.Entries.Should().Contain(e =>
            string.Format(e.Message, e.Args) == "HttpTransport: Attachment 'test2.txt' dropped because it's too large (5 bytes).");

        actualEnvelopeSerialized.Should().NotContain("test2.txt");
    }

    [Fact]
    public async Task SendEnvelopeAsync_ItemRateLimit_PromotesNextSessionWithSameId()
    {
        // Arrange
        using var httpHandler = new RecordingHttpMessageHandler(
            new FakeHttpMessageHandler(
                () => SentryResponses.GetRateLimitResponse("1:session")
            ));

        var httpTransport = new HttpTransport(
            new SentryOptions
            {
                Dsn = ValidDsn
            },
            new HttpClient(httpHandler));

        var session = new Session("foo", "bar", "baz");

        // First request always goes through
        await httpTransport.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Send session update with init=true
        await httpTransport.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent(), null, null, session.CreateUpdate(true, DateTimeOffset.Now)));

        // Pretend the rate limit has already passed
        foreach (var (category, _) in httpTransport.CategoryLimitResets)
        {
            httpTransport.CategoryLimitResets[category] = DateTimeOffset.Now - TimeSpan.FromDays(1);
        }

        // Act

        // Send another update with init=false (should get promoted)
        await httpTransport.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent(), null, null, session.CreateUpdate(false, DateTimeOffset.Now)));

        var lastRequest = httpHandler.GetRequests().Last();
        var actualEnvelopeSerialized = await lastRequest.Content!.ReadAsStringAsync();

        // Assert
        actualEnvelopeSerialized.Should().Contain("\"init\":true");
    }

    [Fact]
    public async Task SendEnvelopeAsync_ItemRateLimit_DoesNotAffectNextSessionWithDifferentId()
    {
        // Arrange
        using var httpHandler = new RecordingHttpMessageHandler(
            new FakeHttpMessageHandler(
                () => SentryResponses.GetRateLimitResponse("1:session")
            ));

        var httpTransport = new HttpTransport(
            new SentryOptions
            {
                Dsn = ValidDsn
            },
            new HttpClient(httpHandler));

        var session = new Session("foo", "bar", "baz");

        // First request always goes through
        await httpTransport.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Send session update with init=true
        await httpTransport.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent(), null, null, session.CreateUpdate(true, DateTimeOffset.Now)));

        // Pretend the rate limit has already passed
        foreach (var (category, _) in httpTransport.CategoryLimitResets)
        {
            httpTransport.CategoryLimitResets[category] = DateTimeOffset.Now - TimeSpan.FromDays(1);
        }

        // Act

        // Send an update for different session with init=false (should NOT get promoted)
        var nextSession = new Session("foo2", "bar2", "baz2");
        await httpTransport.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent(), null, null, nextSession.CreateUpdate(false, DateTimeOffset.Now)));

        var lastRequest = httpHandler.GetRequests().Last();
        var actualEnvelopeSerialized = await lastRequest.Content!.ReadAsStringAsync();

        // Assert
        actualEnvelopeSerialized.Should().NotContain("\"init\":true");
    }

    [Fact]
    public void CreateRequest_AuthHeader_IsSet()
    {
        // Arrange
        var httpTransport = new HttpTransport(
            new SentryOptions { Dsn = ValidDsn },
            new HttpClient());

        var envelope = Envelope.FromEvent(new SentryEvent());

        // Act
        using var request = httpTransport.CreateRequest(envelope);
        var authHeader = request.Headers.GetValues("X-Sentry-Auth").FirstOrDefault();

        // Assert
        authHeader.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreateRequest_AuthHeader_IncludesVersion()
    {
        // Arrange
        var httpTransport = new HttpTransport(
            new SentryOptions { Dsn = ValidDsn },
            new HttpClient());

        var envelope = Envelope.FromEvent(new SentryEvent());

        // Act
        using var request = httpTransport.CreateRequest(envelope);
        var authHeader = request.Headers.GetValues("X-Sentry-Auth").First();

        // Assert
        var versionString = Regex.Match(authHeader, @"sentry_client=(\S+),sentry_key").Groups[1].Value;
        Assert.Contains(versionString, $"{SdkVersion.Instance.Name}/{SdkVersion.Instance.Version}");
    }

    [Fact]
    public void CreateRequest_RequestMethod_Post()
    {
        // Arrange
        var httpTransport = new HttpTransport(
            new SentryOptions { Dsn = ValidDsn },
            new HttpClient());

        var envelope = Envelope.FromEvent(new SentryEvent());

        // Act
        var request = httpTransport.CreateRequest(envelope);

        // Assert
        request.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public void CreateRequest_SentryUrl_FromOptions()
    {
        // Arrange
        var httpTransport = new HttpTransport(
            new SentryOptions { Dsn = ValidDsn },
            new HttpClient());

        var envelope = Envelope.FromEvent(new SentryEvent());

        var uri = Dsn.Parse(ValidDsn).GetEnvelopeEndpointUri();

        // Act
        var request = httpTransport.CreateRequest(envelope);

        // Assert
        request.RequestUri.Should().Be(uri);
    }

    [Fact]
    public async Task CreateRequest_Content_IncludesEvent()
    {
        // Arrange
        var httpTransport = new HttpTransport(
            new SentryOptions { Dsn = ValidDsn },
            new HttpClient());

        var envelope = Envelope.FromEvent(new SentryEvent());

        // Act
        var request = httpTransport.CreateRequest(envelope);
        var requestContent = await request.Content!.ReadAsStringAsync();

        // Assert
        requestContent.Should().Contain(envelope.TryGetEventId().ToString());
    }

    [Fact]
    public void ProcessEnvelope_ShouldNotAttachClientReportWhenOptionDisabled()
    {
        var options = new SentryOptions
        {
            // Disable sending of client reports
            SendClientReports = false
        };

        var httpTransport = Substitute.For<HttpTransportBase>(options, null, null);

        var recorder = options.ClientReportRecorder;
        recorder.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Error);

        var envelope = Envelope.FromEvent(new SentryEvent());
        var processedEnvelope = httpTransport.ProcessEnvelope(envelope);

        // There should only be the one event in the envelope
        Assert.Single(processedEnvelope.Items);
        Assert.Equal("event", processedEnvelope.Items[0].TryGetType());
    }
}
