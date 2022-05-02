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

        var tcs = new TaskCompletionSource<bool>();
        var timeout = TimeSpan.FromSeconds(7);
        using var cts = new CancellationTokenSource(timeout);
        innerTransport.EnvelopeSent += (_, _) => tcs.SetResult(true);
        cts.Token.Register(() => tcs.TrySetCanceled());

        await using var transport = CachingTransport.Create(innerTransport, options);

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope, CancellationToken.None);
        await tcs.Task; // wait for the inner transport to signal that it sent the envelope

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
    public async Task DoesNotDeleteCacheIfHttpRequestException()
    {
        var exception = new HttpRequestException(null);
        await TestNetworkException(exception);
    }

    [Fact]
    public async Task DoesNotDeleteCacheIfIOException()
    {
        var exception = new IOException(null);
        await TestNetworkException(exception);
    }

    [Fact]
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
