using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Sentry.Protocol;
using Sentry.Protocol.Batching;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class EnvelopeTests
    {
        // Tests driven by examples from docs:
        // https://develop.sentry.dev/sdk/envelopes/#full-examples

        [Fact]
        public async Task Serialization_EnvelopeWithoutItems_Success()
        {
            // Arrange
            using var envelope = new Envelope(
                new Dictionary<string, object> {["event_id"] = "12c2d058d58442709aa2eca08bf20986"},
                Array.Empty<EnvelopeItem>()
            );

            // Act
            var output = await envelope.SerializeToStringAsync();

            // Assert
            output.Should().Be(
                "{\"event_id\":\"12c2d058d58442709aa2eca08bf20986\"}\n"
            );
        }

        [Fact]
        public async Task Deserialization_EnvelopeWithoutItems_Success()
        {
            // Arrange
            using var input = "{\"event_id\":\"12c2d058d58442709aa2eca08bf20986\"}\n".ToMemoryStream();

            using var expectedEnvelope = new Envelope(
                new Dictionary<string, object> {["event_id"] = "12c2d058d58442709aa2eca08bf20986"},
                Array.Empty<EnvelopeItem>()
            );

            // Act
            using var envelope = await Envelope.DeserializeAsync(input);

            // Assert
            envelope.Should().BeEquivalentTo(expectedEnvelope);
        }

        [Fact]
        public async Task Serialization_EnvelopeWithoutHeader_Success()
        {
            // Arrange
            using var envelope = new Envelope(
                new Dictionary<string, object>(),
                new[]
                {
                    new EnvelopeItem(
                        new Dictionary<string, object>{["type"] = "session"},
                        new StreamSerializable("{\"started\": \"2020-02-07T14:16:00Z\",\"attrs\":{\"release\":\"sentry-test@1.0.0\"}}"
                            .ToMemoryStream())
                    )
                }
            );

            // Act
            var output = await envelope.SerializeToStringAsync();

            // Assert
            output.Should().Be(
                "{}\n" +
                "{\"type\":\"session\",\"length\":75}\n" +
                "{\"started\": \"2020-02-07T14:16:00Z\",\"attrs\":{\"release\":\"sentry-test@1.0.0\"}}\n"
            );
        }

        [Fact]
        public async Task Deserialization_EnvelopeWithoutHeader_Success()
        {
            // Arrange
            using var input = (
                "{}\n" +
                "{\"type\":\"session\",\"length\":75}\n" +
                "{\"started\": \"2020-02-07T14:16:00Z\",\"attrs\":{\"release\":\"sentry-test@1.0.0\"}}\n"
            ).ToMemoryStream();

            using var expectedEnvelope = new Envelope(
                new Dictionary<string, object>(),
                new[]
                {
                    new EnvelopeItem(
                        new Dictionary<string, object>{["type"] = "session", ["length"] = 75L},
                        new StreamSerializable("{\"started\": \"2020-02-07T14:16:00Z\",\"attrs\":{\"release\":\"sentry-test@1.0.0\"}}"
                            .ToMemoryStream())
                    )
                }
            );

            // Act
            using var envelope = await Envelope.DeserializeAsync(input);

            // Assert
            envelope.Should().BeEquivalentTo(expectedEnvelope);
        }

        [Fact]
        public async Task Serialization_EnvelopeWithTwoItems_Success()
        {
            // Arrange
            using var envelope = new Envelope(
                new Dictionary<string, object>
                {
                    ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc",
                    ["dsn"] = "https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42"
                },
                new[]
                {
                    new EnvelopeItem(
                        new Dictionary<string, object>
                        {
                            ["type"] = "attachment",
                            ["length"] = 13,
                            ["content_type"] = "text/plain",
                            ["filename"] = "hello.txt"
                        },
                        new StreamSerializable("\xef\xbb\xbfHello\r\n".ToMemoryStream())
                    ),

                    new EnvelopeItem(
                        new Dictionary<string, object>
                        {
                            ["type"] = "event",
                            ["length"] = 41,
                            ["content_type"] = "application/json",
                            ["filename"] = "application.log"
                        },
                        new StreamSerializable("{\"message\":\"hello world\",\"level\":\"error\"}".ToMemoryStream())
                    )
                }
            );

            // Act
            var output = await envelope.SerializeToStringAsync();

            // Assert
            output.Should().Be(
                "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\",\"dsn\":\"https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42\"}\n" +
                "{\"type\":\"attachment\",\"length\":13,\"content_type\":\"text/plain\",\"filename\":\"hello.txt\"}\n" +
                "\xef\xbb\xbfHello\r\n\n" +
                "{\"type\":\"event\",\"length\":41,\"content_type\":\"application/json\",\"filename\":\"application.log\"}\n" +
                "{\"message\":\"hello world\",\"level\":\"error\"}\n"
            );
        }

        [Fact]
        public async Task Deserialization_EnvelopeWithTwoItems_Success()
        {
            // Arrange
            using var input = (
                "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\",\"dsn\":\"https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42\"}\n" +
                "{\"type\":\"attachment\",\"length\":13,\"content_type\":\"text/plain\",\"filename\":\"hello.txt\"}\n" +
                "\xef\xbb\xbfHello\r\n\n" +
                "{\"type\":\"event\",\"length\":41,\"content_type\":\"application/json\",\"filename\":\"application.log\"}\n" +
                "{\"message\":\"hello world\",\"level\":\"error\"}\n"
            ).ToMemoryStream();

            using var expectedEnvelope = new Envelope(
                new Dictionary<string, object>
                {
                    ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc",
                    ["dsn"] = "https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42"
                },
                new[]
                {
                    new EnvelopeItem(
                        new Dictionary<string, object>
                        {
                            ["type"] = "attachment",
                            ["length"] = 13L,
                            ["content_type"] = "text/plain",
                            ["filename"] = "hello.txt"
                        },
                        new StreamSerializable("\xef\xbb\xbfHello\r\n".ToMemoryStream())
                    ),

                    new EnvelopeItem(
                        new Dictionary<string, object>
                        {
                            ["type"] = "event",
                            ["length"] = 41L,
                            ["content_type"] = "application/json",
                            ["filename"] = "application.log"
                        },
                        new StreamSerializable("{\"message\":\"hello world\",\"level\":\"error\"}".ToMemoryStream())
                    )
                }
            );

            // Act
            using var envelope = await Envelope.DeserializeAsync(input);

            // Assert
            envelope.Should().BeEquivalentTo(expectedEnvelope);
        }

        [Fact]
        public async Task Serialization_EnvelopeWithTwoEmptyItems_Success()
        {
            // Arrange
            using var envelope = new Envelope(
                new Dictionary<string, object> {["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc"},
                new[]
                {
                    new EnvelopeItem(
                        new Dictionary<string, object>
                        {
                            ["type"] = "attachment",
                            ["length"] = 0L
                        },
                        new StreamSerializable(new MemoryStream())
                    ),

                    new EnvelopeItem(
                        new Dictionary<string, object>
                        {
                            ["type"] = "attachment",
                            ["length"] = 0L
                        },
                        new StreamSerializable(new MemoryStream())
                    )
                }
            );

            // Act
            var output = await envelope.SerializeToStringAsync();

            // Assert
            output.Should().Be(
                "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\"}\n" +
                "{\"type\":\"attachment\",\"length\":0}\n" +
                "\n" +
                "{\"type\":\"attachment\",\"length\":0}\n" +
                "\n"
            );
        }

        [Fact]
        public async Task Deserialization_EnvelopeWithTwoEmptyItems_Success()
        {
            // Arrange
            using var input = (
                "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\"}\n" +
                "{\"type\":\"attachment\",\"length\":0}\n" +
                "\n" +
                "{\"type\":\"attachment\",\"length\":0}\n" +
                "\n"
            ).ToMemoryStream();

            using var expectedEnvelope = new Envelope(
                new Dictionary<string, object> {["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc"},
                new[]
                {
                    new EnvelopeItem(
                        new Dictionary<string, object>
                        {
                            ["type"] = "attachment",
                            ["length"] = 0L
                        },
                        new StreamSerializable(new MemoryStream())
                    ),

                    new EnvelopeItem(
                        new Dictionary<string, object>
                        {
                            ["type"] = "attachment",
                            ["length"] = 0L
                        },
                        new StreamSerializable(new MemoryStream())
                    )
                }
            );

            // Act
            using var envelope = await Envelope.DeserializeAsync(input);

            // Assert
            envelope.Should().BeEquivalentTo(expectedEnvelope);
        }

        [Fact]
        public async Task Serialization_EnvelopeWithItemWithoutLength_Success()
        {
            // Arrange
            using var envelope = new Envelope(
                new Dictionary<string, object> {["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc"},
                new[]
                {
                    new EnvelopeItem(
                        new Dictionary<string, object> {["type"] = "attachment"},
                        new StreamSerializable("helloworld".ToMemoryStream())
                    )
                }
            );

            // Act
            var output = await envelope.SerializeToStringAsync();

            // Assert
            output.Should().Be(
                "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\"}\n" +
                "{\"type\":\"attachment\",\"length\":10}\n" +
                "helloworld\n"
            );
        }

        [Fact]
        public async Task Deserialization_EnvelopeWithItemWithoutLength_Success()
        {
            // Arrange
            using var input = (
                "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\"}\n" +
                "{\"type\":\"attachment\"}\n" +
                "helloworld\n"
            ).ToMemoryStream();

            using var expectedEnvelope = new Envelope(
                new Dictionary<string, object> {["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc"},
                new[]
                {
                    new EnvelopeItem(
                        new Dictionary<string, object> {["type"] = "attachment"},
                        new StreamSerializable("helloworld".ToMemoryStream())
                    )
                }
            );

            // Act
            using var envelope = await Envelope.DeserializeAsync(input);

            // Assert
            envelope.Should().BeEquivalentTo(expectedEnvelope);
        }

        [Fact]
        public async Task Roundtrip_WithEvent_Success()
        {
            // Arrange
            var @event = new SentryEvent
            {
                Message = "Foo bar",
                Environment = "release"
            };

            using var envelope = Envelope.FromEvent(@event);

            using var stream = new MemoryStream();

            // Act
            await envelope.SerializeAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);

            using var envelopeRoundtrip = await Envelope.DeserializeAsync(stream);

            // Assert

            // Can't compare the entire object graph because output envelope contains evaluated length,
            // which original envelope doesn't have.
            envelopeRoundtrip.Header.Should().BeEquivalentTo(envelope.Header);
            envelopeRoundtrip.Items.Should().ContainSingle();
            envelopeRoundtrip.Items[0].Payload.Should().BeEquivalentTo(@event);
        }

        [Fact]
        public async Task Roundtrip_WithUserFeedback_Success()
        {
            // Arrange
            var feedback = new UserFeedback(
                new SentryId(Guid.NewGuid()),
                "foo@bar.com",
                "Everything sucks",
                "Donald J. Trump"
            );

            using var envelope = Envelope.FromUserFeedback(feedback);

            using var stream = new MemoryStream();

            // Act
            await envelope.SerializeAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);

            using var envelopeRoundtrip = await Envelope.DeserializeAsync(stream);

            // Assert

            // Can't compare the entire object graph because output envelope contains evaluated length,
            // which original envelope doesn't have.
            envelopeRoundtrip.Header.Should().BeEquivalentTo(envelope.Header);
            envelopeRoundtrip.Items.Should().ContainSingle();
            envelopeRoundtrip.Items[0].Payload.Should().BeEquivalentTo(feedback);
        }

        [Fact]
        public async Task Deserialization_EmptyStream_Throws()
        {
            // Arrange
            using var input = new MemoryStream();

            // Act & assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await Envelope.DeserializeAsync(input)
            );
        }

        [Fact]
        public async Task Deserialization_InvalidData_Throws()
        {
            // Arrange
            using var input = new MemoryStream(new byte[1_000_000]); // all 0's

            // Act & assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await Envelope.DeserializeAsync(input)
            );
        }

        [Fact]
        public async Task Deserialization_MalformedData_Throws()
        {
            // Arrange
            using var input = (
                // no header
                "helloworld\n"
            ).ToMemoryStream();

            // Act & assert
            // Currently throws a Newtonsoft.Json exception, which is an implementation detail
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await Envelope.DeserializeAsync(input)
            );
        }
    }
}
