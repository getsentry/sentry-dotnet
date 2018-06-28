using System.IO;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class DefaultRequestPayloadExtractorTests
    {
        private class Fixture
        {
            public HttpRequest HttpRequest { get; set; } = Substitute.For<HttpRequest>();
            public Stream Stream { get; set; } = Substitute.For<Stream>();

            public Fixture()
            {
                Stream.CanSeek.Returns(true);
                Stream.CanRead.Returns(true);
            }

            public DefaultRequestPayloadExtractor GetSut()
            {
                HttpRequest.Body.Returns(Stream);
                return new DefaultRequestPayloadExtractor();
            }
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void ExtractPayload_OriginalStreamPosition_Reset()
        {
            const int originalPosition = 100;
            _fixture.Stream.Position.Returns(originalPosition);

            var sut = _fixture.GetSut();

            sut.ExtractPayload(_fixture.HttpRequest);

            _fixture.Stream.Received().Position = originalPosition;
        }

        [Fact]
        public void ExtractPayload_OriginalStream_NotClosed()
        {
            var sut = _fixture.GetSut();

            sut.ExtractPayload(_fixture.HttpRequest);

            _fixture.Stream.DidNotReceive().Close();
        }

        [Fact]
        public void ExtractPayload_StringData_ReadCorrectly()
        {
            const string expected = "The request payload: éãüçó";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(expected);
            writer.Flush();
            _fixture.Stream = stream;

            var sut = _fixture.GetSut();

            var actual = sut.ExtractPayload(_fixture.HttpRequest);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ExtractPayload_CantSeakStream_DoesNotChangePosition()
        {
            _fixture.Stream.CanSeek.Returns(false);

            var sut = _fixture.GetSut();

            Assert.Null(sut.ExtractPayload(_fixture.HttpRequest));

            _fixture.Stream.DidNotReceive().Position = Arg.Any<int>();
        }

        [Fact]
        public void ExtractPayload_CantReadStream_DoesNotChangePosition()
        {
            _fixture.Stream.CanRead.Returns(false);

            var sut = _fixture.GetSut();

            Assert.Null(sut.ExtractPayload(_fixture.HttpRequest));

            _fixture.Stream.DidNotReceive().Position = Arg.Any<int>();
        }
    }
}
