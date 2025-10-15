namespace Sentry.Tests.Protocol.Exceptions;

public class MechanismTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public MechanismTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new Mechanism
        {
            Type = "mechanism type",
            Description = "mechanism description",
            Source = "exception source",
            Handled = true,
            Terminal = false,
            HelpLink = "https://helplink",
            Synthetic = true,
            IsExceptionGroup = true,
            ExceptionId = 123,
            ParentId = 456
        };

        sut.Data.Add("data-key", "data-value");
        sut.Meta.Add("meta-key", "meta-value");

        var actual = sut.ToJsonString(_testOutputLogger, indented: true);

        const string expected = """
            {
              "type": "mechanism type",
              "description": "mechanism description",
              "source": "exception source",
              "help_link": "https://helplink",
              "handled": true,
              "terminal": false,
              "synthetic": true,
              "is_exception_group": true,
              "exception_id": 123,
              "parent_id": 456,
              "data": {
                "data-key": "data-value"
              },
              "meta": {
                "meta-key": "meta-value"
              }
            }
            """;

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((Mechanism mechanism, string serialized) @case)
    {
        var actual = @case.mechanism.ToJsonString(_testOutputLogger);

        Assert.Equal(@case.serialized, actual);
    }

    public static IEnumerable<object[]> TestCases()
    {
        yield return new object[] { (new Mechanism(), """{"type":"generic"}""") };
        yield return new object[] { (new Mechanism { Type = "some type" }, """{"type":"some type"}""") };
        yield return new object[] { (new Mechanism { Handled = false }, """{"type":"generic","handled":false}""") };
        yield return new object[] { (new Mechanism { Handled = true }, """{"type":"generic","handled":true}""") };
        yield return new object[] { (new Mechanism { Terminal = false }, """{"type":"generic","terminal":false}""") };
        yield return new object[] { (new Mechanism { Terminal = true }, """{"type":"generic","terminal":true}""") };
        yield return new object[] { (new Mechanism { Terminal = null }, """{"type":"generic"}""") }; // null = default, not serialized
        yield return new object[] { (new Mechanism { Synthetic = true }, """{"type":"generic","synthetic":true}""") };
        yield return new object[] { (new Mechanism { HelpLink = "https://sentry.io/docs" }, """{"type":"generic","help_link":"https://sentry.io/docs"}""") };
        yield return new object[] { (new Mechanism { Description = "some desc" }, """{"type":"generic","description":"some desc"}""") };
        yield return new object[] { (new Mechanism { Data = { new KeyValuePair<string, object>("data-key", "data-value") } }, """{"type":"generic","data":{"data-key":"data-value"}}""") };
        yield return new object[] { (new Mechanism { Meta = { new KeyValuePair<string, object>("meta-key", "meta-value") } }, """{"type":"generic","meta":{"meta-key":"meta-value"}}""") };
    }
}
