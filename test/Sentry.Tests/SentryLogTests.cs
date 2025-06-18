using System.Text.Encodings.Web;
using Sentry.PlatformAbstractions;

namespace Sentry.Tests;

/// <summary>
/// <see href="https://develop.sentry.dev/sdk/telemetry/logs/"/>
/// </summary>
public class SentryLogTests
{
    private static readonly DateTimeOffset Timestamp = new(2025, 04, 22, 14, 51, 00, TimeSpan.FromHours(2));
    private static readonly SentryId TraceId = SentryId.Create();
    private static readonly SpanId? ParentSpanId = SpanId.Create();

    private static readonly ISystemClock Clock = new MockClock(Timestamp);

    private readonly IDiagnosticLogger _output;

    public SentryLogTests(ITestOutputHelper output)
    {
        _output = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void WriteTo_Envelope_MinimalSerializedSentryLog()
    {
        var options = new SentryOptions
        {
            Environment = "my-environment",
            Release = "my-release",
        };

        var log = new SentryLog(Timestamp, TraceId, SentryLogLevel.Trace, "message");
        log.SetAttributes(options);

        var envelope = Envelope.FromLog(log);

        using var stream = new MemoryStream();
        envelope.Serialize(stream, _output, Clock);
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        var header = JsonDocument.Parse(reader.ReadLine()!);
        var item = JsonDocument.Parse(reader.ReadLine()!);
        var payload = JsonDocument.Parse(reader.ReadLine()!);

        reader.EndOfStream.Should().BeTrue();

        header.ToIndentedJsonString().Should().Be($$"""
        {
          "sdk": {
            "name": "{{SdkVersion.Instance.Name}}",
            "version": "{{SdkVersion.Instance.Version}}"
          },
          "sent_at": "{{Timestamp.Format()}}"
        }
        """);

        item.ToIndentedJsonString().Should().Match("""
        {
          "type": "log",
          "item_count": 1,
          "content_type": "application/vnd.sentry.items.log+json",
          "length": ?*
        }
        """);

        payload.ToIndentedJsonString().Should().Be($$"""
        {
          "items": [
            {
              "timestamp": {{Timestamp.ToUnixTimeSeconds()}},
              "level": "trace",
              "body": "message",
              "trace_id": "{{TraceId.ToString()}}",
              "attributes": {
                "sentry.environment": {
                  "value": "my-environment",
                  "type": "string"
                },
                "sentry.release": {
                  "value": "my-release",
                  "type": "string"
                },
                "sentry.sdk.name": {
                  "value": "{{SdkVersion.Instance.Name}}",
                  "type": "string"
                },
                "sentry.sdk.version": {
                  "value": "{{SdkVersion.Instance.Version}}",
                  "type": "string"
                }
              }
            }
          ]
        }
        """);
    }

    [Fact]
    public void WriteTo_EnvelopeItem_MaximalSerializedSentryLog()
    {
        var options = new SentryOptions
        {
            Environment = "my-environment",
            Release = "my-release",
        };

        var log = new SentryLog(Timestamp, TraceId, (SentryLogLevel)24, "message")
        {
            Template = "template",
            Parameters = ImmutableArray.Create<object>("string", false, 1, 2.2),
            ParentSpanId = ParentSpanId,
        };
        log.SetAttribute("string-attribute", "string-value");
        log.SetAttribute("boolean-attribute", true);
        log.SetAttribute("integer-attribute", 3);
        log.SetAttribute("double-attribute", 4.4);
        log.SetAttributes(options);

        var envelope = EnvelopeItem.FromLog(log);

        using var stream = new MemoryStream();
        envelope.Serialize(stream, _output);
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        var item = JsonDocument.Parse(reader.ReadLine()!);
        var payload = JsonDocument.Parse(reader.ReadLine()!);

        reader.EndOfStream.Should().BeTrue();

        item.ToIndentedJsonString().Should().Match("""
        {
          "type": "log",
          "item_count": 1,
          "content_type": "application/vnd.sentry.items.log+json",
          "length": ?*
        }
        """);

        payload.ToIndentedJsonString().Should().Be($$"""
        {
          "items": [
            {
              "timestamp": {{Timestamp.ToUnixTimeSeconds()}},
              "level": "fatal",
              "body": "message",
              "trace_id": "{{TraceId.ToString()}}",
              "severity_number": 24,
              "attributes": {
                "sentry.message.template": {
                  "value": "template",
                  "type": "string"
                },
                "sentry.message.parameter.0": {
                  "value": "string",
                  "type": "string"
                },
                "sentry.message.parameter.1": {
                  "value": false,
                  "type": "boolean"
                },
                "sentry.message.parameter.2": {
                  "value": 1,
                  "type": "integer"
                },
                "sentry.message.parameter.3": {
                  "value": {{2.2.Format()}},
                  "type": "double"
                },
                "string-attribute": {
                  "value": "string-value",
                  "type": "string"
                },
                "boolean-attribute": {
                  "value": true,
                  "type": "boolean"
                },
                "integer-attribute": {
                  "value": 3,
                  "type": "integer"
                },
                "double-attribute": {
                  "value": {{4.4.Format()}},
                  "type": "double"
                },
                "sentry.environment": {
                  "value": "my-environment",
                  "type": "string"
                },
                "sentry.release": {
                  "value": "my-release",
                  "type": "string"
                },
                "sentry.sdk.name": {
                  "value": "{{SdkVersion.Instance.Name}}",
                  "type": "string"
                },
                "sentry.sdk.version": {
                  "value": "{{SdkVersion.Instance.Version}}",
                  "type": "string"
                },
                "sentry.trace.parent_span_id": {
                  "value": "{{ParentSpanId.ToString()}}",
                  "type": "string"
                }
              }
            }
          ]
        }
        """);
    }
}

file static class JsonFormatterExtensions
{
    public static string Format(this DateTimeOffset value)
    {
        return value.ToString("yyyy-MM-ddTHH:mm:sszzz", DateTimeFormatInfo.InvariantInfo);
    }

    public static string Format(this double value)
    {
        if (SentryRuntime.Current.IsNetFx() || SentryRuntime.Current.IsMono())
        {
            // since .NET Core 3.0, the Floating-Point Formatter returns the shortest roundtrippable string, rather than the exact string
            // e.g. on .NET Framework (Windows)
            // * 2.2.ToString() -> 2.2000000000000002
            // * 4.4.ToString() -> 4.4000000000000004
            // see https://devblogs.microsoft.com/dotnet/floating-point-parsing-and-formatting-improvements-in-net-core-3-0/

            var utf16Text = value.ToString("G17", NumberFormatInfo.InvariantInfo);
            var utf8Bytes = Encoding.UTF8.GetBytes(utf16Text);
            return Encoding.UTF8.GetString(utf8Bytes);
        }

        return value.ToString(NumberFormatInfo.InvariantInfo);
    }
}

file static class JsonDocumentExtensions
{
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    public static string ToIndentedJsonString(this JsonDocument document)
    {
        var json = JsonSerializer.Serialize(document, Options);

        // Standardize on \n on all platforms, for consistency in tests.
        return IsWindows ? json.Replace("\r\n", "\n") : json;
    }
}
