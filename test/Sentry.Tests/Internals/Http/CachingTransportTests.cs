using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
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

        [Fact]
        public async Task EnvelopeGetsSent()
        {
            // Arrange
            var options = new SentryOptions
            {
                CacheDirectoryPath = CacheDirectoryPath
            };

            using var innerTransport = new FakeTransport();
            using var transport = new CachingTransport(innerTransport, options);

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            await transport.SendEnvelopeAsync(envelope);
            await transport.FlushAsync();

            // Assert
            var sentEnvelope = innerTransport.GetSentEnvelopes().Single();
            sentEnvelope.Should().BeEquivalentTo(envelope, o => o.Excluding(x => x.Items[0].Header));
        }
    }
}
