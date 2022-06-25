using System.Text.Json;

[UsesVerify]
public class SerializationTests
{
    [Theory]
    [MemberData(nameof(GetData))]
    public async Task Serialization(string name, object target)
    {
        using var stream = new MemoryStream();
        await using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteDynamicValue(target, null);
        }

        var json = Encoding.UTF8.GetString(stream.ToArray());
        await Verify(json).UseParameters(name);
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
    }
}
