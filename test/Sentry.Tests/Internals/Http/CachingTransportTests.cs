using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal.Http;
using Sentry.Protocol.Envelopes;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals.Http
{
    public class CachingTransportTests
    {
        [Fact(Timeout = 7000)]
        public async Task WorksInBackground()
        {
            // Arrange
            using var cacheDirectory = new TempDirectory();
            var options = new SentryOptions {CacheDirectoryPath = cacheDirectory.Path};

            using var innerTransport = new FakeTransport();
            await using var transport = new CachingTransport(innerTransport, options);

            // Act
            using var envelope = Envelope.FromEvent(new SentryEvent());
            await transport.SendEnvelopeAsync(envelope);

            // Wait until directory is empty
            while (
                Directory.Exists(cacheDirectory.Path) &&
                Directory.EnumerateFiles(cacheDirectory.Path, "*", SearchOption.AllDirectories).Any())
            {
                await Task.Delay(100);
            }

            // Assert
            var sentEnvelope = innerTransport.GetSentEnvelopes().Single();
            sentEnvelope.Should().BeEquivalentTo(envelope, o => o.Excluding(x => x.Items[0].Header));
        }

        [Fact(Timeout = 7000)]
        public async Task EnvelopeReachesInnerTransport()
        {
            // Arrange
            using var cacheDirectory = new TempDirectory();
            var options = new SentryOptions {CacheDirectoryPath = cacheDirectory.Path};

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

        [Fact(Timeout = 7000)]
        public async Task MaintainsLimit()
        {
            // Arrange
            using var cacheDirectory = new TempDirectory();
            var options = new SentryOptions
            {
                CacheDirectoryPath = cacheDirectory.Path,
                MaxQueueItems = 3
            };

            var innerTransport = Substitute.For<ITransport>();

            // Introduce enough delay for the cache to overflow under normal circumstances
            innerTransport
                .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask(Task.Delay(3000)));

            await using var transport = new CachingTransport(innerTransport, options);

            // Act & assert
            for (var i = 0; i < 20; i++)
            {
                using var envelope = Envelope.FromEvent(new SentryEvent());
                await transport.SendEnvelopeAsync(envelope);

                transport.GetCacheLength().Should().BeLessOrEqualTo(options.MaxQueueItems);
            }
        }

        [Fact(Timeout = 7000)]
        public async Task AwareOfExistingFiles()
        {
            // Arrange
            using var cacheDirectory = new TempDirectory();
            var options = new SentryOptions {CacheDirectoryPath = cacheDirectory.Path};

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
            while (
                Directory.Exists(cacheDirectory.Path) &&
                Directory.EnumerateFiles(cacheDirectory.Path, "*", SearchOption.AllDirectories).Any())
            {
                await Task.Delay(100);
            }

            // Assert
            innerTransport.GetSentEnvelopes().Should().HaveCount(3);
        }

        [Fact(Timeout = 7000)]
        public async Task DoesNotRetryOnNonTransientExceptions()
        {
            // Arrange
            using var cacheDirectory = new TempDirectory();
            var options = new SentryOptions {CacheDirectoryPath = cacheDirectory.Path};

            var innerTransport = Substitute.For<ITransport>();
            var isFailing = true;

            innerTransport
                .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                    isFailing
                        ? new ValueTask(Task.FromException(new InvalidOperationException()))
                        : new ValueTask()
                );

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
        public async Task DoesNotDeleteCacheIfConnectionWithIssue()
        {
            // Arrange
            using var cacheDirectory = new TempDirectory();
            var options = new SentryOptions { CacheDirectoryPath = cacheDirectory.Path };

            var exception = new HttpRequestException(null, new SocketException());
            Exception receivedException = new Exception();
            var innerTransport = Substitute.For<ITransport>();

            innerTransport
                .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
                .Returns(_ =>  new ValueTask(Task.FromException(exception)));

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
}
