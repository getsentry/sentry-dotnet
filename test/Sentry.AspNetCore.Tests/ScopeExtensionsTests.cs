using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class ScopeExtensionsTests
    {
        private readonly Scope _sut = new Scope(new SentryOptions());
        private readonly HttpContext _httpContext = Substitute.For<HttpContext>();
        private readonly HttpRequest _httpRequest = Substitute.For<HttpRequest>();
        private readonly IServiceProvider _provider = Substitute.For<IServiceProvider>();
        public SentryAspNetCoreOptions SentryAspNetCoreOptions { get; set; }
            = new SentryAspNetCoreOptions
            {
#pragma warning disable 618
                IncludeRequestPayload = true,
#pragma warning restore 618
                MaxRequestBodySize = RequestSize.Always
            };

        public ScopeExtensionsTests()
        {
            _httpContext.RequestServices.Returns(_provider);
            _httpContext.Request.Returns(_httpRequest);
        }

        [Fact]
        public void Populate_Request_Method_SetToScope()
        {
            const string expected = "method";
            _httpContext.Request.Method.Returns(expected);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Request.Method);
        }

        [Fact]
        public void Populate_Request_QueryString_SetToScope()
        {
            const string expected = "?query=bla&somethign=ble";
            _httpContext.Request.QueryString.Returns(new QueryString(expected));

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Request.QueryString);
        }

        [Fact]
        public void Populate_Request_Url_SetToScope()
        {
            const string expectedPath = "/request/path";
            _httpContext.Request.Path.Returns(new PathString(expectedPath));

            const string expectedHost = "host.com";
            _httpContext.Request.Host.Returns(new HostString(expectedHost));

            const string expectedScheme = "http";
            _httpContext.Request.Scheme.Returns(expectedScheme);

            const string expected = "http://host.com/request/path";

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Request.Url);
        }

        [Fact]
        public void Populate_Request_Url_UnsetsRequestPath()
        {
            const string expected = "/request/path";
            _sut.SetTag("RequestPath", expected);
            _httpContext.Request.Path.Returns(new PathString(expected));

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.False(_sut.Tags.ContainsKey("RequestPath"));
        }

        [Fact]
        public void Populate_Request_Url_IncludesPortWhenOnContext()
        {
            const string expectedPath = "/request/path";
            _httpContext.Request.Path.Returns(new PathString(expectedPath));

            const string expectedHost = "host.com:9000";
            _httpContext.Request.Host.Returns(new HostString(expectedHost));

            const string expectedScheme = "http";
            _httpContext.Request.Scheme.Returns(expectedScheme);

            const string expected = "http://host.com:9000/request/path";

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Request.Url);
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

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(headers[firstKey], _sut.Request.Headers[firstKey]);
            Assert.Equal(headers[secondKey], _sut.Request.Headers[secondKey]);
        }

        [Fact]
        public void Populate_ByDefaultRequestCookies_NotSetToScope()
        {
            const string firstKey = "Cookie";
            var headers = new HeaderDictionary
            {
                { firstKey, new StringValues("Cookies data") }
            };

            _httpRequest.Headers.Returns(headers);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Empty(_sut.Request.Headers);
        }

        [Fact]
        public void Populate_SendDefaultPiiTrueRequestCookies_SetToScope()
        {
            const string firstKey = "Cookie";
            var headers = new HeaderDictionary
            {
                { firstKey, new StringValues("Cookies data") }
            };

            _httpRequest.Headers.Returns(headers);

            SentryAspNetCoreOptions.SendDefaultPii = true;
            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(headers[firstKey], _sut.Request.Headers[firstKey]);
        }

        [Fact]
        public void Populate_RemoteIp_ByDefault_NotSetToEnv()
        {
            var connection = Substitute.For<ConnectionInfo>();
            connection.RemoteIpAddress.Returns(IPAddress.IPv6Loopback);
            _httpContext.Connection.Returns(connection);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.DoesNotContain("REMOTE_ADDR", _sut.Request.Env.Keys);
        }

        [Fact]
        public void Populate_RemoteIp_SendDefaultPiiTrue_SetToEnv()
        {
            const string expected = "::1";
            var connection = Substitute.For<ConnectionInfo>();
            connection.RemoteIpAddress.Returns(IPAddress.IPv6Loopback);
            _httpContext.Connection.Returns(connection);

            SentryAspNetCoreOptions.SendDefaultPii = true;
            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Request.Env["REMOTE_ADDR"]);
        }

        [Fact]
        public void Populate_LocalPort_SetToEnv()
        {
            const int expected = 1337;
            var connection = Substitute.For<ConnectionInfo>();
            connection.LocalPort.Returns(expected);
            _httpContext.Connection.Returns(connection);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

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

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Request.Env["SERVER_SOFTWARE"]);
        }

        [Fact]
        public void Populate_MachineName_SetToEnv()
        {
            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(Environment.MachineName, _sut.Request.Env["SERVER_NAME"]);
        }

        [Fact]
        public void Populate_NoAspNetCoreOptions_NoPayloadExtractors_NoBodyRead()
        {
            _sut.Populate(_httpContext, null);

            _httpContext.RequestServices
                .DidNotReceive()
                .GetService(typeof(IEnumerable<IRequestPayloadExtractor>));
        }

        [Fact]
        public void Populate_AspNetCoreOptionsSetTrue_NoPayloadExtractors_NoBodyRead()
        {
            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

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

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            extractor.Received(1).ExtractPayload(Arg.Any<IHttpRequest>());
        }

        [Theory]
        [MemberData(nameof(InvalidRequestBodies))]
        public void Populate_PayloadExtractors_DoesNotConsiderInvalidResponse(object expected)
        {
            var first = Substitute.For<IRequestPayloadExtractor>();
            first.ExtractPayload(Arg.Any<IHttpRequest>()).Returns(expected);
            _httpContext.RequestServices
                .GetService(typeof(IEnumerable<IRequestPayloadExtractor>))
                .Returns(new[] { first });

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            first.Received(1).ExtractPayload(Arg.Any<IHttpRequest>());

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
            first.ExtractPayload(Arg.Any<IHttpRequest>()).Returns(expected);
            var second = Substitute.For<IRequestPayloadExtractor>();
            _httpContext.RequestServices
                .GetService(typeof(IEnumerable<IRequestPayloadExtractor>))
                .Returns(new[] { first, second });

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            second.DidNotReceive().ExtractPayload(Arg.Any<IHttpRequest>());

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

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            first.Received(1).ExtractPayload(Arg.Any<IHttpRequest>());
            second.Received(1).ExtractPayload(Arg.Any<IHttpRequest>());
        }

        [Fact]
        public void Populate_TraceIdentifier_SetAsTag()
        {
            const string expected = "identifier";
            _httpContext.TraceIdentifier.Returns(expected);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Tags[nameof(HttpContext.TraceIdentifier)]);
        }

        [Fact]
        public void Populate_TraceIdentifier_WhenRequestIdMatch_NotSetAsTag()
        {
            const string expected = "identifier";
            _sut.SetTag("RequestId", expected);
            _httpContext.TraceIdentifier.Returns(expected);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.False(_sut.Tags.TryGetValue(nameof(HttpContext.TraceIdentifier), out _));
        }

        [Fact]
        public void Populate_TraceIdentifier_WhenRequestIdDoesNotMatch_SetAsTag()
        {
            const string expected = "identifier";
            _sut.SetTag("RequestId", "different identifier");
            _httpContext.TraceIdentifier.Returns(expected);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Tags[nameof(HttpContext.TraceIdentifier)]);
        }
    }
}
