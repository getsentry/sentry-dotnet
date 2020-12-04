using System.IO;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public abstract class BaseRequestPayloadExtractorTests<TExtractor>
        where TExtractor : BaseRequestPayloadExtractor, new()
    {
        protected class Fixture
        {
            public HttpRequest HttpRequestCore { get; set; } = Substitute.For<HttpRequest>();
            public IHttpRequest HttpRequest { get; set; }
            public Stream Stream { get; set; } = Substitute.For<Stream>();

            public Fixture()
            {
                _ = Stream.CanSeek.Returns(true);
                _ = Stream.CanRead.Returns(true);
                HttpRequest = new HttpRequestAdapter(HttpRequestCore);
            }

            public TExtractor GetSut()
            {
                _ = HttpRequest.Body.Returns(Stream);
                return new TExtractor();
            }
        }

        protected Fixture TestFixture = new Fixture();

        [Fact]
        public void ExtractPayload_OriginalStreamPosition_Reset()
        {
            const int originalPosition = 100;
            _ = TestFixture.Stream.Position.Returns(originalPosition);

            var sut = TestFixture.GetSut();

            _ = sut.ExtractPayload(TestFixture.HttpRequest);

            TestFixture.Stream.Received().Position = originalPosition;
        }

        [Fact]
        public void ExtractPayload_OriginalStream_NotClosed()
        {
            var sut = TestFixture.GetSut();

            _ = sut.ExtractPayload(TestFixture.HttpRequest);

            TestFixture.Stream.DidNotReceive().Close();
        }

        [Fact]
        public void ExtractPayload_CantSeekStream_DoesNotChangePosition()
        {
            _ = TestFixture.Stream.CanSeek.Returns(false);

            var sut = TestFixture.GetSut();

            Assert.Null(sut.ExtractPayload(TestFixture.HttpRequest));

            TestFixture.Stream.DidNotReceive().Position = Arg.Any<long>();
        }

        [Fact]
        public void ExtractPayload_CantReadStream_DoesNotChangePosition()
        {
            _ = TestFixture.Stream.CanRead.Returns(false);

            var sut = TestFixture.GetSut();

            Assert.Null(sut.ExtractPayload(TestFixture.HttpRequest));

            TestFixture.Stream.DidNotReceive().Position = Arg.Any<long>();
        }
    }
}
