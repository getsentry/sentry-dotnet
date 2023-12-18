namespace Sentry.Tests.Protocol;

public class DebugImageTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public DebugImageTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new DebugImage
        {
            Type = "elf",
            ImageAddress = 5,
            ImageSize = 1234,
            DebugId = "900f7d1b868432939de4457478f34720",
            DebugFile = "libc.debug",
            CodeId = "900f7d1b868432939de4457478f34720",
            CodeFile = "libc.so"
        };

        var actual = sut.ToJsonString(_testOutputLogger, indented: true);

        Assert.Equal("""
            {
              "type": "elf",
              "image_addr": "0x5",
              "image_size": 1234,
              "debug_id": "900f7d1b868432939de4457478f34720",
              "debug_file": "libc.debug",
              "code_id": "900f7d1b868432939de4457478f34720",
              "code_file": "libc.so"
            }
            """,
            actual);

        var parsed = Json.Parse(actual, DebugImage.FromJson);

        parsed.Should().BeEquivalentTo(sut);
    }
}
