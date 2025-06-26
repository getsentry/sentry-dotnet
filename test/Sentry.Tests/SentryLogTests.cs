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

    private readonly TestOutputDiagnosticLogger _output;

    public SentryLogTests(ITestOutputHelper output)
    {
        _output = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void Protocol_Default_VerifyAttributes()
    {
        var options = new SentryOptions
        {
            Environment = "my-environment",
            Release = "my-release",
        };
        var sdk = new SdkVersion
        {
            Name = "Sentry.Test.SDK",
            Version = "1.2.3-test+Sentry"
        };

        var log = new SentryLog(Timestamp, TraceId, (SentryLogLevel)24, "message")
        {
            Template = "template",
            Parameters = ImmutableArray.Create<object>("params"),
            ParentSpanId = ParentSpanId,
        };
        log.SetAttribute("attribute", "value");
        log.SetDefaultAttributes(options, sdk);

        log.Timestamp.Should().Be(Timestamp);
        log.TraceId.Should().Be(TraceId);
        log.Level.Should().Be((SentryLogLevel)24);
        log.Message.Should().Be("message");
        log.Template.Should().Be("template");
        log.Parameters.Should().BeEquivalentTo(["params"]);
        log.ParentSpanId.Should().Be(ParentSpanId);

        log.TryGetAttribute("attribute", out object attribute).Should().BeTrue();
        attribute.Should().Be("value");
        log.TryGetAttribute("sentry.environment", out string environment).Should().BeTrue();
        environment.Should().Be(options.Environment);
        log.TryGetAttribute("sentry.release", out string release).Should().BeTrue();
        release.Should().Be(options.Release);
        log.TryGetAttribute("sentry.sdk.name", out string name).Should().BeTrue();
        name.Should().Be(sdk.Name);
        log.TryGetAttribute("sentry.sdk.version", out string version).Should().BeTrue();
        version.Should().Be(sdk.Version);
        log.TryGetAttribute("not-found", out object notFound).Should().BeFalse();
        notFound.Should().BeNull();
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
        log.SetDefaultAttributes(options, new SdkVersion());

        var envelope = Envelope.FromLogs([log]);

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
                }
              }
            }
          ]
        }
        """);

        _output.Entries.Should().BeEmpty();
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
        log.SetDefaultAttributes(options, new SdkVersion { Name = "Sentry.Test.SDK", Version = "1.2.3-test+Sentry" });

        var envelope = EnvelopeItem.FromLogs([log]);

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
                  "value": "Sentry.Test.SDK",
                  "type": "string"
                },
                "sentry.sdk.version": {
                  "value": "1.2.3-test+Sentry",
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

        _output.Entries.Should().BeEmpty();
    }

#if (NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER) //System.Buffers.ArrayBufferWriter<T>
    [Fact]
    public void WriteTo_MessageParameters_AsAttributes()
    {
        var log = new SentryLog(Timestamp, TraceId, SentryLogLevel.Trace, "message")
        {
            Parameters =
            [
                sbyte.MinValue,
                byte.MaxValue,
                short.MinValue,
                ushort.MaxValue,
                int.MinValue,
                uint.MaxValue,
                long.MinValue,
                ulong.MaxValue,
                nint.MinValue,
                nuint.MaxValue,
                1f,
                2d,
                3m,
                true,
                'c',
                "string",
                KeyValuePair.Create("key", "value"),
                null,
            ],
        };

        ArrayBufferWriter<byte> bufferWriter = new();
        using Utf8JsonWriter writer = new(bufferWriter);
        log.WriteTo(writer, _output);
        writer.Flush();

        var document = JsonDocument.Parse(bufferWriter.WrittenMemory);
        var attributes = document.RootElement.GetProperty("attributes");
        Assert.Collection(attributes.EnumerateObject().ToArray(),
            property => property.AssertAttributeInteger("sentry.message.parameter.0", json => json.GetSByte(), sbyte.MinValue),
            property => property.AssertAttributeInteger("sentry.message.parameter.1", json => json.GetByte(), byte.MaxValue),
            property => property.AssertAttributeInteger("sentry.message.parameter.2", json => json.GetInt16(), short.MinValue),
            property => property.AssertAttributeInteger("sentry.message.parameter.3", json => json.GetUInt16(), ushort.MaxValue),
            property => property.AssertAttributeInteger("sentry.message.parameter.4", json => json.GetInt32(), int.MinValue),
            property => property.AssertAttributeInteger("sentry.message.parameter.5", json => json.GetUInt32(), uint.MaxValue),
            property => property.AssertAttributeInteger("sentry.message.parameter.6", json => json.GetInt64(), long.MinValue),
            property => property.AssertAttributeString("sentry.message.parameter.7", json => json.GetString(), ulong.MaxValue.ToString(NumberFormatInfo.InvariantInfo)),
            property => property.AssertAttributeInteger("sentry.message.parameter.8", json => json.GetInt64(), nint.MinValue),
            property => property.AssertAttributeString("sentry.message.parameter.9", json => json.GetString(), nuint.MaxValue.ToString(NumberFormatInfo.InvariantInfo)),
            property => property.AssertAttributeDouble("sentry.message.parameter.10", json => json.GetSingle(), 1f),
            property => property.AssertAttributeDouble("sentry.message.parameter.11", json => json.GetDouble(), 2d),
            property => property.AssertAttributeString("sentry.message.parameter.12", json => json.GetString(), 3m.ToString(NumberFormatInfo.InvariantInfo)),
            property => property.AssertAttributeBoolean("sentry.message.parameter.13", json => json.GetBoolean(), true),
            property => property.AssertAttributeString("sentry.message.parameter.14", json => json.GetString(), "c"),
            property => property.AssertAttributeString("sentry.message.parameter.15", json => json.GetString(), "string"),
            property => property.AssertAttributeString("sentry.message.parameter.16", json => json.GetString(), "[key, value]")
        );
        Assert.Collection(_output.Entries,
            entry => entry.Message.Should().Match("*ulong*is not supported*overflow*"),
            entry => entry.Message.Should().Match("*nuint*is not supported*64-bit*"),
            entry => entry.Message.Should().Match("*decimal*is not supported*overflow*"),
            entry => entry.Message.Should().Match("*System.Collections.Generic.KeyValuePair`2[System.String,System.String]*is not supported*ToString*"),
            entry => entry.Message.Should().Match("*null*is not supported*ignored*")
        );
    }
#endif

#if (NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER) //System.Buffers.ArrayBufferWriter<T>
    [Fact]
    public void WriteTo_Attributes_AsJson()
    {
        var log = new SentryLog(Timestamp, TraceId, SentryLogLevel.Trace, "message");
        log.SetAttribute("sbyte", sbyte.MinValue);
        log.SetAttribute("byte", byte.MaxValue);
        log.SetAttribute("short", short.MinValue);
        log.SetAttribute("ushort", ushort.MaxValue);
        log.SetAttribute("int", int.MinValue);
        log.SetAttribute("uint", uint.MaxValue);
        log.SetAttribute("long", long.MinValue);
        log.SetAttribute("ulong", ulong.MaxValue);
        log.SetAttribute("nint", nint.MinValue);
        log.SetAttribute("nuint", nuint.MaxValue);
        log.SetAttribute("float", 1f);
        log.SetAttribute("double", 2d);
        log.SetAttribute("decimal", 3m);
        log.SetAttribute("bool", true);
        log.SetAttribute("char", 'c');
        log.SetAttribute("string", "string");
        log.SetAttribute("object", KeyValuePair.Create("key", "value"));
        log.SetAttribute("null", null!);

        ArrayBufferWriter<byte> bufferWriter = new();
        using Utf8JsonWriter writer = new(bufferWriter);
        log.WriteTo(writer, _output);
        writer.Flush();

        var document = JsonDocument.Parse(bufferWriter.WrittenMemory);
        var attributes = document.RootElement.GetProperty("attributes");
        Assert.Collection(attributes.EnumerateObject().ToArray(),
            property => property.AssertAttributeInteger("sbyte", json => json.GetSByte(), sbyte.MinValue),
            property => property.AssertAttributeInteger("byte", json => json.GetByte(), byte.MaxValue),
            property => property.AssertAttributeInteger("short", json => json.GetInt16(), short.MinValue),
            property => property.AssertAttributeInteger("ushort", json => json.GetUInt16(), ushort.MaxValue),
            property => property.AssertAttributeInteger("int", json => json.GetInt32(), int.MinValue),
            property => property.AssertAttributeInteger("uint", json => json.GetUInt32(), uint.MaxValue),
            property => property.AssertAttributeInteger("long", json => json.GetInt64(), long.MinValue),
            property => property.AssertAttributeString("ulong", json => json.GetString(), ulong.MaxValue.ToString(NumberFormatInfo.InvariantInfo)),
            property => property.AssertAttributeInteger("nint", json => json.GetInt64(), nint.MinValue),
            property => property.AssertAttributeString("nuint", json => json.GetString(), nuint.MaxValue.ToString(NumberFormatInfo.InvariantInfo)),
            property => property.AssertAttributeDouble("float", json => json.GetSingle(), 1f),
            property => property.AssertAttributeDouble("double", json => json.GetDouble(), 2d),
            property => property.AssertAttributeString("decimal", json => json.GetString(), 3m.ToString(NumberFormatInfo.InvariantInfo)),
            property => property.AssertAttributeBoolean("bool", json => json.GetBoolean(), true),
            property => property.AssertAttributeString("char", json => json.GetString(), "c"),
            property => property.AssertAttributeString("string", json => json.GetString(), "string"),
            property => property.AssertAttributeString("object", json => json.GetString(), "[key, value]")
        );
        Assert.Collection(_output.Entries,
            entry => entry.Message.Should().Match("*ulong*is not supported*overflow*"),
            entry => entry.Message.Should().Match("*nuint*is not supported*64-bit*"),
            entry => entry.Message.Should().Match("*decimal*is not supported*overflow*"),
            entry => entry.Message.Should().Match("*System.Collections.Generic.KeyValuePair`2[System.String,System.String]*is not supported*ToString*"),
            entry => entry.Message.Should().Match("*null*is not supported*ignored*")
        );
    }
#endif
}

file static class AssertExtensions
{
    public static void AssertAttributeString<T>(this JsonProperty attribute, string name, Func<JsonElement, T> getValue, T value)
    {
        attribute.AssertAttribute(name, "string", getValue, value);
    }

    public static void AssertAttributeBoolean<T>(this JsonProperty attribute, string name, Func<JsonElement, T> getValue, T value)
    {
        attribute.AssertAttribute(name, "boolean", getValue, value);
    }

    public static void AssertAttributeInteger<T>(this JsonProperty attribute, string name, Func<JsonElement, T> getValue, T value)
    {
        attribute.AssertAttribute(name, "integer", getValue, value);
    }

    public static void AssertAttributeDouble<T>(this JsonProperty attribute, string name, Func<JsonElement, T> getValue, T value)
    {
        attribute.AssertAttribute(name, "double", getValue, value);
    }

    private static void AssertAttribute<T>(this JsonProperty attribute, string name, string type, Func<JsonElement, T> getValue, T value)
    {
        Assert.Equal(name, attribute.Name);
        Assert.Collection(attribute.Value.EnumerateObject().ToArray(),
            property =>
            {
                Assert.Equal("value", property.Name);
                Assert.Equal(value, getValue(property.Value));
            }, property =>
            {
                Assert.Equal("type", property.Name);
                Assert.Equal(type, property.Value.GetString());
            });
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
