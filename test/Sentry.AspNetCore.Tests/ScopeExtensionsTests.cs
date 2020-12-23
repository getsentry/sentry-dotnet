using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class ScopeExtensionsTests
    {
        private readonly Scope _sut = new(new SentryOptions());
        private readonly HttpContext _httpContext = Substitute.For<HttpContext>();
        private readonly HttpRequest _httpRequest = Substitute.For<HttpRequest>();
        private readonly IServiceProvider _provider = Substitute.For<IServiceProvider>();
        public SentryAspNetCoreOptions SentryAspNetCoreOptions { get; set; }
            = new()
            {
                MaxRequestBodySize = RequestSize.Always
            };

        public ScopeExtensionsTests()
        {
            _ = _httpContext.RequestServices.Returns(_provider);
            _ = _httpContext.Request.Returns(_httpRequest);
        }

        [Fact]
        public void Populate_Request_Method_SetToScope()
        {
            const string expected = "method";
            _ = _httpContext.Request.Method.Returns(expected);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Request.Method);
        }

        [Fact]
        public void Populate_Request_QueryString_SetToScope()
        {
            const string expected = "?query=bla&something=ble";
            _ = _httpContext.Request.QueryString.Returns(new QueryString(expected));

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Request.QueryString);
        }

        [Fact]
        public void Populate_Request_Url_SetToScope()
        {
            const string expectedPath = "/request/path";
            _ = _httpContext.Request.Path.Returns(new PathString(expectedPath));

            const string expectedHost = "host.com";
            _ = _httpContext.Request.Host.Returns(new HostString(expectedHost));

            const string expectedScheme = "http";
            _ = _httpContext.Request.Scheme.Returns(expectedScheme);

            const string expected = "http://host.com/request/path";

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Request.Url);
        }

        [Fact]
        public void Populate_Request_Url_UnsetsRequestPath()
        {
            const string expected = "/request/path";
            _sut.SetTag("RequestPath", expected);
            _ = _httpContext.Request.Path.Returns(new PathString(expected));

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.False(_sut.Tags.ContainsKey("RequestPath"));
        }

        [Fact]
        public void Populate_Request_Url_IncludesPortWhenOnContext()
        {
            const string expectedPath = "/request/path";
            _ = _httpContext.Request.Path.Returns(new PathString(expectedPath));

            const string expectedHost = "host.com:9000";
            _ = _httpContext.Request.Host.Returns(new HostString(expectedHost));

            const string expectedScheme = "http";
            _ = _httpContext.Request.Scheme.Returns(expectedScheme);

            const string expected = "http://host.com:9000/request/path";

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Request.Url);
        }

        [Fact]
        public void Populate_Request_Headers_SetToScope()
        {
            const string firstKey = "User-Agent";
            const string secondKey = "Accept-Encoding";
            var headers = new HeaderDictionary
            {
                { firstKey, new StringValues("Mozilla/5.0") },
                { secondKey, new StringValues(new [] { "gzip", "deflate", "br" }) }
            };

            _ = _httpRequest.Headers.Returns(headers);

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

            _ = _httpRequest.Headers.Returns(headers);

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

            _ = _httpRequest.Headers.Returns(headers);

            SentryAspNetCoreOptions.SendDefaultPii = true;
            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(headers[firstKey], _sut.Request.Headers[firstKey]);
        }

        [Fact]
        public void Populate_RemoteIp_ByDefault_NotSetToEnv()
        {
            var connection = Substitute.For<ConnectionInfo>();
            _ = connection.RemoteIpAddress.Returns(IPAddress.IPv6Loopback);
            _ = _httpContext.Connection.Returns(connection);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.DoesNotContain("REMOTE_ADDR", _sut.Request.Env.Keys);
        }

        [Fact]
        public void Populate_RemoteIp_SendDefaultPiiTrue_SetToEnv()
        {
            const string expected = "::1";
            var connection = Substitute.For<ConnectionInfo>();
            _ = connection.RemoteIpAddress.Returns(IPAddress.IPv6Loopback);
            _ = _httpContext.Connection.Returns(connection);

            SentryAspNetCoreOptions.SendDefaultPii = true;
            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Request.Env["REMOTE_ADDR"]);
        }

        [Fact]
        public void Populate_LocalPort_SetToEnv()
        {
            const int expected = 1337;
            var connection = Substitute.For<ConnectionInfo>();
            _ = connection.LocalPort.Returns(expected);
            _ = _httpContext.Connection.Returns(connection);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected.ToString(), _sut.Request.Env["SERVER_PORT"]);
        }

        [Fact]
        public void Populate_Server_SetToEnv()
        {
            const string expected = "kestrel.sentry";
            var response = Substitute.For<HttpResponse>();
            var header = new HeaderDictionary { { "Server", expected } };

            _ = response.Headers.Returns(header);
            _ = _httpContext.Response.Returns(response);

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

            _ = _httpContext.RequestServices
                    .DidNotReceive()
                    .GetService(typeof(IEnumerable<IRequestPayloadExtractor>));
        }

        [Fact]
        public void Populate_AspNetCoreOptionsSetTrue_NoPayloadExtractors_NoBodyRead()
        {
            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            _ = _httpContext.RequestServices
                    .Received(1)
                    .GetService(typeof(IEnumerable<IRequestPayloadExtractor>));
        }

        [Fact]
        public void Populate_AspNetCoreOptionsSetTrue_PayloadExtractors_NoBodyRead()
        {
            var extractor = Substitute.For<IRequestPayloadExtractor>();
            _ = _httpContext.RequestServices
                    .GetService(typeof(IEnumerable<IRequestPayloadExtractor>))
                    .Returns(new[] { extractor });

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            _ = extractor.Received(1).ExtractPayload(Arg.Any<IHttpRequest>());
        }

        [Theory]
        [MemberData(nameof(InvalidRequestBodies))]
        public void Populate_PayloadExtractors_DoesNotConsiderInvalidResponse(object expected)
        {
            var first = Substitute.For<IRequestPayloadExtractor>();
            _ = first.ExtractPayload(Arg.Any<IHttpRequest>()).Returns(expected);
            _ = _httpContext.RequestServices
                    .GetService(typeof(IEnumerable<IRequestPayloadExtractor>))
                    .Returns(new[] { first });

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            _ = first.Received(1).ExtractPayload(Arg.Any<IHttpRequest>());

            Assert.Null(_sut.Request.Data);
        }

        [Fact]
        public void Populate_RouteData_SetToScope()
        {
            const string controller = "Ctrl";
            const string action = "Actn";
            var routeFeature = new RoutingFeature()
            {
                RouteData = new RouteData() {Values = {{"controller", controller}, {"action", action},}}
            };
            var features = new FeatureCollection();
            features.Set<IRoutingFeature>(routeFeature);
            _ = _httpContext.Features.Returns(features);
            _ = _httpContext.Request.Method.Returns("GET");

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal($"GET {controller}.{action}", _sut.TransactionName);
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
            _ = first.ExtractPayload(Arg.Any<IHttpRequest>()).Returns(expected);
            var second = Substitute.For<IRequestPayloadExtractor>();
            _ = _httpContext.RequestServices
                    .GetService(typeof(IEnumerable<IRequestPayloadExtractor>))
                    .Returns(new[] { first, second });

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            _ = second.DidNotReceive().ExtractPayload(Arg.Any<IHttpRequest>());

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
            _ = _httpContext.RequestServices
                    .GetService(typeof(IEnumerable<IRequestPayloadExtractor>))
                    .Returns(new[] { first, second });

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            _ = first.Received(1).ExtractPayload(Arg.Any<IHttpRequest>());
            _ = second.Received(1).ExtractPayload(Arg.Any<IHttpRequest>());
        }

        [Fact]
        public void Populate_TraceIdentifier_SetAsTag()
        {
            const string expected = "identifier";
            _ = _httpContext.TraceIdentifier.Returns(expected);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Tags[nameof(HttpContext.TraceIdentifier)]);
        }

        [Fact]
        public void Populate_TraceIdentifier_WhenRequestIdMatch_NotSetAsTag()
        {
            const string expected = "identifier";
            _sut.SetTag("RequestId", expected);
            _ = _httpContext.TraceIdentifier.Returns(expected);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.False(_sut.Tags.TryGetValue(nameof(HttpContext.TraceIdentifier), out _));
        }

        [Fact]
        public void Populate_TraceIdentifier_WhenRequestIdDoesNotMatch_SetAsTag()
        {
            const string expected = "identifier";
            _sut.SetTag("RequestId", "different identifier");
            _ = _httpContext.TraceIdentifier.Returns(expected);

            _sut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _sut.Tags[nameof(HttpContext.TraceIdentifier)]);
        }
    }
}
