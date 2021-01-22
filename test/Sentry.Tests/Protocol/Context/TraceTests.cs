using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol.Context
{
    public class TraceTests
    {
        [Fact]
        public void Ctor_NoPropertyFilled_SerializesEmptyObject()
        {
            // Arrange
            var trace = new Trace();

            // Act
            var actual = trace.ToJsonString();

            // Assert
            Assert.Equal("{\"type\":\"trace\"}", actual);
        }

        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            // Arrange
            var trace = new Trace
            {
                Operation = "op123",
                Status = SpanStatus.Aborted,
                IsSampled = false,
                ParentSpanId = SpanId.Parse("1000000000000000"),
                SpanId = SpanId.Parse("2000000000000000"),
                TraceId = SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8")
            };

            // Act
            var actual = trace.ToJsonString();

            // Assert
            Assert.Equal(
                "{" +
                "\"type\":\"trace\"," +
                "\"span_id\":\"2000000000000000\"," +
                "\"parent_span_id\":\"1000000000000000\"," +
                "\"trace_id\":\"75302ac48a024bde9a3b3734a82e36c8\"," +
                "\"op\":\"op123\"," +
                "\"status\":\"aborted\"," +
                "\"sampled\":false" +
                "}",
                actual
            );
        }

        [Fact]
        public void Clone_CopyValues()
        {
            // Arrange
            var trace = new Trace
            {
                Operation = "op123",
                Status = SpanStatus.Aborted,
                IsSampled = false,
                ParentSpanId = SpanId.Parse("1000000000000000"),
                SpanId = SpanId.Parse("2000000000000000"),
                TraceId = SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8")
            };

            // Act
            var clone = trace.Clone();

            // Assert
            Assert.Equal(trace.Operation, clone.Operation);
            Assert.Equal(trace.Status, clone.Status);
            Assert.Equal(trace.IsSampled, clone.IsSampled);
            Assert.Equal(trace.ParentSpanId, clone.ParentSpanId);
            Assert.Equal(trace.SpanId, clone.SpanId);
            Assert.Equal(trace.TraceId, clone.TraceId);
        }
    }
}
