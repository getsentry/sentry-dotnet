using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Sentry.Protocol;
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
            var envelope = new Envelope(
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
            var input = "{\"event_id\":\"12c2d058d58442709aa2eca08bf20986\"}\n".ToMemoryStream();

            // Act
            var envelope = await Envelope.DeserializeAsync(input);

            // Assert
            envelope.Should().BeEquivalentTo(new Envelope(
                new Dictionary<string, object> {["event_id"] = "12c2d058d58442709aa2eca08bf20986"},
                Array.Empty<EnvelopeItem>()
            ));
        }

        [Fact]
        public async Task Serialization_EnvelopeWithoutHeader_Success()
        {
            // Arrange
            var envelope = new Envelope(
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
        public async Task Serialization_EnvelopeWithTwoItems_Success()
        {
            // Arrange
            var envelope = new Envelope(
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
                            ["length"] = 10,
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
                "{\"type\":\"attachment\",\"length\":10,\"content_type\":\"text/plain\",\"filename\":\"hello.txt\"}\n" +
                "\xef\xbb\xbfHello\r\n\n" +
                "{\"type\":\"event\",\"length\":41,\"content_type\":\"application/json\",\"filename\":\"application.log\"}\n" +
                "{\"message\":\"hello world\",\"level\":\"error\"}\n"
            );
        }

        [Fact]
        public async Task Serialization_EnvelopeWithTwoEmptyItems_Success()
        {
            // Arrange
            var envelope = new Envelope(
                new Dictionary<string, object> {["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc"},
                new[]
                {
                    new EnvelopeItem(
                        new Dictionary<string, object> {
                            ["type"] = "attachment",
                            ["length"] = 0},
                        new StreamSerializable(new MemoryStream())
                    ),

                    new EnvelopeItem(
                        new Dictionary<string, object>
                        {
                            ["type"] = "attachment",
                            ["length"] = 0
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
        public async Task Serialization_EnvelopeWithItemWithoutLength_Success()
        {
            // Arrange
            var envelope = new Envelope(
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
    }
}
