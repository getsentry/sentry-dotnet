#if NET7_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
#endif

namespace Sentry.Tests;

public class SerializationTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SerializationTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Theory]
    [MemberData(nameof(GetData))]
    public async Task Serialization(string name, object target)
    {
        var json = target.ToJsonString(_testOutputLogger);
        await Verify(json).UseParameters(name);
    }

#if NET7_0_OR_GREATER
    internal class NestedStringClass { public string Value { get; set; } }
    internal class NestedIntClass { public int Value { get; set; } }
    internal class NestedNIntClass { public nint Value { get; set; } }
    internal class NestedNuIntClass { public nuint Value { get; set; } }
    internal class NestedIntPtrClass { public IntPtr Value { get; set; } }
    internal class NestedNullableIntPtrClass { public IntPtr? Value { get; set; } }
    internal class NestedUIntPtrClass { public UIntPtr Value { get; set; } }
    internal class NestedNullableUIntPtrClass { public UIntPtr? Value { get; set; } }

    public static IEnumerable<object[]> GetData()
    {
        yield return new object[] { "string", "string value" };
        yield return new object[] { "int", 5 };

        JsonExtensions.ResetSerializerOptions();
        JsonExtensions.AddJsonSerializerContext(options => new SerializationTestsJsonContext(options));
        yield return new object[] { "nested string", new NestedStringClass { Value = "string value" } };
        yield return new object[] { "nested int", new NestedIntClass { Value = 5 } };
        yield return new object[] { "nested nint", new NestedNIntClass { Value = 5 } };
        yield return new object[] { "nested nuint", new NestedNuIntClass { Value = 5 } };
        yield return new object[] { "nested IntPtr", new NestedIntPtrClass { Value = (IntPtr)3 } };
        yield return new object[] { "nested nullable IntPtr", new NestedNullableIntPtrClass { Value = (IntPtr?)3 } };
        yield return new object[] { "nested UIntPtr", new NestedUIntPtrClass { Value = (UIntPtr)3 } };
        yield return new object[] { "nested nullable UIntPtr", new NestedNullableUIntPtrClass { Value = (UIntPtr?)3 } };

        JsonExtensions.ResetSerializerOptions();
        JsonExtensions.AddJsonConverter(new CustomObjectConverter());
        JsonExtensions.AddJsonSerializerContext(options => new SerializationTestsJsonContext(options));
        yield return new object[] { "custom object with value", new CustomObject("test") };
        yield return new object[] { "custom object with null", new CustomObject(null) };
    }
#else
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
        JsonExtensions.AddJsonConverter(new CustomObjectConverter());
        yield return new object[] {"custom object with value", new CustomObject("test")};
        yield return new object[] {"custom object with null", new CustomObject(null)};
    }
#endif

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

#if NET7_0_OR_GREATER
[JsonSerializable(typeof(SerializationTests.CustomObject))]
[JsonSerializable(typeof(SerializationTests.NestedStringClass))]
[JsonSerializable(typeof(SerializationTests.NestedIntClass))]
[JsonSerializable(typeof(SerializationTests.NestedNIntClass))]
[JsonSerializable(typeof(SerializationTests.NestedNuIntClass))]
[JsonSerializable(typeof(SerializationTests.NestedIntPtrClass))]
[JsonSerializable(typeof(SerializationTests.NestedNullableIntPtrClass))]
[JsonSerializable(typeof(SerializationTests.NestedUIntPtrClass))]
[JsonSerializable(typeof(SerializationTests.NestedNullableUIntPtrClass))]
internal partial class SerializationTestsJsonContext : JsonSerializerContext
{
}
#endif
