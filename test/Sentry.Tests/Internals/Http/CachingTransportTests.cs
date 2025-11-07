using Sentry.Internal.Http;

namespace Sentry.Tests.Internals.Http;

public class CachingTransportTests : IDisposable
{
    private readonly TestOutputDiagnosticLogger _logger;
    private readonly TempDirectory _cacheDirectory;
    private readonly SentryOptions _options;

    public CachingTransportTests(ITestOutputHelper testOutputHelper)
    {
        _logger = Substitute.ForPartsOf<TestOutputDiagnosticLogger>(testOutputHelper);
        _cacheDirectory = new TempDirectory();
        _options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            CacheDirectoryPath = _cacheDirectory.Path,
            NetworkStatusListener = FakeReliableNetworkStatusListener.Instance
        };
    }

    public void Dispose()
    {
        _cacheDirectory.Dispose();
    }

    [Fact]
    public async Task WithAttachment()
    {
        // Arrange
        string httpContent = null;
        Exception exception = null;
        var innerTransport = new HttpTransport(_options, new HttpClient(new CallbackHttpClientHandler(async message =>
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

        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

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
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options);

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

        await using var transport = CachingTransport.Create(innerTransport, _options);
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
        var loggerCompletionSource = new TaskCompletionSource<object>();

        _logger
            .When(l =>
                l.Log(SentryLevel.Error,
                    "Exception in CachingTransport worker.",
                    Arg.Any<OperationCanceledException>(),
                    Arg.Any<object[]>()))
            .Do(_ => loggerCompletionSource.SetResult(null));

        var innerTransport = Substitute.For<ITransport>();

        var capturingCompletionSource = new TaskCompletionSource<object>();
        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(_ =>
            {
                capturingCompletionSource.SetResult(null);
                return new OperationCanceledException();
            });

        await using var transport = CachingTransport.Create(innerTransport, _options);
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
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

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
        _options.MaxCacheItems = 2;
        var innerTransport = Substitute.For<ITransport>();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

        // Act
        for (var i = 0; i < _options.MaxCacheItems + 2; i++)
        {
            using var envelope = Envelope.FromEvent(new SentryEvent());
            await transport.SendEnvelopeAsync(envelope);
        }

        // Assert
        transport.GetCacheLength().Should().BeLessOrEqualTo(_options.MaxCacheItems);
    }

    [Fact]
    public async Task AwareOfExistingFiles()
    {
        // Arrange
        // Send some envelopes with a failing transport to make sure they all stay in cache
        var initialInnerTransport = new FakeFailingTransport();
        await using var initialTransport = CachingTransport.Create(initialInnerTransport, _options, startWorker: false);

        for (var i = 0; i < 3; i++)
        {
            using var envelope = Envelope.FromEvent(new SentryEvent());
            await initialTransport.SendEnvelopeAsync(envelope);
        }

        // Move them all to processing and leave them there (due to FakeFailingTransport)
        await initialTransport.FlushAsync();
        await initialTransport.DisposeAsync();

        // Act

        // Creating the transport should move files from processing during initialization.
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

        // Flushing the worker will ensure all files are processed.
        await transport.FlushAsync();

        // Assert
        innerTransport.GetSentEnvelopes().Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_Malformed_Envelopes_Gracefully()
    {
        // Arrange
        var cacheDirectoryPath =
            _options.GetIsolatedCacheDirectoryPath() ??
            throw new InvalidOperationException("Cache directory or DSN is not set.");
        var processingDirectoryPath = Path.Combine(cacheDirectoryPath, "__processing");
        var fileName = $"{Guid.NewGuid()}.envelope";
        var filePath = Path.Combine(processingDirectoryPath, fileName);

        _options.FileSystem.CreateDirectory(processingDirectoryPath);   // Create the processing directory
        _options.FileSystem.CreateFileForWriting(filePath, out var file);
        file.Dispose(); // Make a malformed envelope... just an empty file
        _options.FileSystem.FileExists(filePath).Should().BeTrue();

        // Act
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);
        await transport.FlushAsync(); // Flush the worker to process

        // Assert
        _options.FileSystem.FileExists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task NonTransientExceptionShouldLog()
    {
        // Arrange
        var innerTransport = Substitute.For<ITransport>();

        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new Exception("The Message")));

        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

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
        var innerTransport = Substitute.For<ITransport>();
        var isFailing = true;

        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
                // ReSharper disable once AccessToModifiedClosure
                isFailing
                    ? Task.FromException(new InvalidOperationException())
                    : Task.CompletedTask);

        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

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
        _options.ClientReportRecorder = Substitute.For<IClientReportRecorder>();


        var innerTransport = Substitute.For<ITransport>();
        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException()));

        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);
        await transport.FlushAsync();

        // Test that we recorded the discarded event
        _options.ClientReportRecorder.Received(1)
            .RecordDiscardedEvent(DiscardReason.CacheOverflow, DataCategory.Error);
    }

    [Fact]
    public async Task RoundtripsClientReports()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var clock = new MockClock(timestamp);
        var recorder = new ClientReportRecorder(_options, clock);
        _options.ClientReportRecorder = recorder;

        var innerTransport = new FakeTransportWithRecorder(recorder);
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

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
        var recorder = (ClientReportRecorder)_options.ClientReportRecorder;
        var innerTransport = new FakeTransportWithRecorder(recorder);
        await using var transport = CachingTransport.Create(innerTransport, _options,
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
        // Arrange - network unavailable
        var pingHost = Substitute.For<IPing>();
        pingHost.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        _options.NetworkStatusListener = new PollingNetworkStatusListener(pingHost);

        var receivedException = new Exception();
        var innerTransport = Substitute.For<ITransport>();

        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(exception));

        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

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

        // Assert
        receivedException.Should().Be(exception);
        var files = _options.FileSystem.EnumerateFiles(_options.CacheDirectoryPath!, "*", SearchOption.AllDirectories).ToArray();
        files.Should().NotBeEmpty();

        // Arrange - network recovery
        innerTransport.ClearReceivedCalls();
        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.CompletedTask);

        // Act
        await transport.FlushAsync();

        // Assert
        receivedException.Should().Be(exception);
        files = _options.FileSystem.EnumerateFiles(_options.CacheDirectoryPath, "*", SearchOption.AllDirectories).ToArray();
        files.Should().NotContain(file => file.Contains("__processing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TransportSendsWithoutWaitingWhenNetworkIsOnline()
    {
        // Arrange
        var listener = Substitute.For<INetworkStatusListener>();
        listener.Online.Returns(true);
        listener.WaitForNetworkOnlineAsync(Arg.Any<CancellationToken>())
            .Throws(new Exception("We should not be waiting for the network status if we know it's online."));

        _options.NetworkStatusListener = listener;

        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

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

        _options.NetworkStatusListener = listener;

        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);
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

        _options.NetworkStatusListener = listener;

        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);
        await transport.FlushAsync();

        // Assert
        var envelopes = innerTransport.GetSentEnvelopes();
        envelopes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FlushAsync_RejectedByServer_DiscardsEnvelope()
    {
        // Arrange
        var listener = Substitute.For<INetworkStatusListener>();
        listener.Online.Returns(_ => true);

        _options.NetworkStatusListener = listener;
        _options.ClientReportRecorder = Substitute.For<IClientReportRecorder>();

        using var envelope = Envelope.FromEvent(new SentryEvent());

        var innerTransport = Substitute.For<ITransport>();
        innerTransport.SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new SocketException(32 /* Bad pipe exception */));
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

        // Act
        await transport.SendEnvelopeAsync(envelope);
        await transport.FlushAsync();

        // Assert
        foreach (var item in envelope.Items)
        {
            _options.ClientReportRecorder.Received(1)
                .RecordDiscardedEvent(DiscardReason.BufferOverflow, item.DataCategory);
        }
    }

    [Fact]
    public async Task DoesntWriteSentAtHeaderToCacheFile()
    {
        // Arrange
        _options.FileSystem = new FakeFileSystem(); // Keeps file write operations in memory instead of writing to disk

        var innerTransport = Substitute.For<ITransport>();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

        using var envelope = Envelope.FromEvent(new SentryEvent());

        // Act
        await transport.SendEnvelopeAsync(envelope);

        // Assert
        var isolatedCacheDir = _options.GetIsolatedCacheDirectoryPath();
        var filePath = _options.FileSystem
            .EnumerateFiles(isolatedCacheDir!, "*", SearchOption.AllDirectories)
            .Single();

        var contents = _options.FileSystem.ReadAllTextFromFile(filePath);
        Assert.DoesNotContain("sent_at", contents);
    }

    [Fact]
    public async Task SalvageAbandonedCacheSessions_MovesEnvelopesFromOtherIsolatedDirectories()
    {
        // Arrange
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

        var currentIsolated = _options.GetIsolatedCacheDirectoryPath()!; // already validated during creation
        var baseCacheDir = Directory.GetParent(currentIsolated)!.FullName;

        // Create two abandoned isolated cache directories with envelope files (including in nested folder)
        void CreateAbandonedDir(int index)
        {
            var abandoned = Path.Combine(baseCacheDir, $"isolated_abandoned_{index}");
            _options.FileSystem.CreateDirectory(abandoned);
            var nested = Path.Combine(abandoned, "nested");
            _options.FileSystem.CreateDirectory(nested);

            // root file
            var rootFile = Path.Combine(abandoned, $"root_{index}.envelope");
            _options.FileSystem.WriteAllTextToFile(rootFile, "dummy content");
            // nested file
            var nestedFile = Path.Combine(nested, $"nested_{index}.envelope");
            _options.FileSystem.WriteAllTextToFile(nestedFile, "dummy content");
        }

        CreateAbandonedDir(1);
        CreateAbandonedDir(2);

        // Act
        transport.SalvageAbandonedCacheSessions(CancellationToken.None);

        // Assert: All *.envelope files from abandoned dirs should now reside in the current isolated cache directory root
        var movedFiles = _options.FileSystem
            .EnumerateFiles(currentIsolated, "*.envelope", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToArray();

        movedFiles.Should().Contain(new[] { "root_1.envelope", "nested_1.envelope", "root_2.envelope", "nested_2.envelope" });

        // Ensure abandoned directories no longer contain those files
        var abandonedResidual = _options.FileSystem
            .EnumerateFiles(baseCacheDir, "*.envelope", SearchOption.AllDirectories)
            .Where(p => !p.StartsWith(currentIsolated, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        abandonedResidual.Should().BeEmpty();
    }

    [Fact]
    public async Task SalvageAbandonedCacheSessions_IgnoresCurrentDirectory()
    {
        // Arrange
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);
        var currentIsolated = _options.GetIsolatedCacheDirectoryPath()!;

        var currentFile = Path.Combine(currentIsolated, "current.envelope");
        if (_options.FileSystem.CreateFileForWriting(currentFile, out var stream))
        {
            await stream.DisposeAsync();
        }
        _options.FileSystem.FileExists(currentFile).Should().BeTrue();

        // Act
        transport.SalvageAbandonedCacheSessions(CancellationToken.None);

        // Assert: File created in current directory should remain untouched
        _options.FileSystem.FileExists(currentFile).Should().BeTrue();
    }

    [Fact]
    public async Task SalvageAbandonedCacheSessions_SkipsDirectoriesWithActiveLock()
    {
        // Arrange
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

        var currentIsolated = _options.GetIsolatedCacheDirectoryPath()!;
        var baseCacheDir = Directory.GetParent(currentIsolated)!.FullName;

        // Create an abandoned directory that matches the search pattern and acquire its lock
        var lockedDir = Path.Combine(baseCacheDir, "isolated_locked");
        _options.FileSystem.CreateDirectory(lockedDir);
        var lockedEnvelope = Path.Combine(lockedDir, "locked.envelope");
        if (_options.FileSystem.CreateFileForWriting(lockedEnvelope, out var stream))
        {
            stream.Dispose();
        }
        // Acquire the lock so salvage can't take it
        using (var coordinator = new CacheDirectoryCoordinator(lockedDir, _options.DiagnosticLogger, _options.FileSystem))
        {
            var acquired = coordinator.TryAcquire();
            acquired.Should().BeTrue("test must hold the lock to validate skip behavior");

            // Act
            transport.SalvageAbandonedCacheSessions(CancellationToken.None);

            // Assert: File should still be in locked abandoned directory and not in current directory
            _options.FileSystem.FileExists(lockedEnvelope).Should().BeTrue();
            var moved = _options.FileSystem
                .EnumerateFiles(currentIsolated, "locked.envelope", SearchOption.TopDirectoryOnly)
                .Any();
            moved.Should().BeFalse();
        }
    }

    [Fact]
    public async Task MigrateVersion5Cache_MovesEnvelopesFromBaseAndProcessing()
    {
        // Arrange
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

        var isolatedCacheDir = _options.GetIsolatedCacheDirectoryPath()!;

        var baseCacheDir = _options.GetBaseCacheDirectoryPath()!;
        var rootFile = Path.Combine(baseCacheDir, "v5_root.envelope");
        _options.FileSystem.CreateDirectory(baseCacheDir);
        _options.FileSystem.WriteAllTextToFile(rootFile, "dummy content");

        var processingDir = Path.Combine(baseCacheDir, "__processing");
        var procFile = Path.Combine(processingDir, "v5_proc.envelope");
        _options.FileSystem.CreateDirectory(processingDir);
        _options.FileSystem.WriteAllTextToFile(procFile, "dummy content");

        // Act
        transport.MigrateVersion5Cache(CancellationToken.None);

        // Assert
        var markerFile = Path.Combine(baseCacheDir, ".migrated");
        _options.FileSystem.FileExists(markerFile).Should().BeTrue();
        _options.FileSystem.FileExists(rootFile).Should().BeFalse();
        _options.FileSystem.FileExists(procFile).Should().BeFalse();

        var movedRoot = Path.Combine(isolatedCacheDir, "v5_root.envelope");
        var movedProc = Path.Combine(isolatedCacheDir, "v5_proc.envelope");
        _options.FileSystem.FileExists(movedRoot).Should().BeTrue();
        _options.FileSystem.FileExists(movedProc).Should().BeTrue();
     }

    [Fact]
    public async Task MigrateVersion5Cache_AlreadyMigrated_Skipped()
    {
        // Arrange
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

        var baseCacheDir = _options.GetBaseCacheDirectoryPath()!;
        var rootFile = Path.Combine(baseCacheDir, "v5_root.envelope");
        _options.FileSystem.CreateDirectory(baseCacheDir);
        _options.FileSystem.WriteAllTextToFile(rootFile, "dummy content");

        var processingDir = Path.Combine(baseCacheDir, "__processing");
        var procFile = Path.Combine(processingDir, "v5_proc.envelope");
        _options.FileSystem.CreateDirectory(processingDir);
        _options.FileSystem.WriteAllTextToFile(procFile, "dummy content");

        var marker = Path.Combine(baseCacheDir, ".migrated");
        _options.FileSystem.WriteAllTextToFile(marker, "6.0.0");

        // Act
        var result = transport.MigrateVersion5Cache(CancellationToken.None);

        // Assert
        result.Should().Be(CachingTransport.ResultMigrationAlreadyMigrated);
        _options.FileSystem.FileExists(rootFile).Should().BeTrue();
        _options.FileSystem.FileExists(procFile).Should().BeTrue();
    }

    [Fact]
    public async Task MigrateVersion5Cache_MigrationLockHeld_Skipped()
    {
        // Arrange
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

        var baseCacheDir = _options.GetBaseCacheDirectoryPath()!;
        var rootFile = Path.Combine(baseCacheDir, "v5_root.envelope");
        _options.FileSystem.CreateDirectory(baseCacheDir);
        _options.FileSystem.WriteAllTextToFile(rootFile, "dummy content");

        var processingDir = Path.Combine(baseCacheDir, "__processing");
        var procFile = Path.Combine(processingDir, "v5_proc.envelope");
        _options.FileSystem.CreateDirectory(processingDir);
        _options.FileSystem.WriteAllTextToFile(procFile, "dummy content");

        var migrationLockPath = Path.Combine(baseCacheDir, "migration");
        using var coordinator = new CacheDirectoryCoordinator(migrationLockPath, _options.DiagnosticLogger, _options.FileSystem);
        coordinator.TryAcquire().Should().BeTrue("test must hold the migration lock");

        // Act
        var result = transport.MigrateVersion5Cache(CancellationToken.None);

        // Assert
        result.Should().Be(CachingTransport.ResultMigrationLockNotAcquired);
        _options.FileSystem.FileExists(rootFile).Should().BeTrue();
        _options.FileSystem.FileExists(procFile).Should().BeTrue();
    }

    [Fact]
    public async Task MigrateVersion5Cache_Cancelled_Skipped()
    {
        // Arrange
        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, _options, startWorker: false);

        var baseCacheDir = _options.GetBaseCacheDirectoryPath()!;
        var rootFile = Path.Combine(baseCacheDir, "v5_root.envelope");
        _options.FileSystem.CreateDirectory(baseCacheDir);
        _options.FileSystem.WriteAllTextToFile(rootFile, "dummy content");

        var processingDir = Path.Combine(baseCacheDir, "__processing");
        var procFile = Path.Combine(processingDir, "v5_proc.envelope");
        _options.FileSystem.CreateDirectory(processingDir);
        _options.FileSystem.WriteAllTextToFile(procFile, "dummy content");

        // Act
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var result = transport.MigrateVersion5Cache(cts.Token);

        // Assert
        result.Should().Be(CachingTransport.ResultMigrationCancelled);
        _options.FileSystem.FileExists(rootFile).Should().BeTrue();
        _options.FileSystem.FileExists(procFile).Should().BeTrue();
    }
}
