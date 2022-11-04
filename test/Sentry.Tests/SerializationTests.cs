using System.Text.Json;
using System.Text.Json.Serialization;
using Sentry.Testing;

namespace Sentry.Tests;

[UsesVerify]
public class SerializationTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SerializationTests (ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Theory]
    [MemberData(nameof(GetData))]
    [Trait("Category", "Verify")]
    public async Task Serialization(string name, object target)
    {
        var json = target.ToJsonString(_testOutputLogger);
        await Verifier.Verify(json).UseParameters(name);
    }

    public static IEnumerable<object[]> GetData()
    {
        yield return new object[] {"string", "string value"};
        yield return new object[] {"nested string", new {Value = "string value"}};
        yield return new object[] {"int", 5};
        yield return new object[] {"nested int", new {Value = 5}};
        yield return new object[] {"nested nint", new {Value = (nint)5}};
        yield return new object[] {"nested nuint", new {Value = (nuint)5}};
        yield return new object[] {"nested IntPtr", new {Value = (IntPtr)3}};
        yield return new object[] {"nested nullable IntPtr", new {Value = (IntPtr?)3}};
        yield return new object[] {"nested UIntPtr", new {Value = (UIntPtr)3}};
        yield return new object[] {"nested nullable UIntPtr", new {Value = (IntPtr?)3}};

        JsonExtensions.ResetSerializerOptions();
        new SentryOptions().AddJsonConverter(new CustomObjectConverter());
        yield return new object[] {"custom object with value", new CustomObject("test")};
        yield return new object[] {"custom object with null", new CustomObject(null)};
    }

    public class CustomObject
    {
        public CustomObject(string value)
        {
            Value = value;
        }

        internal string Value { get; }
    }

    public class CustomObjectConverter : JsonConverter<CustomObject>
    {
        public override CustomObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString());

        public override void Write(Utf8JsonWriter writer, CustomObject value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Value);
    }
}
