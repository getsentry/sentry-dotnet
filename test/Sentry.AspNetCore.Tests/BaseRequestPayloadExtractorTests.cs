using System.IO;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public abstract class BaseRequestPayloadExtractorTests<TExtractor>
        where TExtractor : BaseRequestPayloadExtractor, new()
    {
        protected class Fixture
        {
            public HttpRequest HttpRequest { get; set; } = Substitute.For<HttpRequest>();
            public Stream Stream { get; set; } = Substitute.For<Stream>();

            public Fixture()
            {
                Stream.CanSeek.Returns(true);
                Stream.CanRead.Returns(true);
            }

            public TExtractor GetSut()
            {
                HttpRequest.Body.Returns(Stream);
                return new TExtractor();
            }
        }

        protected Fixture TestFixture = new Fixture();

        [Fact]
        public void ExtractPayload_OriginalStreamPosition_Reset()
        {
            const int originalPosition = 100;
            TestFixture.Stream.Position.Returns(originalPosition);

            var sut = TestFixture.GetSut();

            sut.ExtractPayload(TestFixture.HttpRequest);

            TestFixture.Stream.Received().Position = originalPosition;
        }

        [Fact]
        public void ExtractPayload_OriginalStream_NotClosed()
        {
            var sut = TestFixture.GetSut();

            sut.ExtractPayload(TestFixture.HttpRequest);

            TestFixture.Stream.DidNotReceive().Close();
        }

        [Fact]
        public void ExtractPayload_CantSeakStream_DoesNotChangePosition()
        {
            TestFixture.Stream.CanSeek.Returns(false);

            var sut = TestFixture.GetSut();

            Assert.Null(sut.ExtractPayload(TestFixture.HttpRequest));

            TestFixture.Stream.DidNotReceive().Position = Arg.Any<int>();
        }

        [Fact]
        public void ExtractPayload_CantReadStream_DoesNotChangePosition()
        {
            TestFixture.Stream.CanRead.Returns(false);

            var sut = TestFixture.GetSut();

            Assert.Null(sut.ExtractPayload(TestFixture.HttpRequest));

            TestFixture.Stream.DidNotReceive().Position = Arg.Any<int>();
        }
    }
}
