using Sentry.Internal.Http;

namespace Sentry.Tests.Internals.Http;

public class CachingTransportTests
{
    private readonly TestOutputDiagnosticLogger _logger;
    private readonly IFileSystem _fileSystem = new FakeFileSystem();

    public CachingTransportTests(ITestOutputHelper testOutputHelper)
    {
        _logger = Substitute.ForPartsOf<TestOutputDiagnosticLogger>(testOutputHelper);
    }

    [Fact]
    public async Task WithAttachment()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem
        };

        string httpContent = null;
        Exception exception = null;
        var innerTransport = new HttpTransport(options, new HttpClient(new CallbackHttpClientHandler(async message =>
         {
             try
             {
                 httpContent = await message.Content!.ReadAsStringAsync();
             }
             catch (Exception readStreamException)
             {
                 exception = readStreamException;
             }
         })));

        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        const string attachmentContent = "test-attachment";
        var tempFile = Path.GetTempFileName();

#if NETCOREAPP
        await File.WriteAllTextAsync(tempFile, attachmentContent);
#else
        File.WriteAllText(tempFile, attachmentContent);
#endif

        try
        {
            var attachment = new SentryAttachment(AttachmentType.Default, new FileAttachmentContent(tempFile), "Attachment.txt", null);
            using var envelope = Envelope.FromEvent(new SentryEvent(), attachments: new[] { attachment });

            // Act
            await transport.SendEnvelopeAsync(envelope);
            await transport.FlushAsync();

            // Assert
            Assert.Contains(attachmentContent, httpContent);
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
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem
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
        sentEnvelope.Should().BeEquivalentTo(envelope);
    }

    [Fact]
    public async Task ShouldNotLogOperationCanceledExceptionWhenIsCancellationRequested()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory(_fileSystem);

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem,
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
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var loggerCompletionSource = new TaskCompletionSource<object>();

        _logger
            .When(l =>
                l.Log(SentryLevel.Error,
                    "Exception in CachingTransport worker.",
                    Arg.Any<OperationCanceledException>(),
                    Arg.Any<object[]>()))
            .Do(_ => loggerCompletionSource.SetResult(null));

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem,
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
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem
        };

        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);
        await transport.FlushAsync();

        // Assert
        var sentEnvelope = innerTransport.GetSentEnvelopes().Single();
        sentEnvelope.Should().BeEquivalentTo(envelope);
    }

    [Fact]
    public async Task MaintainsLimit()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem,
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
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem
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
    public async Task Handle_Malformed_Envelopes_Gracefully()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem
        };
        var cacheDirectoryPath =
            options.TryGetProcessSpecificCacheDirectoryPath() ??
            throw new InvalidOperationException("Cache directory or DSN is not set.");
        var processingDirectoryPath = Path.Combine(cacheDirectoryPath, "__processing");
        var fileName = $"{Guid.NewGuid()}.envelope";
        var filePath = Path.Combine(processingDirectoryPath, fileName);

        _fileSystem.CreateDirectory(processingDirectoryPath);   // Create the processing directory
        _fileSystem.CreateFileForWriting(filePath).Dispose(); // Make a malformed envelope... just an empty file
        _fileSystem.FileExists(filePath).Should().BeTrue();

        // Act
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);
        await transport.FlushAsync(); // Flush the worker to process

        // Assert
        _fileSystem.FileExists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task NonTransientExceptionShouldLog()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem
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
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem
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
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem,
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
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem
        };

        var timestamp = DateTimeOffset.UtcNow;
        var clock = new MockClock(timestamp);
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
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem
        };

        var recorder = (ClientReportRecorder)options.ClientReportRecorder;
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
            new object[] {new WebException(null)},
            new object[] {new IOException(null)},
            new object[] {new SocketException()}
        };

    [Theory]
    [MemberData(nameof(NetworkTestData))]
    public async Task TestNetworkException(Exception exception)
    {
        // Arrange
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem
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
        Assert.True(_fileSystem.EnumerateFiles(cacheDirectory.Path, "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task TransportSendsWithoutWaitingWhenNetworkIsOnline()
    {
        // Arrange
        var listener = Substitute.For<INetworkStatusListener>();
        listener.Online.Returns(true);
        listener.WaitForNetworkOnlineAsync(Arg.Any<CancellationToken>())
            .Throws(new Exception("We should not be waiting for the network status if we know it's online."));

        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem,
            NetworkStatusListener = listener
        };

        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);
        await transport.FlushAsync();

        // Assert
        var envelopes = innerTransport.GetSentEnvelopes();
        envelopes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TransportPausesWhenNetworkIsOffline()
    {
        // Arrange
        var waitingForNetwork = new TaskCompletionSource<bool>();
        var listener = Substitute.For<INetworkStatusListener>();
        listener.Online.Returns(false);
        listener.WaitForNetworkOnlineAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                waitingForNetwork.SetResult(true);
                return Task.Delay(Timeout.Infinite, callInfo.Arg<CancellationToken>());
            });

        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem,
            NetworkStatusListener = listener
        };

        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);
        using var cts = new CancellationTokenSource();
        try
        {
            _ = Task.Run(async () =>
            {
                // Wait for the caching transport to pause waiting for a network connection.
                // Then do the assertions while we're paused.
                await waitingForNetwork.Task;

                // Assert
                // ReSharper disable AccessToDisposedClosure
                var envelopes = innerTransport.GetSentEnvelopes();
                envelopes.Should().BeEmpty();
                cts.Cancel();
                // ReSharper restore AccessToDisposedClosure
            }, cts.Token);

            // Act
            using var envelope = Envelope.FromEvent(new SentryEvent());
            await transport.SendEnvelopeAsync(envelope, cts.Token);
            await transport.FlushAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // This is anticipated but caused by the test, so no need to assert it.
        }
    }

    [Fact]
    public async Task TransportResumesWhenNetworkComesBackOnline()
    {
        // Arrange
        var online = false;
        var listener = Substitute.For<INetworkStatusListener>();
        listener.Online.Returns(_ => online);
        listener.WaitForNetworkOnlineAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                online = true;
                return Task.CompletedTask;
            });

        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem,
            NetworkStatusListener = listener
        };

        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);
        await transport.FlushAsync();

        // Assert
        var envelopes = innerTransport.GetSentEnvelopes();
        envelopes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DoesntWriteSentAtHeaderToCacheFile()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory(_fileSystem);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = cacheDirectory.Path,
            FileSystem = _fileSystem
        };

        var innerTransport = Substitute.For<ITransport>();
        await using var transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        using var envelope = Envelope.FromEvent(new SentryEvent());

        // Act
        await transport.SendEnvelopeAsync(envelope);

        // Assert
        var filePath = _fileSystem
            .EnumerateFiles(cacheDirectory.Path, "*", SearchOption.AllDirectories)
            .Single();

        var contents = _fileSystem.ReadAllTextFromFile(filePath);
        Assert.DoesNotContain("sent_at", contents);
    }
}
