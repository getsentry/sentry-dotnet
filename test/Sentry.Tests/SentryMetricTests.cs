using System.Text.Encodings.Web;
using Sentry.PlatformAbstractions;

namespace Sentry.Tests;

/// <summary>
/// See <see href="https://develop.sentry.dev/sdk/telemetry/metrics/"/>.
/// See also <see cref="Sentry.Tests.Protocol.TraceMetricTests"/>.
/// </summary>
public class SentryMetricTests
{
    private static readonly DateTimeOffset Timestamp = new(2025, 04, 22, 14, 51, 00, 789, TimeSpan.FromHours(2));
    private static readonly SentryId TraceId = SentryId.Create();
    private static readonly SpanId? SpanId = Sentry.SpanId.Create();

    private static readonly ISystemClock Clock = new MockClock(Timestamp);

    private readonly TestOutputDiagnosticLogger _output;

    public SentryMetricTests(ITestOutputHelper output)
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
            Version = "1.2.3-test+Sentry",
        };

        var metric = new SentryMetric<int>(Timestamp, TraceId, SentryMetricType.Counter, "sentry_tests.sentry_metric_tests.counter", 1)
        {
            SpanId = SpanId,
            Unit = "test_unit",
        };
        metric.SetAttribute("attribute", "value");
        metric.Attributes.SetDefaultAttributes(options, sdk);

        metric.Timestamp.Should().Be(Timestamp);
        metric.TraceId.Should().Be(TraceId);
        metric.Type.Should().Be(SentryMetricType.Counter);
        metric.Name.Should().Be("sentry_tests.sentry_metric_tests.counter");
        metric.Value.Should().Be(1);
        metric.SpanId.Should().Be(SpanId);
        metric.Unit.Should().BeEquivalentTo("test_unit");

        metric.TryGetAttribute<string>("attribute", out var attribute).Should().BeTrue();
        attribute.Should().Be("value");
        metric.TryGetAttribute<string>("sentry.environment", out var environment).Should().BeTrue();
        environment.Should().Be(options.Environment);
        metric.TryGetAttribute<string>("sentry.release", out var release).Should().BeTrue();
        release.Should().Be(options.Release);
        metric.TryGetAttribute<string>("sentry.sdk.name", out var name).Should().BeTrue();
        name.Should().Be(sdk.Name);
        metric.TryGetAttribute<string>("sentry.sdk.version", out var version).Should().BeTrue();
        version.Should().Be(sdk.Version);
        metric.TryGetAttribute<object>("not-found", out var notFound).Should().BeFalse();
        notFound.Should().BeNull();
    }

    [Fact]
    public void WriteTo_Envelope_MinimalSerializedSentryMetric()
    {
        var options = new SentryOptions
        {
            Environment = "my-environment",
            Release = "my-release",
        };

        var metric = new SentryMetric<int>(Timestamp, TraceId, SentryMetricType.Counter, "sentry_tests.sentry_metric_tests.counter", 1);
        metric.Attributes.SetDefaultAttributes(options, new SdkVersion());

        var envelope = Envelope.FromMetric(new TraceMetric([metric]));

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
          "type": "trace_metric",
          "item_count": 1,
          "content_type": "application/vnd.sentry.items.trace-metric+json",
          "length": ?*
        }
        """);

        payload.ToIndentedJsonString().Should().Be($$"""
        {
          "items": [
            {
              "timestamp": {{Timestamp.GetTimestamp()}},
              "type": "counter",
              "name": "sentry_tests.sentry_metric_tests.counter",
              "value": 1,
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
    public void WriteTo_EnvelopeItem_MaximalSerializedSentryMetric()
    {
        var options = new SentryOptions
        {
            Environment = "my-environment",
            Release = "my-release",
        };

        var metric = new SentryMetric<int>(Timestamp, TraceId, SentryMetricType.Counter, "sentry_tests.sentry_metric_tests.counter", 1)
        {
            SpanId = SpanId,
            Unit = "test_unit",
        };
        metric.SetAttribute("string-attribute", "string-value");
        metric.SetAttribute("boolean-attribute", true);
        metric.SetAttribute("integer-attribute", 3);
        metric.SetAttribute("double-attribute", 4.4);
        metric.Attributes.SetDefaultAttributes(options, new SdkVersion { Name = "Sentry.Test.SDK", Version = "1.2.3-test+Sentry" });

        var envelope = EnvelopeItem.FromMetric(new TraceMetric([metric]));

        using var stream = new MemoryStream();
        envelope.Serialize(stream, _output);
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        var item = JsonDocument.Parse(reader.ReadLine()!);
        var payload = JsonDocument.Parse(reader.ReadLine()!);

        reader.EndOfStream.Should().BeTrue();

        item.ToIndentedJsonString().Should().Match("""
        {
          "type": "trace_metric",
          "item_count": 1,
          "content_type": "application/vnd.sentry.items.trace-metric+json",
          "length": ?*
        }
        """);

        payload.ToIndentedJsonString().Should().Be($$"""
        {
          "items": [
            {
              "timestamp": {{Timestamp.GetTimestamp()}},
              "type": "counter",
              "name": "sentry_tests.sentry_metric_tests.counter",
              "value": 1,
              "trace_id": "{{TraceId.ToString()}}",
              "span_id": "{{SpanId.ToString()}}",
              "unit": "test_unit",
              "attributes": {
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
                }
              }
            }
          ]
        }
        """);

        _output.Entries.Should().BeEmpty();
    }

    [Fact]
    public void WriteTo_NumericValueType_Byte()
    {
        var metric = new SentryMetric<byte>(Timestamp, TraceId, SentryMetricType.Counter, "sentry_tests.sentry_metric_tests.counter", 1);

        var document = metric.ToJsonDocument<SentryMetric>(static (obj, writer, logger) => obj.WriteTo(writer, logger), _output);
        var value = document.RootElement.GetProperty("value");

        value.ValueKind.Should().Be(JsonValueKind.Number);
        value.TryGetByte(out var @byte).Should().BeTrue();
        @byte.Should().Be(1);

        _output.Entries.Should().BeEmpty();
    }

    [Fact]
    public void WriteTo_NumericValueType_Int16()
    {
        var metric = new SentryMetric<short>(Timestamp, TraceId, SentryMetricType.Counter, "sentry_tests.sentry_metric_tests.counter", 1);

        var document = metric.ToJsonDocument<SentryMetric>(static (obj, writer, logger) => obj.WriteTo(writer, logger), _output);
        var value = document.RootElement.GetProperty("value");

        value.ValueKind.Should().Be(JsonValueKind.Number);
        value.TryGetInt16(out var @short).Should().BeTrue();
        @short.Should().Be(1);

        _output.Entries.Should().BeEmpty();
    }

    [Fact]
    public void WriteTo_NumericValueType_Int32()
    {
        var metric = new SentryMetric<int>(Timestamp, TraceId, SentryMetricType.Counter, "sentry_tests.sentry_metric_tests.counter", 1);

        var document = metric.ToJsonDocument<SentryMetric>(static (obj, writer, logger) => obj.WriteTo(writer, logger), _output);
        var value = document.RootElement.GetProperty("value");

        value.ValueKind.Should().Be(JsonValueKind.Number);
        value.TryGetInt32(out var @int).Should().BeTrue();
        @int.Should().Be(1);

        _output.Entries.Should().BeEmpty();
    }

    [Fact]
    public void WriteTo_NumericValueType_Int64()
    {
        var metric = new SentryMetric<long>(Timestamp, TraceId, SentryMetricType.Counter, "sentry_tests.sentry_metric_tests.counter", 1);

        var document = metric.ToJsonDocument<SentryMetric>(static (obj, writer, logger) => obj.WriteTo(writer, logger), _output);
        var value = document.RootElement.GetProperty("value");

        value.ValueKind.Should().Be(JsonValueKind.Number);
        value.TryGetInt64(out var @long).Should().BeTrue();
        @long.Should().Be(1L);

        _output.Entries.Should().BeEmpty();
    }

    [Fact]
    public void WriteTo_NumericValueType_Single()
    {
        var metric = new SentryMetric<float>(Timestamp, TraceId, SentryMetricType.Counter, "sentry_tests.sentry_metric_tests.counter", 1);

        var document = metric.ToJsonDocument<SentryMetric>(static (obj, writer, logger) => obj.WriteTo(writer, logger), _output);
        var value = document.RootElement.GetProperty("value");

        value.ValueKind.Should().Be(JsonValueKind.Number);
        value.TryGetSingle(out var @float).Should().BeTrue();
        @float.Should().Be(1f);

        _output.Entries.Should().BeEmpty();
    }

    [Fact]
    public void WriteTo_NumericValueType_Double()
    {
        var metric = new SentryMetric<double>(Timestamp, TraceId, SentryMetricType.Counter, "sentry_tests.sentry_metric_tests.counter", 1);

        var document = metric.ToJsonDocument<SentryMetric>(static (obj, writer, logger) => obj.WriteTo(writer, logger), _output);
        var value = document.RootElement.GetProperty("value");

        value.ValueKind.Should().Be(JsonValueKind.Number);
        value.TryGetDouble(out var @double).Should().BeTrue();
        @double.Should().Be(1d);

        _output.Entries.Should().BeEmpty();
    }

#if DEBUG && (NET || NETCOREAPP)
    [Fact]
    public void WriteTo_NumericValueType_Decimal()
    {
        var metric = new SentryMetric<decimal>(Timestamp, TraceId, SentryMetricType.Counter, "sentry_tests.sentry_metric_tests.counter", 1);

        var exception = Assert.ThrowsAny<Exception>(() => metric.ToJsonDocument<SentryMetric>(static (obj, writer, logger) => obj.WriteTo(writer, logger), _output));
        exception.Message.Should().Contain($"Unhandled Metric Type {typeof(decimal)}.");
        exception.Message.Should().Contain("This instruction should be unreachable.");

        _output.Entries.Should().BeEmpty();
    }
#endif

    [Fact]
    public void WriteTo_Attributes_AsJson()
    {
        var metric = new SentryMetric<int>(Timestamp, TraceId, SentryMetricType.Counter, "sentry_tests.sentry_metric_tests.counter", 1);
        metric.SetAttribute("sbyte", sbyte.MinValue);
        metric.SetAttribute("byte", byte.MaxValue);
        metric.SetAttribute("short", short.MinValue);
        metric.SetAttribute("ushort", ushort.MaxValue);
        metric.SetAttribute("int", int.MinValue);
        metric.SetAttribute("uint", uint.MaxValue);
        metric.SetAttribute("long", long.MinValue);
        metric.SetAttribute("ulong", ulong.MaxValue);
#if NET5_0_OR_GREATER
        metric.SetAttribute("nint", nint.MinValue);
        metric.SetAttribute("nuint", nuint.MaxValue);
#endif
        metric.SetAttribute("float", 1f);
        metric.SetAttribute("double", 2d);
        metric.SetAttribute("decimal", 3m);
        metric.SetAttribute("bool", true);
        metric.SetAttribute("char", 'c');
        metric.SetAttribute("string", "string");
#if (NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        metric.SetAttribute("object", KeyValuePair.Create("key", "value"));
#else
        metric.SetAttribute("object", new KeyValuePair<string, string>("key", "value"));
#endif
        metric.Attributes.SetAttribute("null", null!);

        var document = metric.ToJsonDocument<SentryMetric>(static (obj, writer, logger) => obj.WriteTo(writer, logger), _output);
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
#if NET5_0_OR_GREATER
            property => property.AssertAttributeInteger("nint", json => json.GetInt64(), nint.MinValue),
            property => property.AssertAttributeString("nuint", json => json.GetString(), nuint.MaxValue.ToString(NumberFormatInfo.InvariantInfo)),
#endif
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
#if NET5_0_OR_GREATER
            entry => entry.Message.Should().Match("*nuint*is not supported*64-bit*"),
#endif
            entry => entry.Message.Should().Match("*decimal*is not supported*overflow*"),
            entry => entry.Message.Should().Match("*System.Collections.Generic.KeyValuePair`2[System.String,System.String]*is not supported*ToString*"),
            entry => entry.Message.Should().Match("*null*is not supported*ignored*")
        );
    }
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

file static class DateTimeOffsetExtensions
{
    public static string GetTimestamp(this DateTimeOffset value)
    {
        var timestamp = value.ToUnixTimeMilliseconds() / 1_000.0;
        return timestamp.ToString(NumberFormatInfo.InvariantInfo);
    }
}

file static class JsonFormatterExtensions
{
    public static string Format(this DateTimeOffset value)
    {
        return value.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo);
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
