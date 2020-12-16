using System;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NSubstitute;
using Sentry.Internal.Http;
using Xunit;
using static System.Threading.CancellationToken;

namespace Sentry.Tests.Internals.Http
{
    public class GzipRequestBodyHandlerTests
    {
        private class Fixture
        {
            public HttpMessageHandler Handler { get; set; } = Substitute.For<HttpMessageHandler>();
            public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

            public HttpRequestMessage Message { get; set; }

            public const int MessageCharCount = 1000;

            public Fixture()
            {
                var uri = Dsn.Parse(DsnSamples.ValidDsnWithSecret).GetStoreEndpointUri();

                Message = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = new StringContent(new string('a', MessageCharCount))
                };
            }

            public HttpMessageInvoker GetSut()
                => new(new GzipRequestBodyHandler(Handler, CompressionLevel));
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public async Task SendAsync_Content_Compressed()
        {
            var sut = _fixture.GetSut();

            _ = await sut.SendAsync(_fixture.Message, None);

            var gzippedContent = await _fixture.Message.Content.ReadAsByteArrayAsync();
            Assert.True(gzippedContent.Length < 100);
        }

        [Fact]
        public async Task SendAsync_Content_ReplacedWithGzipContent()
        {
            var sut = _fixture.GetSut();

            _ = await sut.SendAsync(_fixture.Message, None);

            _ = Assert.IsType<GzipRequestBodyHandler.GzipContent>(_fixture.Message.Content);
        }

        [Fact]
        public async Task SendAsync_Headers_CopiedOver()
        {
            _fixture.Message.Content.Headers.Add("test", new[] { "val1", "val2" });

            var sut = _fixture.GetSut();

            _ = await sut.SendAsync(_fixture.Message, None);

            Assert.Contains(_fixture.Message.Content.Headers,
                p => p.Key == "test" && p.Value.Count() == 2);
        }

        [Fact]
        public async Task SendAsync_ContentType_Gzip()
        {
            var sut = _fixture.GetSut();

            _ = await sut.SendAsync(_fixture.Message, None);

            Assert.Equal("gzip", _fixture.Message.Content.Headers.ContentEncoding.First());
        }

        [Fact]
        public void Ctor_NoCompression_ThrowsInvalidOperationException()
        {
            _ = Assert.Throws<InvalidOperationException>(
                    () => new GzipRequestBodyHandler(Substitute.For<HttpMessageHandler>(),
                        CompressionLevel.NoCompression));
        }
    }
}
