using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Sentry.AspNetCore.Tests;

public partial class ScopeExtensionsTests
{
    private readonly IDiagnosticLogger _logger;
    private readonly Scope _sut = new(new SentryOptions());
    private readonly HttpContext _httpContext = Substitute.For<HttpContext>();
    private readonly HttpRequest _httpRequest = Substitute.For<HttpRequest>();
    private readonly IServiceProvider _provider = Substitute.For<IServiceProvider>();

    public SentryAspNetCoreOptions SentryAspNetCoreOptions { get; }

    public ScopeExtensionsTests(ITestOutputHelper output)
    {
        _httpContext.RequestServices.Returns(_provider);
        _httpContext.Request.Returns(_httpRequest);

        _logger = Substitute.ForPartsOf<TestOutputDiagnosticLogger>(output);

        SentryAspNetCoreOptions = new()
        {
            MaxRequestBodySize = RequestSize.Always,
            Debug = true,
            DiagnosticLogger = _logger
        };
    }

    private class Fixture
    {
        public const string ControllerName = "Ctrl";
        public const string ActionName = "Actn";

        public readonly Scope Scope = new(new SentryOptions());
        public HttpContext HttpContext { get; } = Substitute.For<HttpContext>();

        public Fixture GetSut(bool addTransaction = true)
        {
            if (addTransaction)
            {
                Scope.Transaction = Substitute.For<ITransactionTracer>();
            }

            var routeFeature = new RoutingFeature
            {
                RouteData = new RouteData
                {
                    Values =
                    {
                        {"controller", ControllerName},
                        {"action", ActionName}
                    }
                }
            };
            var features = new FeatureCollection();
            features.Set<IRoutingFeature>(routeFeature);
            HttpContext.Features.Returns(features);
            HttpContext.Request.Method.Returns("GET");
            return this;
        }

        public Fixture GetSutWithEmptyRoute(bool addTransaction = true)
        {
            if (addTransaction)
            {
                Scope.Transaction = Substitute.For<ITransactionTracer>();
            }
            var routeFeature = new RoutingFeature
            {
                RouteData = new RouteData
                {
                    Values = { { "", null } }
                }
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
        _httpContext.Request.Method.Returns(expected);

        _sut.Populate(_httpContext, SentryAspNetCoreOptions);

        Assert.Equal(expected, _sut.Request.Method);
    }

    [Fact]
    public void Populate_Request_QueryString_SetToScope()
    {
        const string expected = "?query=bla&something=ble";
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
        const string secondKey = "Accept-Encoding";
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

        _sut.Request.Cookies.Should().Be(headers[firstKey]);
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
        var extractor = Substitute.For<IRequestPayloadExtractor>();
        if (expected is Exception exception)
        {
            extractor.ExtractPayload(Arg.Any<IHttpRequest>()).Throws(exception);
        }
        else
        {
            extractor.ExtractPayload(Arg.Any<IHttpRequest>()).Returns(expected);
        }

#if NET5_0_OR_GREATER
        if (expected is BadHttpRequestException)
        {
            _httpContext.RequestAborted = new CancellationToken(canceled: true);
        }
#endif

        _httpContext.RequestServices
            .GetService(typeof(IEnumerable<IRequestPayloadExtractor>))
            .Returns(new[] { extractor });

        _sut.Populate(_httpContext, SentryAspNetCoreOptions);

        extractor.Received(1).ExtractPayload(Arg.Any<IHttpRequest>());

        if (_httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.Received().Log(SentryLevel.Debug, "Failed to extract body because the request was aborted.");
        }
        else if (expected is Exception ex)
        {
            _logger.Received().Log(SentryLevel.Error, "Failed to extract body.", ex);
        }

        Assert.Null(_sut.Request.Data);
    }

    public static IEnumerable<object[]> InvalidRequestBodies()
    {
        yield return new object[] { "" };
        yield return new object[] { null };
        yield return new object[] {new Exception()};
#if NET5_0_OR_GREATER
        yield return new object[] { new BadHttpRequestException("Unexpected end of request content.") };
#endif
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

    [Fact]
    public void Populate_TransactionAndTransactionNameIsNull_TransactionNameReplaced()
    {
        // Arrange
        var sut = _fixture.GetSut(addTransaction: false);
        var scope = sut.Scope;
        var expectedTransactionName = $"GET {Fixture.ControllerName}.{Fixture.ActionName}";

        // Act
        scope.Populate(_fixture.HttpContext, SentryAspNetCoreOptions);

        // Assert
        Assert.Equal(expectedTransactionName, scope.TransactionName);
    }

    [Fact]
    public void Populate_TransactionIsNullAndRouteNotFound_TransactionNameAsNull()
    {
        // Arrange
        var sut = _fixture.GetSutWithEmptyRoute(addTransaction: false);
        var scope = sut.Scope;

        // Act
        scope.Populate(_fixture.HttpContext, SentryAspNetCoreOptions);

        // Assert
        Assert.Null(scope.TransactionName);
    }

    [Fact]
    public void Populate_TransactionNameSet_TransactionNameSkipped()
    {
        // Arrange
        var sut = _fixture.GetSut();
        var scope = sut.Scope;
        var expectedRoute = "MyRoute";
        scope.Transaction.Name = expectedRoute;

        // Act
        scope.Populate(_fixture.HttpContext, SentryAspNetCoreOptions);

        // Assert
        Assert.Equal(expectedRoute, scope.TransactionName);
    }
}
