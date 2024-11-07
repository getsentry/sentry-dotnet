using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Sentry.AspNetCore.Tests;

public class FormRequestPayloadExtractorTests : BaseRequestPayloadExtractorTests<FormRequestPayloadExtractor>
{
    public FormRequestPayloadExtractorTests()
    {
        TestFixture = new Fixture();
    }

    [Theory]
    [InlineData("application/x-www-form-urlencoded")]
    [InlineData("application/x-www-form-urlencoded; charset=utf-8")]
    public void ExtractPayload_SupportedContentType_ReadForm(string contentType)
    {
        TestFixture.HttpRequest.ContentType.Returns(contentType);

        var expected = new Dictionary<string, StringValues> { { "key", new StringValues("val") } };
        var f = new FormCollection(expected);
        _ = TestFixture.HttpRequestCore.Form.Returns(f);

        var sut = TestFixture.GetSut();

        var actual = sut.ExtractPayload(TestFixture.HttpRequest);
        Assert.NotNull(actual);

        var actualDic = actual as IDictionary<string, IEnumerable<string>>;
        Assert.NotNull(actualDic);

        Assert.Equal(expected.Count, actualDic.Count);
    }

    [Fact]
    public void ExtractPayload_UnsupportedContentType_DoesNotReadStream()
    {
        TestFixture.HttpRequest.ContentType.Returns("application/json");

        var sut = TestFixture.GetSut();

        Assert.Null(sut.ExtractPayload(TestFixture.HttpRequest));

        TestFixture.Stream.DidNotReceive().Position = Arg.Any<long>();
    }
}
