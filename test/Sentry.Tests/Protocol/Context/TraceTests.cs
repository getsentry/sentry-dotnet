using Trace = Sentry.Protocol.Trace;

namespace Sentry.Tests.Protocol.Context;

public class TraceTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public TraceTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void Ctor_NoPropertyFilled_SerializesEmptyObject()
    {
        // Arrange
        var trace = new Trace();

        // Act
        var actual = trace.ToJsonString(_testOutputLogger);

        // Assert
        Assert.Equal("""{"type":"trace","origin":"manual"}""", actual);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        // Arrange
        var trace = new Trace
        {
            Operation = "op123",
            Origin = "auto.abc.def.ghi",
            Status = SpanStatus.Aborted,
            IsSampled = false,
            ParentSpanId = SpanId.Parse("1000000000000000"),
            SpanId = SpanId.Parse("2000000000000000"),
            TraceId = SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8")
        };

        // Act
        var actual = trace.ToJsonString(_testOutputLogger, indented: true);

        // Assert
        Assert.Equal(
            """
            {
              "type": "trace",
              "span_id": "2000000000000000",
              "parent_span_id": "1000000000000000",
              "trace_id": "75302ac48a024bde9a3b3734a82e36c8",
              "op": "op123",
              "origin": "auto.abc.def.ghi",
              "status": "aborted"
            }
            """,
            actual);
    }

    [Fact]
    public void Clone_CopyValues()
    {
        // Arrange
        var trace = new Trace
        {
            Operation = "op123",
            Origin = "auto.abc.def.ghi",
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
        Assert.Equal(trace.Origin, clone.Origin);
        Assert.Equal(trace.Status, clone.Status);
        Assert.Equal(trace.IsSampled, clone.IsSampled);
        Assert.Equal(trace.ParentSpanId, clone.ParentSpanId);
        Assert.Equal(trace.SpanId, clone.SpanId);
        Assert.Equal(trace.TraceId, clone.TraceId);
    }

    [Fact]
    public void SpanId_LeadingZero_ToStringValid()
    {
        // Arrange
        const string spanIdInput = "0ecd6f15f72015cb";
        var spanId = new SpanId(spanIdInput);

        // Assert
        Assert.Equal(spanIdInput, spanId.ToString());
    }
}
