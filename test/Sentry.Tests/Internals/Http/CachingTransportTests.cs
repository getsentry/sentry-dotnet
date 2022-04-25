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

    [Fact(Timeout = 7000)]
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
        await using var transport = CachingTransport.Create(innerTransport, options);

        var tempFile = Path.GetTempFileName();

        try
        {
            var attachment = new Attachment(AttachmentType.Default, new FileAttachmentContent(tempFile), "Attachment.txt", null);
            using var envelope = Envelope.FromEvent(new SentryEvent(), attachments: new[] { attachment });

            // Act
            await transport.SendEnvelopeAsync(envelope);
            await WaitForDirectoryToBecomeEmptyAsync(cacheDirectory.Path);

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

    [Fact(Timeout = 7000)]
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

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);
        await WaitForDirectoryToBecomeEmptyAsync(cacheDirectory.Path);

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
            .ThrowsForAnyArgs(_ =>
            {
                capturingCompletionSource.SetResult(null);
                cancelingCompletionSource.Task.Wait(TimeSpan.FromSeconds(4));
                return new OperationCanceledException();
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
        var timeout = TimeSpan.FromSeconds(7);

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

        var tcs = new TaskCompletionSource<bool>();
        using var cts = new CancellationTokenSource(timeout);
        innerTransport.EnvelopeSent += (_, _) => tcs.SetResult(true);
        cts.Token.Register(() => tcs.TrySetCanceled());

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope, cts.Token);
        var completed = await tcs.Task;

        // Assert
        Assert.True(completed, "The task timed out!");
        var sentEnvelope = innerTransport.GetSentEnvelopes().Single();
        sentEnvelope.Should().BeEquivalentTo(envelope, o => o.Excluding(x => x.Items[0].Header));
    }

    [Fact(Timeout = 5000)]
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

        var tcs = new TaskCompletionSource<object>();

        // Block until we're done
        innerTransport
            .When(t => t.SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>()))
            .Do(_ => tcs.Task.Wait());

        await using var transport = CachingTransport.Create(innerTransport, options);

        // Act & assert
        for (var i = 0; i < options.MaxCacheItems + 2; i++)
        {
            using var envelope = Envelope.FromEvent(new SentryEvent());
            await transport.SendEnvelopeAsync(envelope);

            transport.GetCacheLength().Should().BeLessOrEqualTo(options.MaxCacheItems);
        }
        tcs.SetResult(null);
    }

    [Fact(Timeout = 7000)]
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
        {
            using var initialInnerTransport = new FakeTransport();
            await using var initialTransport = CachingTransport.Create(initialInnerTransport, options);

            // Shutdown the worker immediately so nothing gets processed
            await initialTransport.StopWorkerAsync();

            for (var i = 0; i < 3; i++)
            {
                using var envelope = Envelope.FromEvent(new SentryEvent());
                await initialTransport.SendEnvelopeAsync(envelope);
            }
        }

        using var innerTransport = new FakeTransport();
        await using var transport = CachingTransport.Create(innerTransport, options);

        // Act
        await WaitForDirectoryToBecomeEmptyAsync(cacheDirectory.Path);

        // Assert
        innerTransport.GetSentEnvelopes().Should().HaveCount(3);
    }

    [Fact(Timeout = 7000)]
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

        await using var transport = CachingTransport.Create(innerTransport, options);

        // Can't really reliably test this with a worker
        await transport.StopWorkerAsync();

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

    [Fact(Timeout = 7000)]
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
                isFailing
                    ? Task.FromException(new InvalidOperationException())
                    : Task.CompletedTask);

        await using var transport = CachingTransport.Create(innerTransport, options);

        // Can't really reliably test this with a worker
        await transport.StopWorkerAsync();

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

    [Fact(Timeout = 7000)]
    public async Task DoesNotDeleteCacheIfHttpRequestException()
    {
        var exception = new HttpRequestException(null);
        await TestNetworkException(exception);
    }

    [Fact(Timeout = 7000)]
    public async Task DoesNotDeleteCacheIfIOException()
    {
        var exception = new IOException(null);
        await TestNetworkException(exception);
    }

    [Fact(Timeout = 7000)]
    public async Task DoesNotDeleteCacheIfSocketException()
    {
        var exception = new SocketException();
        await TestNetworkException(exception);
    }

    private async Task TestNetworkException(Exception exception)
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

        await using var transport = CachingTransport.Create(innerTransport, options);

        // Can't really reliably test this with a worker
        await transport.StopWorkerAsync();

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

    private async Task WaitForDirectoryToBecomeEmptyAsync(string directoryPath, TimeSpan? timeout = null)
    {
        bool DirectoryIsEmpty() => !Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).Any();

        if (DirectoryIsEmpty())
        {
            // No point in waiting if the directory is already empty
            return;
        }

        var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(7));

        using var watcher = new FileSystemWatcher(directoryPath);
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;

        // Wait until timeout or directory is empty
        while (!DirectoryIsEmpty())
        {
            cts.Token.ThrowIfCancellationRequested();

            var tcs = new TaskCompletionSource<bool>();
            cts.Token.Register(() => tcs.TrySetCanceled());
            watcher.Deleted += (_, _) => tcs.SetResult(true);

            // One final check before waiting
            if (DirectoryIsEmpty())
            {
                return;
            }

            // Wait for a file to be deleted
            await tcs.Task;
        }
    }
}
