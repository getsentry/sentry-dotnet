using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Sentry.Protocol;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class ScopeExtensionsTests
    {
        private readonly Scope _sut = new Scope();
        private readonly HttpContext _httpContext = Substitute.For<HttpContext>();
        private readonly HttpRequest _httpRequest = Substitute.For<HttpRequest>();
        private readonly IServiceProvider _provider = Substitute.For<IServiceProvider>();
        public SentryAspNetCoreOptions SentryAspNetCoreOptions { get; set; }
            = new SentryAspNetCoreOptions { IncludeRequestPayload = true };

        public ScopeExtensionsTests()
        {
            _httpContext.RequestServices.Returns(_provider);

            _httpContext.RequestServices
                .GetService(typeof(SentryAspNetCoreOptions))
                .Returns(SentryAspNetCoreOptions);

            _httpContext.Request.Returns(_httpRequest);
        }

        [Fact]
        public void Populate_Request_Method_SetToScope()
        {
            const string expected = "method";
            _httpContext.Request.Method.Returns(expected);

            _sut.Populate(_httpContext);

            Assert.Equal(expected, _sut.Request.Method);
        }

        [Fact]
        public void Populate_Request_QueryString_SetToScope()
        {
            const string expected = "?query=bla&somethign=ble";
            _httpContext.Request.QueryString.Returns(new QueryString(expected));

            _sut.Populate(_httpContext);

            Assert.Equal(expected, _sut.Request.QueryString);
        }

        [Fact]
        public void Populate_Request_Url_SetToScope()
        {
            const string expected = "/request/path";
            _httpContext.Request.Path.Returns(new PathString(expected));

            _sut.Populate(_httpContext);

            Assert.Equal(expected, _sut.Request.Url);
        }

        [Fact]
        public void Populate_Request_Url_UnsetsRequestPath()
        {
            const string expected = "/request/path";
            _sut.SetTag("RequestPath", expected);
            _httpContext.Request.Path.Returns(new PathString(expected));

            _sut.Populate(_httpContext);

            Assert.False(_sut.Tags.TryGetKey("RequestPath", out _));
        }

        [Fact]
        public void Populate_Request_Headers_SetToScope()
        {
            const string firstKey = "User-Agent";
            const string secondKey = "Accept-Encodingt";
            var headers = new HeaderDictionary
            {
                { firstKey, new StringValues("Mozilla/5.0") },
                { secondKey, new StringValues(new [] { "gzip", "deflate", "br" }) }
            };

            _httpRequest.Headers.Returns(headers);

            _sut.Populate(_httpContext);

            Assert.Equal(headers[firstKey], _sut.Request.Headers[firstKey]);
            Assert.Equal(headers[secondKey], _sut.Request.Headers[secondKey]);
        }

        [Fact]
        public void Populate_RemoteIp_SetToEnv()
        {
            const string expected = "::1";
            var connection = Substitute.For<ConnectionInfo>();
            connection.RemoteIpAddress.Returns(IPAddress.IPv6Loopback);
            _httpContext.Connection.Returns(connection);

            _sut.Populate(_httpContext);

            Assert.Equal(expected, _sut.Request.Env["REMOTE_ADDR"]);
        }

        [Fact]
        public void Populate_LocalPort_SetToEnv()
        {
            const int expected = 1337;
            var connection = Substitute.For<ConnectionInfo>();
            connection.LocalPort.Returns(expected);
            _httpContext.Connection.Returns(connection);

            _sut.Populate(_httpContext);

            Assert.Equal(expected.ToString(), _sut.Request.Env["SERVER_PORT"]);
        }

        [Fact]
        public void Populate_Server_SetToEnv()
        {
            const string expected = "kestrel.sentry";
            var response = Substitute.For<HttpResponse>();
            var header = new HeaderDictionary { { "Server", expected } };

            response.Headers.Returns(header);
            _httpContext.Response.Returns(response);

            _sut.Populate(_httpContext);

            Assert.Equal(expected, _sut.Request.Env["SERVER_SOFTWARE"]);
        }

        [Fact]
        public void Populate_MachineName_SetToEnv()
        {
            _sut.Populate(_httpContext);

            Assert.Equal(Environment.MachineName, _sut.Request.Env["SERVER_NAME"]);
        }

        [Fact]
        public void Populate_NoAspNetCoreOptions_NoPayloadExtractors_NoBodyRead()
        {
            _httpContext.RequestServices
                .GetService(typeof(SentryAspNetCoreOptions))
                .Returns(null);

            _sut.Populate(_httpContext);

            _httpContext.RequestServices
                .DidNotReceive()
                .GetService(typeof(IEnumerable<IRequestPayloadExtractor>));
        }

        [Fact]
        public void Populate_AspNetCoreOptionsSetTrue_NoPayloadExtractors_NoBodyRead()
        {
            _sut.Populate(_httpContext);

            _httpContext.RequestServices
                .Received(1)
                .GetService(typeof(IEnumerable<IRequestPayloadExtractor>));
        }

        [Fact]
        public void Populate_AspNetCoreOptionsSetTrue_PayloadExtractors_NoBodyRead()
        {
            var extractor = Substitute.For<IRequestPayloadExtractor>();
            _httpContext.RequestServices
                .GetService(typeof(IEnumerable<IRequestPayloadExtractor>))
                .Returns(new[] { extractor });

            _sut.Populate(_httpContext);

            extractor.Received(1).ExtractPayload(Arg.Any<HttpRequest>());
        }

        [Theory]
        [MemberData(nameof(InvalidRequestBodies))]
        public void Populate_PayloadExtractors_DoesNotConsiderInvalidResponse(object expected)
        {
            var first = Substitute.For<IRequestPayloadExtractor>();
            first.ExtractPayload(Arg.Any<HttpRequest>()).Returns(expected);
            _httpContext.RequestServices
                .GetService(typeof(IEnumerable<IRequestPayloadExtractor>))
                .Returns(new[] { first });

            _sut.Populate(_httpContext);

            first.Received(1).ExtractPayload(Arg.Any<HttpRequest>());

            Assert.Null(_sut.Request.Data);
        }

        public static IEnumerable<object[]> InvalidRequestBodies()
        {
            yield return new object[] { "" };
            yield return new object[] { null };
        }

        [Theory]
        [MemberData(nameof(ValidRequestBodies))]
        public void Populate_PayloadExtractors_StopsOnFirstDictionary(object expected)
        {
            var first = Substitute.For<IRequestPayloadExtractor>();
            first.ExtractPayload(Arg.Any<HttpRequest>()).Returns(expected);
            var second = Substitute.For<IRequestPayloadExtractor>();
            _httpContext.RequestServices
                .GetService(typeof(IEnumerable<IRequestPayloadExtractor>))
                .Returns(new[] { first, second });

            _sut.Populate(_httpContext);

            second.DidNotReceive().ExtractPayload(Arg.Any<HttpRequest>());

            Assert.Same(expected, _sut.Request.Data);
        }

        public static IEnumerable<object[]> ValidRequestBodies()
        {
            yield return new object[] { "string" };
            yield return new object[] { new { Anonymous = "object" } };
            yield return new object[] { new Dictionary<string, string> { { "key", "value" } } };
        }

        [Fact]
        public void Populate_PayloadExtractors_FirstReturnsNull_CallsSecond()
        {
            var first = Substitute.For<IRequestPayloadExtractor>();
            var second = Substitute.For<IRequestPayloadExtractor>();
            _httpContext.RequestServices
                .GetService(typeof(IEnumerable<IRequestPayloadExtractor>))
                .Returns(new[] { first, second });

            _sut.Populate(_httpContext);

            first.Received(1).ExtractPayload(Arg.Any<HttpRequest>());
            second.Received(1).ExtractPayload(Arg.Any<HttpRequest>());
        }

        [Fact]
        public void Populate_TraceIdentifier_SetAsTag()
        {
            const string expected = "identifier";
            _httpContext.TraceIdentifier.Returns(expected);

            _sut.Populate(_httpContext);

            Assert.Equal(expected, _sut.Tags[nameof(HttpContext.TraceIdentifier)]);
        }

        [Fact]
        public void Populate_TraceIdentifier_WhenRequestIdMatch_NotSetAsTag()
        {
            const string expected = "identifier";
            _sut.SetTag("RequestId", expected);
            _httpContext.TraceIdentifier.Returns(expected);

            _sut.Populate(_httpContext);

            Assert.False(_sut.Tags.TryGetValue(nameof(HttpContext.TraceIdentifier), out _));
        }

        [Fact]
        public void Populate_TraceIdentifier_WhenRequestIdDoesNotMatch_SetAsTag()
        {
            const string expected = "identifier";
            _sut.SetTag("RequestId", "different identifier");
            _httpContext.TraceIdentifier.Returns(expected);

            _sut.Populate(_httpContext);

            Assert.Equal(expected, _sut.Tags[nameof(HttpContext.TraceIdentifier)]);
        }
    }
}
