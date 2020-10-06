using FluentAssertions;
using Sentry.Protocol.Builders;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class EnvelopeTests
    {
        // Tests driven by examples from docs:
        // https://develop.sentry.dev/sdk/envelopes/#full-examples

        [Fact]
        public void Envelope_without_items_is_serialized_correctly()
        {
            // Arrange
            var envelope = new EnvelopeBuilder()
                .AddHeader("event_id", "12c2d058d58442709aa2eca08bf20986")
                .Build();

            // Act
            var output = envelope.Serialize();

            // Assert
            output.Should().Be(
                "{\"event_id\":\"12c2d058d58442709aa2eca08bf20986\"}\n"
            );
        }

        [Fact]
        public void Envelope_with_two_items_is_serialized_correctly()
        {
            // Arrange
            var envelope = new EnvelopeBuilder()
                .AddHeader("event_id", "9ec79c33ec9942ab8353589fcb2e04dc")
                .AddHeader("dsn", "https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42")
                .AddItem(i => i
                    .AddHeader("type", "attachment")
                    .AddHeader("length", 10)
                    .AddHeader("content_type", "text/plain")
                    .AddHeader("filename", "hello.txt")
                    .SetData("\xef\xbb\xbfHello\r\n"))
                .AddItem(i => i
                    .AddHeader("type", "event")
                    .AddHeader("length", 41)
                    .AddHeader("content_type", "application/json")
                    .AddHeader("filename", "application.log")
                    .SetData("{\"message\":\"hello world\",\"level\":\"error\"}"))
                .Build();

            // Act
            var output = envelope.Serialize();

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
        public void Envelope_with_two_empty_items_is_serialized_correctly()
        {
            // Arrange
            var envelope = new EnvelopeBuilder()
                .AddHeader("event_id", "9ec79c33ec9942ab8353589fcb2e04dc")
                .AddItem(i => i
                    .AddHeader("type", "attachment")
                    .AddHeader("length",  0))
                .AddItem(i => i
                    .AddHeader("type", "attachment")
                    .AddHeader("length",  0))
                .Build();

            // Act
            var output = envelope.Serialize();

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
        public void Envelope_with_an_item_with_implicit_length_is_serialized_correctly()
        {
            // Arrange
            var envelope = new EnvelopeBuilder()
                .AddHeader("event_id", "9ec79c33ec9942ab8353589fcb2e04dc")
                .AddItem(i => i
                    .AddHeader("type", "attachment")
                    .SetData("helloworld"))
                .Build();

            // Act
            var output = envelope.Serialize();

            // Assert
            output.Should().Be(
                "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\"}\n" +
                "{\"type\":\"attachment\"}\n" +
                "helloworld\n"
            );
        }

        [Fact]
        public void Envelope_without_headers_is_serialized_correctly()
        {
            // Arrange
            var envelope = new EnvelopeBuilder()
                .AddItem(i => i
                    .AddHeader("type", "session")
                    .SetData("{\"started\": \"2020-02-07T14:16:00Z\",\"attrs\":{\"release\":\"sentry-test@1.0.0\"}}"))
                .Build();

            // Act
            var output = envelope.Serialize();

            // Assert
            output.Should().Be(
                "{}\n" +
                "{\"type\":\"session\"}\n" +
                "{\"started\": \"2020-02-07T14:16:00Z\",\"attrs\":{\"release\":\"sentry-test@1.0.0\"}}\n"
            );
        }
    }
}
