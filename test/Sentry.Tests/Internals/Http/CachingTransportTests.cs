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

    [Fact(Timeout = 700000)]
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

        var innerTransport = new HttpTransport(options,new HttpClient(new CallbackHttpClientHandler(message =>
        {
            message.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        })));
        await using var transport = new CachingTransport(innerTransport, options);

        var tempFile = Path.GetTempFileName();
        
        try
        {
            var attachment = new Attachment(AttachmentType.Default, new FileAttachmentContent(tempFile), "Attachment.txt", null);
            using var envelope = Envelope.FromEvent(new SentryEvent(), attachments: new[] {attachment});

            // Act
            await transport.SendEnvelopeAsync(envelope);

            // Wait until directory is empty
            while (Directory.EnumerateFiles(cacheDirectory.Path, "*", SearchOption.AllDirectories).Any())
            {
                await Task.Delay(100);
            }

            // Assert
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
            CacheDirectoryPath = cacheDirectory.Path
        };

        using var innerTransport = new FakeTransport();
        await using var transport = new CachingTransport(innerTransport, options);

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);

        // Wait until directory is empty
        while (Directory.EnumerateFiles(cacheDirectory.Path, "*", SearchOption.AllDirectories).Any())
        {
            await Task.Delay(100);
        }

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

        var capturingSync = new ManualResetEventSlim();
        var cancelingSync = new ManualResetEventSlim();
        var innerTransport = Substitute.For<ITransport>();

        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(_ =>
            {
                capturingSync.Set();
                cancelingSync.Wait(TimeSpan.FromSeconds(4));
                return new OperationCanceledException();
            });

        await using var transport = new CachingTransport(innerTransport, options);
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);

        Assert.True(capturingSync.Wait(TimeSpan.FromSeconds(3)), "Inner transport was never called");
        var stopTask = transport.StopWorkerAsync();
        cancelingSync.Set(); // Unblock the worker
        await stopTask;

        // Assert
        Assert.False(_logger.HasErrorOrFatal, "Error or fatal message logged");
    }

    [Fact]
    public async Task ShouldLogOperationCanceledExceptionWhenNotIsCancellationRequested()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var loggerSync = new ManualResetEventSlim();

        var logger = Substitute.For<IDiagnosticLogger>();
        logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
        logger
            .When(l =>
                l.Log(SentryLevel.Error,
                    "Exception in background worker of CachingTransport.",
                    Arg.Any<OperationCanceledException>(),
                    Arg.Any<object[]>()))
            .Do(_ => loggerSync.Set());

        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = logger,
            CacheDirectoryPath = cacheDirectory.Path,
            Debug = true
        };

        var innerTransport = Substitute.For<ITransport>();

        var capturingSync = new ManualResetEventSlim();
        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(_ =>
            {
                capturingSync.Set();
                return new OperationCanceledException();
            });

        await using var transport = new CachingTransport(innerTransport, options);
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);

        // Assert
        Assert.True(capturingSync.Wait(TimeSpan.FromSeconds(3)), "Envelope never reached the transport");
        Assert.True(loggerSync.Wait(TimeSpan.FromSeconds(3)), "Expected log call never received");
    }

    [Fact(Timeout = 7000)]
    public async Task EnvelopeReachesInnerTransport()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLogger = _logger,
            CacheDirectoryPath = cacheDirectory.Path
        };

        using var innerTransport = new FakeTransport();
        await using var transport = new CachingTransport(innerTransport, options);

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent());
        await transport.SendEnvelopeAsync(envelope);

        while (!innerTransport.GetSentEnvelopes().Any())
        {
            await Task.Delay(100);
        }

        // Assert
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
            CacheDirectoryPath = cacheDirectory.Path,
            MaxCacheItems = 2
        };

        var innerTransport = Substitute.For<ITransport>();

        var evt = new ManualResetEventSlim();
        // Block until we're done
        innerTransport
            .When(t => t.SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>()))
            .Do(_ => evt.Wait());

        await using var transport = new CachingTransport(innerTransport, options);

        // Act & assert
        for (var i = 0; i < options.MaxCacheItems + 2; i++)
        {
            using var envelope = Envelope.FromEvent(new SentryEvent());
            await transport.SendEnvelopeAsync(envelope);

            transport.GetCacheLength().Should().BeLessOrEqualTo(options.MaxCacheItems);
        }
        evt.Set();
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
            CacheDirectoryPath = cacheDirectory.Path
        };

        // Send some envelopes with a failing transport to make sure they all stay in cache
        {
            using var initialInnerTransport = new FakeTransport();
            await using var initialTransport = new CachingTransport(initialInnerTransport, options);

            // Shutdown the worker immediately so nothing gets processed
            await initialTransport.StopWorkerAsync();

            for (var i = 0; i < 3; i++)
            {
                using var envelope = Envelope.FromEvent(new SentryEvent());
                await initialTransport.SendEnvelopeAsync(envelope);
            }
        }

        using var innerTransport = new FakeTransport();
        await using var transport = new CachingTransport(innerTransport, options);

        // Act

        // Wait until directory is empty
        while (Directory.EnumerateFiles(cacheDirectory.Path, "*", SearchOption.AllDirectories).Any())
        {
            await Task.Delay(100);
        }

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

        await using var transport = new CachingTransport(innerTransport, options);

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

        await using var transport = new CachingTransport(innerTransport, options);

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
            CacheDirectoryPath = cacheDirectory.Path
        };

        var receivedException = new Exception();
        var innerTransport = Substitute.For<ITransport>();

        innerTransport
            .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(exception));

        await using var transport = new CachingTransport(innerTransport, options);

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
}
