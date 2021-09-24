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
        private readonly Scope _scopeSut = new(new SentryOptions());
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

        private class Fixture
        {
            public readonly Scope Scope = new(new SentryOptions());

            public HttpContext HttpContext { get; set; } = Substitute.For<HttpContext>();

            public const string ControllerName = "Ctrl";
            public const string ActionName = "Actn";

            public Fixture GetSut()
            {
                var routeFeature = new RoutingFeature
                {
                    RouteData = new RouteData { Values = { { "controller", ControllerName }, { "action", ActionName }, } }
                };
                var features = new FeatureCollection();
                features.Set<IRoutingFeature>(routeFeature);
                HttpContext.Features.Returns(features);
                HttpContext.Request.Method.Returns("GET");
                return this;
            }

            public Fixture GetSutWithEmptyRoute()
            {
                var routeFeature = new RoutingFeature
                {
                    RouteData = new RouteData{ Values = {} }
                };
                var features = new FeatureCollection();
                features.Set<IRoutingFeature>(routeFeature);
                HttpContext.Features.Returns(features);
                HttpContext.Request.Method.Returns("GET");
                return this;
            }
        }

       private readonly Fixture _fixture = new();

        [Fact]
        public void Populate_Request_Method_SetToScope()
        {
            const string expected = "method";
            _ = _httpContext.Request.Method.Returns(expected);

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _scopeSut.Request.Method);
        }

        [Fact]
        public void Populate_Request_QueryString_SetToScope()
        {
            const string expected = "?query=bla&something=ble";
            _ = _httpContext.Request.QueryString.Returns(new QueryString(expected));

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _scopeSut.Request.QueryString);
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

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _scopeSut.Request.Url);
        }

        [Fact]
        public void Populate_Request_Url_UnsetsRequestPath()
        {
            const string expected = "/request/path";
            _scopeSut.SetTag("RequestPath", expected);
            _ = _httpContext.Request.Path.Returns(new PathString(expected));

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.False(_scopeSut.Tags.ContainsKey("RequestPath"));
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

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _scopeSut.Request.Url);
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

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(headers[firstKey], _scopeSut.Request.Headers[firstKey]);
            Assert.Equal(headers[secondKey], _scopeSut.Request.Headers[secondKey]);
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

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Empty(_scopeSut.Request.Headers);
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
            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(headers[firstKey], _scopeSut.Request.Headers[firstKey]);
        }

        [Fact]
        public void Populate_RemoteIp_ByDefault_NotSetToEnv()
        {
            var connection = Substitute.For<ConnectionInfo>();
            _ = connection.RemoteIpAddress.Returns(IPAddress.IPv6Loopback);
            _ = _httpContext.Connection.Returns(connection);

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.DoesNotContain("REMOTE_ADDR", _scopeSut.Request.Env.Keys);
        }

        [Fact]
        public void Populate_RemoteIp_SendDefaultPiiTrue_SetToEnv()
        {
            const string expected = "::1";
            var connection = Substitute.For<ConnectionInfo>();
            _ = connection.RemoteIpAddress.Returns(IPAddress.IPv6Loopback);
            _ = _httpContext.Connection.Returns(connection);

            SentryAspNetCoreOptions.SendDefaultPii = true;
            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _scopeSut.Request.Env["REMOTE_ADDR"]);
        }

        [Fact]
        public void Populate_LocalPort_SetToEnv()
        {
            const int expected = 1337;
            var connection = Substitute.For<ConnectionInfo>();
            _ = connection.LocalPort.Returns(expected);
            _ = _httpContext.Connection.Returns(connection);

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected.ToString(), _scopeSut.Request.Env["SERVER_PORT"]);
        }

        [Fact]
        public void Populate_Server_SetToEnv()
        {
            const string expected = "kestrel.sentry";
            var response = Substitute.For<HttpResponse>();
            var header = new HeaderDictionary { { "Server", expected } };

            _ = response.Headers.Returns(header);
            _ = _httpContext.Response.Returns(response);

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _scopeSut.Request.Env["SERVER_SOFTWARE"]);
        }

        [Fact]
        public void Populate_MachineName_SetToEnv()
        {
            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(Environment.MachineName, _scopeSut.Request.Env["SERVER_NAME"]);
        }

        [Fact]
        public void Populate_NoAspNetCoreOptions_NoPayloadExtractors_NoBodyRead()
        {
            _scopeSut.Populate(_httpContext, null);

            _ = _httpContext.RequestServices
                    .DidNotReceive()
                    .GetService(typeof(IEnumerable<IRequestPayloadExtractor>));
        }

        [Fact]
        public void Populate_AspNetCoreOptionsSetTrue_NoPayloadExtractors_NoBodyRead()
        {
            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

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

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

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

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            _ = first.Received(1).ExtractPayload(Arg.Any<IHttpRequest>());

            Assert.Null(_scopeSut.Request.Data);
        }

        [Fact]
        public void Populate_RouteData_SetToScope()
        {
            const string controller = "Ctrl";
            const string action = "Actn";
            var routeFeature = new RoutingFeature()
            {
                RouteData = new RouteData() { Values = { { "controller", controller }, { "action", action }, } }
            };
            var features = new FeatureCollection();
            features.Set<IRoutingFeature>(routeFeature);
            _ = _httpContext.Features.Returns(features);
            _ = _httpContext.Request.Method.Returns("GET");

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal($"GET {controller}.{action}", _scopeSut.TransactionName);
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

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            _ = second.DidNotReceive().ExtractPayload(Arg.Any<IHttpRequest>());

            Assert.Same(expected, _scopeSut.Request.Data);
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

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            _ = first.Received(1).ExtractPayload(Arg.Any<IHttpRequest>());
            _ = second.Received(1).ExtractPayload(Arg.Any<IHttpRequest>());
        }

        [Fact]
        public void Populate_TraceIdentifier_SetAsTag()
        {
            const string expected = "identifier";
            _ = _httpContext.TraceIdentifier.Returns(expected);

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _scopeSut.Tags[nameof(HttpContext.TraceIdentifier)]);
        }

        [Fact]
        public void Populate_TraceIdentifier_WhenRequestIdMatch_NotSetAsTag()
        {
            const string expected = "identifier";
            _scopeSut.SetTag("RequestId", expected);
            _ = _httpContext.TraceIdentifier.Returns(expected);

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.False(_scopeSut.Tags.TryGetValue(nameof(HttpContext.TraceIdentifier), out _));
        }

        [Fact]
        public void Populate_TraceIdentifier_WhenRequestIdDoesNotMatch_SetAsTag()
        {
            const string expected = "identifier";
            _scopeSut.SetTag("RequestId", "different identifier");
            _ = _httpContext.TraceIdentifier.Returns(expected);

            _scopeSut.Populate(_httpContext, SentryAspNetCoreOptions);

            Assert.Equal(expected, _scopeSut.Tags[nameof(HttpContext.TraceIdentifier)]);
        }

        [Fact]
        public void Populate_TransactionNameIsNull_TransactionNameReplaced()
        {
            // Arrange
            var sut = _fixture.GetSut();
            var scope = sut.Scope;
            scope.TransactionName = null;
            var expectedTransactionName = $"GET {Fixture.ControllerName}.{Fixture.ActionName}";

            // Act
            scope.Populate(_fixture.HttpContext, SentryAspNetCoreOptions);

            // Assert
            Assert.Equal(expectedTransactionName, scope.TransactionName);
        }

        [Fact]
        public void Populate_TransactionNameIsNullAndRouteNotFound_TransactionNameAsUnknownRoute()
        {
            // Arrange
            var sut = _fixture.GetSutWithEmptyRoute();
            var scope = sut.Scope;
            scope.TransactionName = null;

            // Act
            scope.Populate(_fixture.HttpContext, SentryAspNetCoreOptions);

            // Assert
            Assert.Equal(SentryTracingMiddleware.UnknownRouteTransactionName, scope.TransactionName);
        }

        [Fact]
        public void Populate_TransactionNameIsUnknown_TransactionNameReplaced()
        {
            // Arrange
            var sut = _fixture.GetSut();
            var scope = sut.Scope;
            scope.TransactionName = SentryTracingMiddleware.UnknownRouteTransactionName;
            var expectedTransactionName = $"GET {Fixture.ControllerName}.{Fixture.ActionName}";

            // Act
            scope.Populate(_fixture.HttpContext, SentryAspNetCoreOptions);

            // Assert
            Assert.Equal(expectedTransactionName, scope.TransactionName);
        }

        [Fact]
        public void Populate_TransactionNameSet_TransactionNameSkipped()
        {
            // Arrange
            var sut = _fixture.GetSut();
            var scope = sut.Scope;
            var expectedRoute = "MyRoute";
            scope.TransactionName = expectedRoute;

            // Act
            scope.Populate(_fixture.HttpContext, SentryAspNetCoreOptions);

            // Assert
            Assert.Equal(expectedRoute, scope.TransactionName);
        }
    }
}
