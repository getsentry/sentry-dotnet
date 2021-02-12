using System;
using System.IO;
using System.Web;
using NSubstitute;
using Sentry.AspNet.Internal;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.AspNet.Tests.Internal
{
    public class SystemWebRequestEventProcessorTests
    {
        private class Fixture
        {
            public IRequestPayloadExtractor RequestPayloadExtractor { get; set; } = Substitute.For<IRequestPayloadExtractor>();
            public SentryOptions SentryOptions { get; set; } = new();
            public object MockBody { get; set; } = new();
            public HttpContext HttpContext { get; set; }

            public Fixture()
            {
                _ = RequestPayloadExtractor.ExtractPayload(Arg.Any<IHttpRequest>()).Returns(MockBody);
                HttpContext = new HttpContext(new HttpRequest("test", "http://test", null), new HttpResponse(new StringWriter()));
            }

            public SystemWebRequestEventProcessor GetSut()
            {
                HttpContext.Current = HttpContext;
                return new SystemWebRequestEventProcessor(RequestPayloadExtractor, SentryOptions);
            }
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void Ctor_NullEvent_ThrowsArgumentNullException()
        {
            _fixture.RequestPayloadExtractor = null;
            _ = Assert.Throws<ArgumentNullException>(() => _fixture.GetSut());
        }

        [Fact]
        public void Process_NullEvent_ReturnsNull() => Assert.Null(_fixture.GetSut().Process(null));

        [Fact]
        public void Process_DefaultFixture_ReadsMockBody()
        {
            var expected = new SentryEvent();

            var sut = _fixture.GetSut();

            var actual = sut.Process(expected);
            Assert.Same(expected, actual);
            Assert.Same(_fixture.MockBody, expected.Request.Data);
        }

        [Fact]
        public void Process_NoHttpContext_NoRequestData()
        {
            _fixture.HttpContext = null;
            var expected = new SentryEvent();

            var sut = _fixture.GetSut();

            var actual = sut.Process(expected);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void Process_NoBodyExtracted_NoRequestData()
        {
            _ = _fixture.RequestPayloadExtractor.ExtractPayload(Arg.Any<IHttpRequest>()).Returns(null);
            var expected = new SentryEvent();

            var sut = _fixture.GetSut();

            var actual = sut.Process(expected);
            Assert.Same(expected, actual);
            Assert.Null(expected.Request.Data);
        }
    }
}
