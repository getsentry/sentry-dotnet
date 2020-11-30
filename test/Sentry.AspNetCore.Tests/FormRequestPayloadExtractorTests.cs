using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class FormRequestPayloadExtractorTests : BaseRequestPayloadExtractorTests<FormRequestPayloadExtractor>
    {
        public FormRequestPayloadExtractorTests()
        {
            TestFixture = new Fixture();
            _ = TestFixture.HttpRequest.ContentType.Returns("application/x-www-form-urlencoded");
        }

        [Fact]
        public void ExtractPayload_SupportedContentType_ReadForm()
        {
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
            _ = TestFixture.HttpRequest.ContentType.Returns("application/json");

            var sut = TestFixture.GetSut();

            Assert.Null(sut.ExtractPayload(TestFixture.HttpRequest));

            TestFixture.Stream.DidNotReceive().Position = Arg.Any<long>();
        }
    }
}
