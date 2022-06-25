using System.Text.Json;
using Sentry.Testing;

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
    public async Task Serialization(string name, object target)
    {
        var json = target.ToJsonString(_testOutputLogger);
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
