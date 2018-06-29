using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Sentry.Protocol;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class SentryMiddlewareTests
    {
        private class Fixture
        {
            public RequestDelegate RequestDelegate { get; set; } = _ => Task.CompletedTask;
            public IHub Hub { get; set; } = Substitute.For<IHub>();
            public SentryAspNetCoreOptions Options { get; set; } = new SentryAspNetCoreOptions();
            public IHostingEnvironment HostingEnvironment { get; set; } = Substitute.For<IHostingEnvironment>();
            public ILogger<SentryMiddleware> Logger { get; set; } = Substitute.For<ILogger<SentryMiddleware>>();
            public HttpContext HttpContext { get; set; } = Substitute.For<HttpContext>();
            public IFeatureCollection FeatureCollection { get; set; } = Substitute.For<IFeatureCollection>();

            public Fixture() => HttpContext.Features.Returns(FeatureCollection);

            public SentryMiddleware GetSut()
                => new SentryMiddleware(
                    RequestDelegate,
                    Hub,
                    Options,
                    HostingEnvironment,
                    Logger);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public async Task InvokeAsync_OnlyCtorRequiredArguments_InvokesNextHandlers()
        {
            _fixture.Options = null;
            _fixture.HostingEnvironment = null;
            _fixture.Logger = null;
            _fixture.RequestDelegate = Substitute.For<RequestDelegate>();

            var sut = _fixture.GetSut();

            await sut.InvokeAsync(_fixture.HttpContext);
            await _fixture.RequestDelegate.Received(1).Invoke(_fixture.HttpContext);
        }

        [Fact]
        public async Task InvokeAsync_ExceptionThrown_SameRethrown()
        {
            var expected = new Exception("test");
            _fixture.RequestDelegate = _ => throw expected;

            var sut = _fixture.GetSut();

            var actual = await Assert.ThrowsAsync<Exception>(
                async () => await sut.InvokeAsync(_fixture.HttpContext));

            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task InvokeAsync_OnlyCtorRequiredArguments_CapturesEventOnError()
        {
            _fixture.Options = null;
            _fixture.HostingEnvironment = null;
            _fixture.Logger = null;
            var expected = new Exception("test");
            _fixture.RequestDelegate = _ => throw expected;

            var sut = _fixture.GetSut();

            await Assert.ThrowsAsync<Exception>(
                async () => await sut.InvokeAsync(_fixture.HttpContext));

            _fixture.Hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public async Task InvokeAsync_OnlyCtorRequiredArguments_CapturesEventOnExceptionHandlerFeatureError()
        {
            _fixture.Options = null;
            _fixture.HostingEnvironment = null;
            _fixture.Logger = null;
            var expected = new Exception("test");
            var feature = Substitute.For<IExceptionHandlerFeature>();
            feature.Error.Returns(expected);
            _fixture.HttpContext.Features.Get<IExceptionHandlerFeature>().Returns(feature);

            var sut = _fixture.GetSut();

            await sut.InvokeAsync(_fixture.HttpContext);

            _fixture.Hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e => e.SentryExceptions.Values.Single().Value == expected.Message));
        }

        [Fact]
        public async Task InvokeAsync_FeatureFoundWithNoError_DoesNotCapturesEvent()
        {
            var feature = Substitute.For<IExceptionHandlerFeature>();
            feature.Error.ReturnsNull();
            _fixture.HttpContext.Features.Get<IExceptionHandlerFeature>().Returns(feature);

            var sut = _fixture.GetSut();

            await sut.InvokeAsync(_fixture.HttpContext);

            _fixture.Hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public async Task InvokeAsync_ScopePushed_BeforeConfiguringScope()
        {
            var scopePushed = false;
            _fixture.Hub.When(h => h.PushScope()).Do(_ => scopePushed = true);
            _fixture.Hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(c => Assert.True(scopePushed));

            var sut = _fixture.GetSut();

            await sut.InvokeAsync(_fixture.HttpContext);

            _fixture.Hub.Received().ConfigureScope(Arg.Any<Action<Scope>>());
        }

        [Fact]
        public async Task InvokeAsync_LocksScope_BeforeConfiguringScope()
        {
            var verified = false;
            var scope = new Scope();
            _fixture.Hub
                .When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(Callback
                    .First(c => c.ArgAt<Action<Scope>>(0)(scope))
                    .Then(c =>
                    {
                        Assert.True(scope.Locked);
                        verified = true;
                    }));

            var sut = _fixture.GetSut();

            await sut.InvokeAsync(_fixture.HttpContext);

            Assert.True(verified);
        }

        [Fact]
        public async Task InvokeAsync_OnEvaluating_HttpContextDataSet()
        {
            const string expectedTraceIdentifier = "trace id";
            _fixture.HttpContext.TraceIdentifier.Returns(expectedTraceIdentifier);
            var scope = new Scope();
            _fixture.Hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(c => c.Arg<Action<Scope>>()(scope));

            var sut = _fixture.GetSut();

            await sut.InvokeAsync(_fixture.HttpContext);

            scope.Evaluate();

            Assert.Equal(expectedTraceIdentifier, scope.Tags[nameof(HttpContext.TraceIdentifier)]);
        }

        [Fact]
        public async Task InvokeAsync_ScopePushedAndPoped_OnHappyPath()
        {
            var disposable = Substitute.For<IDisposable>();
            _fixture.Hub.PushScope().Returns(disposable);

            var sut = _fixture.GetSut();

            await sut.InvokeAsync(_fixture.HttpContext);

            _fixture.Hub.Received(1).PushScope();
            disposable.Received(1).Dispose();
        }

        [Fact]
        public async Task InvokeAsync_ScopePushedAndPoped_OnError()
        {
            var expected = new Exception("test");
            _fixture.RequestDelegate = _ => throw expected;
            var disposable = Substitute.For<IDisposable>();
            _fixture.Hub.PushScope().Returns(disposable);

            var sut = _fixture.GetSut();

            await Assert.ThrowsAsync<Exception>(
                async () => await sut.InvokeAsync(_fixture.HttpContext));

            _fixture.Hub.Received(1).PushScope();
            disposable.Received(1).Dispose();
        }

        [Fact]
        public void PopulateScope_NoHostingEnvironment_NoEnvironmentSet()
        {
            _fixture.HostingEnvironment = null;
            var scope = new Scope();

            var sut = _fixture.GetSut();
            sut.PopulateScope(_fixture.HttpContext, scope);

            Assert.Null(scope.Environment);
        }

        [Fact]
        public void PopulateScope_NoHostingEnvironment_NoWebRootSet()
        {
            _fixture.HostingEnvironment = null;
            var scope = new Scope();

            var sut = _fixture.GetSut();
            sut.PopulateScope(_fixture.HttpContext, scope);

            Assert.False(scope.Request.Env.TryGetKey("DOCUMENT_ROOT", out _));
        }

        [Fact]
        public void PopulateScope_WithHostingEnvironment_WebRootSet()
        {
            const string expectedWebRoot = "root";
            _fixture.HostingEnvironment.WebRootPath.Returns(expectedWebRoot);
            var scope = new Scope();

            var sut = _fixture.GetSut();
            sut.PopulateScope(_fixture.HttpContext, scope);

            Assert.True(scope.Request.Env.TryGetValue("DOCUMENT_ROOT", out var actualWebRoot));
            Assert.Equal(expectedWebRoot, actualWebRoot);
        }

        [Fact]
        public void PopulateScope_WithoutOptions_NoActivitySet()
        {
            _fixture.Options = null;
            var activity = new Activity("test");
            activity.Start();
            activity.AddTag("k", "v");

            var scope = new Scope();

            var sut = _fixture.GetSut();
            sut.PopulateScope(_fixture.HttpContext, scope);

            Assert.DoesNotContain(scope.Tags, pair => pair.Key == "k");
        }

        [Fact]
        public void PopulateScope_WithOptionsIncludeActivityFalse_NoActivitySet()
        {
            _fixture.Options.IncludeActivityData = false;
            var activity = new Activity("test");
            activity.Start();
            activity.AddTag("k", "v");

            var scope = new Scope();

            var sut = _fixture.GetSut();
            sut.PopulateScope(_fixture.HttpContext, scope);

            Assert.DoesNotContain(scope.Tags, pair => pair.Key == "k");
        }

        [Fact]
        public void PopulateScope_WithOptionsIncludeActivityTrue_ActivitySet()
        {
            _fixture.Options.IncludeActivityData = true;
            var activity = new Activity("test");
            activity.Start();
            activity.AddTag("k", "v");

            var scope = new Scope();

            var sut = _fixture.GetSut();
            sut.PopulateScope(_fixture.HttpContext, scope);

            Assert.Contains(scope.Tags, pair => pair.Key == "k");
        }

        [Fact]
        public void PopulateScope_WithOptionsIncludeActivityTrueButNull_ActivityNotSet()
        {
            _fixture.Options.IncludeActivityData = true;
            var activity = new Activity("test");
            activity.Start();
            activity.AddTag("k", "v");
            activity.Stop();

            var scope = new Scope();

            var sut = _fixture.GetSut();
            sut.PopulateScope(_fixture.HttpContext, scope);

            Assert.DoesNotContain(scope.Tags, pair => pair.Key == "k");
        }

        [Fact]
        public void Ctor_NullRequestDelegate_ThrowsArgumentNullException()
        {
            _fixture.RequestDelegate = null;
            var ex = Assert.Throws<ArgumentNullException>(() => _fixture.GetSut());
            Assert.Equal("next", ex.ParamName);
        }

        [Fact]
        public void Ctor_NullHub_ThrowsArgumentNullException()
        {
            _fixture.Hub = null;
            var ex = Assert.Throws<ArgumentNullException>(() => _fixture.GetSut());
            Assert.Equal("sentry", ex.ParamName);
        }

        [Fact]
        public async Task InvokeAsync_OptionsReadPayload_CanSeekStream()
        {
            _fixture.Options.IncludeRequestPayload = true;
            var sut = _fixture.GetSut();
            var request = Substitute.For<HttpRequest>();
            var stream = Substitute.For<Stream>();
            request.Body.Returns(stream);
            var response = Substitute.For<HttpResponse>();
            _fixture.HttpContext.Response.Returns(response);
            _fixture.HttpContext.Request.Returns(request);
            request.HttpContext.Returns(_fixture.HttpContext);

            var invoked = false;
            request.When(w => w.Body = Arg.Any<Stream>())
                .Do(d =>
                {
                    Assert.True(d.ArgAt<Stream>(0).CanSeek);
                    invoked = true;
                });

            await sut.InvokeAsync(_fixture.HttpContext);

            Assert.True(invoked);
        }

        [Fact]
        public async Task InvokeAsync_DefaultOptions_CanNotSeekStream()
        {
            var sut = _fixture.GetSut();
            var request = Substitute.For<HttpRequest>();
            var stream = Substitute.For<Stream>();
            request.Body.Returns(stream);
            _fixture.HttpContext.Request.Returns(request);
            request.HttpContext.Returns(_fixture.HttpContext);

            await sut.InvokeAsync(_fixture.HttpContext);

            request.DidNotReceive().Body = Arg.Any<Stream>();
        }

        [Fact]
        public void NameAndVersion_Name_NotNull() => Assert.NotNull(SentryMiddleware.NameAndVersion.Name);

        [Fact]
        public void NameAndVersion_Version_NotNull() => Assert.NotNull(SentryMiddleware.NameAndVersion.Version);

        [Fact]
        public void PopulateScope_Sdk_ContainNameAndVersion()
        {
            var scope = new Scope();

            var sut = _fixture.GetSut();
            sut.PopulateScope(_fixture.HttpContext, scope);

            Assert.Equal(SentryMiddleware.NameAndVersion.Name, scope.Sdk.Name);
            Assert.Equal(SentryMiddleware.NameAndVersion.Version, scope.Sdk.Version);
        }
    }
}
