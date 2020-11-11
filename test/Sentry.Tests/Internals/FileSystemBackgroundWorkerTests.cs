using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol.Envelopes;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class FileSystemBackgroundWorkerTests : IDisposable
    {
        private string CacheDirectoryPath { get; } = Path.Combine(
            Directory.GetCurrentDirectory(),
            $"EnvelopeCache_{Guid.NewGuid()}"
        );

        [Fact]
        public async Task EnvelopesGetSent()
        {
            // Arrange
            using var transport = new FakeTransport();

            using var worker = new FileSystemBackgroundWorker(transport, new SentryOptions
            {
                CacheDirectoryPath = CacheDirectoryPath
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            worker.EnqueueEnvelope(envelope);
            worker.EnqueueEnvelope(envelope);
            worker.EnqueueEnvelope(envelope);

            await worker.FlushAsync(TimeSpan.FromSeconds(5));

            // Assert
            transport.GetSentEnvelopes().Should().AllBeEquivalentTo(envelope, o => o.Excluding(x => x.Items[0].Header));
        }

        [Fact]
        public void QueueDoesNotOverflow()
        {
            // Arrange
            using var transport = new FakeTransport();

            using var worker = new FileSystemBackgroundWorker(transport, new SentryOptions
            {
                CacheDirectoryPath = CacheDirectoryPath,
                MaxQueueItems = 5
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act & assert
            for (var i = 0; i < 20; i++)
            {
                worker.EnqueueEnvelope(envelope);
                worker.QueueLength.Should().BeLessOrEqualTo(5);
            }
        }

        [Fact]
        public async Task RetriesOnTransientErrors()
        {
            // Arrange
            var transport = Substitute.For<ITransport>();

            transport
                .SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
                .Returns(
                    new ValueTask(Task.FromException(new IOException())),
                    new ValueTask()
                );

            using var worker = new FileSystemBackgroundWorker(transport, new SentryOptions
            {
                CacheDirectoryPath = CacheDirectoryPath
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            worker.EnqueueEnvelope(envelope);
            await worker.FlushAsync(TimeSpan.FromSeconds(5));

            worker.EnqueueEnvelope(envelope);
            await worker.FlushAsync(TimeSpan.FromSeconds(5));

            // Assert
            _ = transport.Received(3).SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CarriesOverExistingCache()
        {
            // Arrange
            {
                var previousTransport = new FakeFailingTransport();

                using var previousWorker = new FileSystemBackgroundWorker(previousTransport, new SentryOptions
                {
                    CacheDirectoryPath = CacheDirectoryPath,
                    ShutdownTimeout = TimeSpan.Zero
                });

                using var previousEnvelope = Envelope.FromEvent(new SentryEvent());

                previousWorker.EnqueueEnvelope(previousEnvelope);
                previousWorker.EnqueueEnvelope(previousEnvelope);
                previousWorker.EnqueueEnvelope(previousEnvelope);
                previousWorker.Shutdown();
            }

            using var transport = new FakeTransport();

            using var worker = new FileSystemBackgroundWorker(transport, new SentryOptions
            {
                CacheDirectoryPath = CacheDirectoryPath
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            worker.EnqueueEnvelope(envelope);
            await worker.FlushAsync(TimeSpan.FromSeconds(5));

            // Assert
            transport.GetSentEnvelopes().Should().HaveCount(4);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(CacheDirectoryPath, true);
            }
            catch
            {
                // Ignore
            }
        }
    }
}
