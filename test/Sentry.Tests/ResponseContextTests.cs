namespace Sentry.Tests;

public class ResponseContextTests
{
    [Fact]
    public void Clone_CopyValues()
    {
        // Arrange
        var sut = new ResponseContext
        {
            BodySize = 42,
            Cookies = "PHPSESSID=298zf09hf012fh2; csrftoken=u32t4o3tb3gg43; _gat=1;",
            StatusCode = 500
        };
        sut.Headers.Add("X-Test", "header");

        // Act
        var clone = sut.Clone();

        // Assert
        clone.BodySize.Should().Be(sut.BodySize);
        clone.Cookies.Should().Be(sut.Cookies);
        clone.StatusCode.Should().Be(sut.StatusCode);
        clone.InternalHeaders.Should().NotBeSameAs(sut.InternalHeaders);
        clone.Headers.Should().BeEquivalentTo(sut.Headers);
    }

    [Fact]
    public void ToJson_CopyValues()
    {
        // Arrange
        var expected = new ResponseContext
        {
            BodySize = 42,
            Cookies = "PHPSESSID=298zf09hf012fh2; csrftoken=u32t4o3tb3gg43; _gat=1;",
            StatusCode = 500
        };
        expected.Headers.Add("X-Test", "header");

        // Act
        var json = JsonDocument.Parse(expected.ToJsonString());
        var actual = ResponseContext.FromJson(json.RootElement);

        // Assert
        actual.BodySize.Should().Be(expected.BodySize);
        actual.Cookies.Should().Be(expected.Cookies);
        actual.StatusCode.Should().Be(expected.StatusCode);
        actual.InternalHeaders.Should().NotBeSameAs(expected.InternalHeaders);
        actual.Headers.Should().BeEquivalentTo(expected.Headers);
    }

    [Fact]
    public void AddHeaders_CreatesOneEntryPerHeader()
    {
        // Arrange
        var sut = new ResponseContext();
        var headers = new List<KeyValuePair<string, IEnumerable<string>>>
        {
            new("X-Test", new[] { "header1", "header2" })
        };

        // Act
        sut.AddHeaders(headers);

        // Assert
        sut.Headers.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "X-Test", "header1; header2" }
        });
    }
}
