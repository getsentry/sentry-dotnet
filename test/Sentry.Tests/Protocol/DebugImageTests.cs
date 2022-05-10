namespace Sentry.Tests.Protocol;

public class DebugImageTests
{
    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new DebugImage
        {
            Type = "elf",
            ImageAddress = "0xffffffff",
            ImageSize = 1234,
            DebugId = "900f7d1b868432939de4457478f34720",
            DebugFile = "libc.debug",
            CodeId = "900f7d1b868432939de4457478f34720",
            CodeFile = "libc.so"
        };

        var actual = sut.ToJsonString();

        Assert.Equal(
            "{" +
            "\"type\":\"elf\"," +
            "\"image_addr\":\"0xffffffff\"," +
            "\"image_size\":1234," +
            "\"debug_id\":\"900f7d1b868432939de4457478f34720\"," +
            "\"debug_file\":\"libc.debug\"," +
            "\"code_id\":\"900f7d1b868432939de4457478f34720\"," +
            "\"code_file\":\"libc.so\"" +
            "}",
            actual);

        var parsed = DebugImage.FromJson(Json.Parse(actual));

        parsed.Should().BeEquivalentTo(sut);
    }
}
