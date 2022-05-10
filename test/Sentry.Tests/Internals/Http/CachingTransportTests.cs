using System.Net.Http;
using System.Net.Sockets;
using NSubstitute.ExceptionExtensions;
using Sentry.Internal.Http;
using Sentry.Testing;

namespace Sentry.Tests.Internals.Http;

public class CachingTransportTests
{
    private readonly TestOutputDiagnosticLogger _logger;

    public CachingTransportTests(ITestOutputHelper testOutputHelper)
    {
        _logger = new TestOutputDiagnosticLogger(testOutputHelper);
    }

    [Fact]
    public async Task WithAttachment()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path
        };

        Exception exception = null;
        var innerTransport = new HttpTransport(options, new HttpClient(new CallbackHttpClientHandler(async message =>
         {
             try
             {
                 await message.Content!.ReadAsStringAsync();
             }
             catch (Exception readStreamException)
             {
                 exception = readStreamException;
             }
         })));

        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        var tempFile = Path.GetTempFileName();

        try
        {
            var attachment = new Attachment(AttachmentType.Default, new FileAttachmentContent(tempFile), "Attachment.txt", null);
            using var envelope = Envelope.FromEvent(new SentryEvent(), attachments: new[] { attachment });

            // Act
            await transport.SendEnvelopeAsync(envelope);
            await transport.FlushAsync();

            // Assert
            if (exception != null)
            {
                throw exception;
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task WorksInBackground()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path
        };

        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, options);

        // Attach to the EnvelopeSent event. We'll wait for that below.
        // ReSharper disable once AccessToDisposedClosure
        using var waiter = new Waiter<Envelope>(handler => innerTransport.EnvelopeSent += handler);

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);

        // wait for the inner transport to signal that it sent the envelope
        await waiter.WaitAsync(TimeSpan.FromSeconds(7));

        // Assert
        var sentEnvelope = innerTransport.GetSentEnvelopes().Single();
        sentEnvelope.Should().BeEquivalentTo(envelope, o => o.Excluding(x => x.Items[0].Header));
    }

    [Fact]
    public async Task ShouldNotLogOperationCanceledExceptionWhenIsCancellationRequested()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();

        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            CacheDirectoryPath = cacheDirectory.Path,
            Debug = true
        };

        var capturingCompletionSource = new TaskCompletionSource<object>();
        var cancelingCompletionSource = new TaskCompletionSource<object>();

        var innerTransport = Substitute.For<ITransport>();

        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(async _ =>
            {
                capturingCompletionSource.SetResult(null);
                await Task.WhenAny(cancelingCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(4)));
                throw new OperationCanceledException();
            });

        await using var transport = CachingTransport.Create(innerTransport, options);
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);

        await Task.WhenAny(capturingCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        Assert.True(capturingCompletionSource.Task.IsCompleted, "Inner transport was never called");

        var stopTask = transport.StopWorkerAsync();
        cancelingCompletionSource.SetResult(null); // Unblock the worker
        await stopTask;

        // Assert
        Assert.False(_logger.HasErrorOrFatal, "Error or fatal message logged");
    }

    [Fact]
    public async Task ShouldLogOperationCanceledExceptionWhenNotIsCancellationRequested()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var loggerCompletionSource = new TaskCompletionSource<object>();

        var logger = Substitute.For<IDiagnosticLogger>();
        logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
        logger
            .When(l =>
                l.Log(SentryLevel.Error,
                    "Exception in background worker of CachingTransport.",
                    Arg.Any<OperationCanceledException>(),
                    Arg.Any<object[]>()))
            .Do(_ => loggerCompletionSource.SetResult(null));

        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = logger,
            CacheDirectoryPath = cacheDirectory.Path,
            Debug = true
        };

        var innerTransport = Substitute.For<ITransport>();

        var capturingCompletionSource = new TaskCompletionSource<object>();
        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(_ =>
            {
                capturingCompletionSource.SetResult(null);
                return new OperationCanceledException();
            });

        await using var transport = CachingTransport.Create(innerTransport, options);
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);

        // Assert
        await Task.WhenAny(capturingCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        Assert.True(capturingCompletionSource.Task.IsCompleted, "Envelope never reached the transport");
        await Task.WhenAny(loggerCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        Assert.True(loggerCompletionSource.Task.IsCompleted, "Expected log call never received");
    }

    [Fact]
    public async Task EnvelopeReachesInnerTransport()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path
        };

        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);
        await transport.FlushAsync();

        // Assert
        var sentEnvelope = innerTransport.GetSentEnvelopes().Single();
        sentEnvelope.Should().BeEquivalentTo(envelope,
            o => o.Excluding(x => x.Items[0].Header));
    }

    [Fact]
    public async Task MaintainsLimit()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            MaxCacheItems = 2
        };

        var innerTransport = Substitute.For<ITransport>();
        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        // Act
        for (var i = 0; i < options.MaxCacheItems + 2; i++)
        {
            using var envelope = Envelope.FromEvent(new SentryEvent());
            await transport.SendEnvelopeAsync(envelope);
        }

        // Assert
        transport.GetCacheLength().Should().BeLessOrEqualTo(options.MaxCacheItems);
    }

    [Fact]
    public async Task AwareOfExistingFiles()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path
        };

        // Send some envelopes with a failing transport to make sure they all stay in cache
        var initialInnerTransport = new FakeFailingTransport();
        await using var initialTransport = CachingTransport.Create(initialInnerTransport, options, startWorker: false);

        for (var i = 0; i < 3; i++)
        {
            using var envelope = Envelope.FromEvent(new SentryEvent());
            await initialTransport.SendEnvelopeAsync(envelope);
        }

        // Move them all to processing and leave them there (due to FakeFailingTransport)
        await initialTransport.FlushAsync();

        // Act

        // Creating the transport should move files from processing during initialization.
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        // Flushing the worker will ensure all files are processed.
        await transport.FlushAsync();

        // Assert
        innerTransport.GetSentEnvelopes().Should().HaveCount(3);
    }

    [Fact]
    public async Task NonTransientExceptionShouldLog()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path
        };

        var innerTransport = Substitute.For<ITransport>();

        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new Exception("The Message")));

        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);

        await transport.FlushAsync();

        var message = _logger.Entries
            .Where(x => x.Level == SentryLevel.Error)
            .Select(x => x.RawMessage)
            .Single();

        // Assert
        Assert.Equal("Failed to send cached envelope: {0}, discarding cached envelope. Envelope contents: {1}", message);
    }

    [Fact]
    public async Task DoesNotRetryOnNonTransientExceptions()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path
        };

        var innerTransport = Substitute.For<ITransport>();
        var isFailing = true;

        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
                // ReSharper disable once AccessToModifiedClosure
                isFailing
                    ? Task.FromException(new InvalidOperationException())
                    : Task.CompletedTask);

        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        // Act
        for (var i = 0; i < 3; i++)
        {
            using var envelope = Envelope.FromEvent(new SentryEvent());
            await transport.SendEnvelopeAsync(envelope);
        }

        await transport.FlushAsync();

        // (transport stops failing)
        innerTransport.ClearReceivedCalls();
        isFailing = false;
        await transport.FlushAsync();

        // Assert
        // (0 envelopes retried)
        _ = innerTransport.Received(0).SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordsDiscardedEventOnNonTransientExceptions()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            ClientReportRecorder = Substitute.For<IClientReportRecorder>()
        };


        var innerTransport = Substitute.For<ITransport>();
        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException()));

        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);
        await transport.FlushAsync();

        // Test that we recorded the discarded event
        options.ClientReportRecorder.Received(1)
            .RecordDiscardedEvent(DiscardReason.CacheOverflow, DataCategory.Error);
    }

    [Fact]
    public async Task RoundtripsClientReports()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path
        };

        var timestamp = DateTimeOffset.UtcNow;
        var clock = Substitute.For<ISystemClock>();
        clock.GetUtcNow().Returns(timestamp);
        var recorder = new ClientReportRecorder(options, clock);
        options.ClientReportRecorder = recorder;

        var innerTransport = new FakeTransportWithRecorder(recorder);
        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        // Act
        recorder.RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Error);
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope); // should internally generate a client report and write to disk
        var interimClientReport = recorder.GenerateClientReport(); // should be null
        await transport.FlushAsync(); // will read from disk and should send that client report

        // Test that the interim report was null
        Assert.Null(interimClientReport);

        // Test that we actually sent the discarded event
        var clientReport = new ClientReport(timestamp,
            new Dictionary<DiscardReasonWithCategory, int>
            {
                {DiscardReason.BeforeSend.WithCategory(DataCategory.Error), 1}
            }
        );
        var expected = await EnvelopeItem.FromClientReport(clientReport).Payload.SerializeToStringAsync(_logger);

        var envelopeSent = innerTransport.GetSentEnvelopes().Single();
        var envelopeItem = envelopeSent.Items.Single(x => x.TryGetType() == "client_report");
        var actual = await envelopeItem.Payload.SerializeToStringAsync(_logger);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task RestoresDiscardedEventCounts()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path
        };

        var recorder = (ClientReportRecorder) options.ClientReportRecorder;
        var innerTransport = new FakeTransportWithRecorder(recorder);
        await using var transport = CachingTransport.Create(innerTransport, options,
            startWorker: false, failStorage: true);

        // some arbitrary discarded events ahead of time
        recorder.RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Attachment);
        recorder.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
        recorder.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
        recorder.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Security);
        recorder.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Security);
        recorder.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Security);

        // Act
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var envelope = Envelope.FromEvent(new SentryEvent());
            await transport.SendEnvelopeAsync(envelope); // will fail, since we set failStorage to true
        });

        // Assert
        recorder.DiscardedEvents.Should().BeEquivalentTo(new Dictionary<DiscardReasonWithCategory, int>
        {
            // These are the original items recorded.  They should still be there.
            {DiscardReason.BeforeSend.WithCategory(DataCategory.Attachment), 1},
            {DiscardReason.EventProcessor.WithCategory(DataCategory.Error), 2},
            {DiscardReason.QueueOverflow.WithCategory(DataCategory.Security), 3}
        });
    }

    public static IEnumerable<object[]> NetworkTestData =>
        new List<object[]>
        {
            new object[] {new HttpRequestException(null)},
            new object[] {new IOException(null)},
            new object[] {new SocketException()}
        };

    [Theory]
    [MemberData(nameof(NetworkTestData))]
    public async Task TestNetworkException(Exception exception)
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path
        };

        var receivedException = new Exception();
        var innerTransport = Substitute.For<ITransport>();

        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(exception));

        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);

        try
        {
            // Act
            await transport.FlushAsync();
        }
        catch (Exception he)
        {
            receivedException = he;
        }
        finally
        {
            // (transport stops failing)
            innerTransport.ClearReceivedCalls();
            await transport.FlushAsync();
        }

        // Assert
        Assert.Equal(exception, receivedException);
        Assert.True(Directory.EnumerateFiles(cacheDirectory.Path, "*", SearchOption.AllDirectories).Any());
    }
}
