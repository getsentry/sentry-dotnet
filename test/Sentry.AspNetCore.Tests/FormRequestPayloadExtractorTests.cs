using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class FormRequestPayloadExtractorTests : BaseRequestPayloadExtractorTests<FormRequestPayloadExtractor>
    {
        public FormRequestPayloadExtractorTests()
        {
            TestFixture = new Fixture();
            TestFixture.HttpRequest.ContentType.Returns("application/x-www-form-urlencoded");
        }

        [Fact]
        public void ExtractPayload_SuportedContentType_ReadForm()
        {
            var expected = new Dictionary<string, StringValues> { { "key", new StringValues("val") } };
            var f = new FormCollection(expected);
            TestFixture.HttpRequest.Form.Returns(f);

            var sut = TestFixture.GetSut();

            var actual = sut.ExtractPayload(TestFixture.HttpRequest);
            Assert.NotNull(actual);

            var actualDic = actual as IDictionary<string, StringValues>;
            Assert.NotNull(actualDic);

            Assert.Equal(expected, actualDic);
        }

        [Fact]
        public void ExtractPayload_UnsuportedContentType_DoesNotReadStream()
        {
            TestFixture.HttpRequest.ContentType.Returns("application/json");

            var sut = TestFixture.GetSut();

            Assert.Null(sut.ExtractPayload(TestFixture.HttpRequest));

            TestFixture.Stream.DidNotReceive().Position = Arg.Any<int>();
        }
    }
}
