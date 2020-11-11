using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Http;
using Sentry.Protocol.Envelopes;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals.Http
{
    public class CachingTransportTests
    {
        private string CacheDirectoryPath { get; } = Path.Combine(
            Directory.GetCurrentDirectory(),
            $"EnvelopeCache_{Guid.NewGuid()}"
        );

        [Fact(Timeout = 10000)]
        public async Task WorksInBackground()
        {
            // Arrange
            var options = new SentryOptions {CacheDirectoryPath = CacheDirectoryPath};

            using var innerTransport = new FakeTransport();
            using var transport = new CachingTransport(innerTransport, options);

            // Act
            using var envelope = Envelope.FromEvent(new SentryEvent());
            await transport.SendEnvelopeAsync(envelope);

            // Wait until directory is empty
            while (
                Directory.Exists(CacheDirectoryPath) &&
                Directory.EnumerateFiles(CacheDirectoryPath).Any())
            {
                await Task.Delay(100);
            }

            // Assert
            var sentEnvelope = innerTransport.GetSentEnvelopes().Single();
            sentEnvelope.Should().BeEquivalentTo(envelope, o => o.Excluding(x => x.Items[0].Header));
        }

        [Fact(Timeout = 10000)]
        public async Task EnvelopeReachesInnerTransport()
        {
            // Arrange
            var options = new SentryOptions {CacheDirectoryPath = CacheDirectoryPath};

            using var innerTransport = new FakeTransport();
            using var transport = new CachingTransport(innerTransport, options);

            // Act
            using var envelope = Envelope.FromEvent(new SentryEvent());
            await transport.SendEnvelopeAsync(envelope);
            await transport.FlushAsync();

            // Assert
            var sentEnvelope = innerTransport.GetSentEnvelopes().Single();
            sentEnvelope.Should().BeEquivalentTo(envelope, o => o.Excluding(x => x.Items[0].Header));
        }

        [Fact(Timeout = 10000)]
        public async Task MaintainsLimit()
        {
            // Arrange
            var options = new SentryOptions
            {
                CacheDirectoryPath = CacheDirectoryPath,
                MaxQueueItems = 3
            };

            var innerTransport = Substitute.For<ITransport>();
            using var signal = new ManualResetEventSlim(false);

            // Transport is blocked until the signal is released
            innerTransport
                .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    signal.Wait();
                    return default;
                });

            using var transport = new CachingTransport(innerTransport, options);

            // Act
            var sendTasks = Enumerable
                .Range(0, 20)
                .Select(async _ =>
                {
                    using var envelope = Envelope.FromEvent(new SentryEvent());
                    await transport.SendEnvelopeAsync(envelope);
                });

            // Unblock the inner transport
            signal.Set();
            await Task.WhenAll(sendTasks);

            await transport.FlushAsync();

            // Assert
            // (only 3 envelopes were sent, rest were throttled)
            _ = innerTransport.Received(3).SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>());
        }

        [Fact(Timeout = 10000)]
        public async Task AwareOfExistingFiles()
        {
            // Arrange
            var options = new SentryOptions {CacheDirectoryPath = CacheDirectoryPath};

            // Send some envelopes with a failing transport to make sure they all stay in cache
            {
                var initialInnerTransport = new FakeFailingTransport();
                using var initialTransport = new CachingTransport(initialInnerTransport, options);

                for (var i = 0; i < 3; i++)
                {
                    using var envelope = Envelope.FromEvent(new SentryEvent());
                    await initialTransport.SendEnvelopeAsync(envelope);
                }
            }

            using var innerTransport = new FakeTransport();
            using var transport = new CachingTransport(innerTransport, options);

            // Act
            await transport.FlushAsync();

            // Assert
            innerTransport.GetSentEnvelopes().Should().HaveCount(3);
        }

        [Fact(Timeout = 10000)]
        public async Task RetriesOnTransientExceptions()
        {
            // Arrange
            var options = new SentryOptions {CacheDirectoryPath = CacheDirectoryPath};

            var innerTransport = Substitute.For<ITransport>();
            var isFailing = true;

            innerTransport
                .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                    isFailing
                        ? new ValueTask(Task.FromException(new IOException()))
                        : new ValueTask()
                );

            using var transport = new CachingTransport(innerTransport, options);

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
            // (3 envelope retried)
            _ = innerTransport.Received(3).SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>());
        }

        [Fact(Timeout = 10000)]
        public async Task DoesNotRetryOnNonTransientExceptions()
        {
            // Arrange
            var options = new SentryOptions {CacheDirectoryPath = CacheDirectoryPath};

            var innerTransport = Substitute.For<ITransport>();
            var isFailing = true;

            innerTransport
                .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                    isFailing
                        ? new ValueTask(Task.FromException(new HttpRequestException()))
                        : new ValueTask()
                );

            using var transport = new CachingTransport(innerTransport, options);

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
    }
}
