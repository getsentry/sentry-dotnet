using System;
using System.Linq;
using System.Threading.Tasks;
#if NETCOREAPP2_1 || NET461
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sentry.Extensions.Logging;
using Sentry.Infrastructure;
using Sentry.Internal;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    // Uses a shared Hub between the Middleware and the LoggerProvider
    // Tests the integration of these 3 objects
    public class MiddlewareLoggerIntegration : IDisposable
    {
        private class Fixture : IDisposable
        {
            public RequestDelegate RequestDelegate { get; set; } = _ => Task.CompletedTask;
            public IHub Hub { get; set; }
            public Func<IHub> HubAccessor { get; set; }
            public ISentryClient Client { get; set; } = Substitute.For<ISentryClient>();
            public ISystemClock Clock { get; set; } = Substitute.For<ISystemClock>();
            public SentryAspNetCoreOptions Options { get; set; } = new();
            public IHostingEnvironment HostingEnvironment { get; set; } = Substitute.For<IHostingEnvironment>();
            public ILogger<SentryMiddleware> MiddlewareLogger { get; set; } = Substitute.For<ILogger<SentryMiddleware>>();
            public ILogger SentryLogger { get; set; }
            public HttpContext HttpContext { get; set; } = Substitute.For<HttpContext>();
            public IFeatureCollection FeatureCollection { get; set; } = Substitute.For<IFeatureCollection>();
            private readonly IDisposable _disposable;

            public Fixture()
            {
                HubAccessor = () => Hub;
                var loggingOptions = new SentryLoggingOptions
                {
                    InitializeSdk = false,
                };
                loggingOptions.InitializeSdk = false;

                var hub = new Hub(new SentryOptions { Dsn = DsnSamples.ValidDsnWithSecret });
                hub.BindClient(Client);
                Hub = hub;
                var provider = new SentryLoggerProvider(hub, Clock, loggingOptions);
                _disposable = provider;
                SentryLogger = provider.CreateLogger(nameof(SentryLogger));
                _ = HttpContext.Features.Returns(FeatureCollection);
            }

            public SentryMiddleware GetSut()
                => new(
                    RequestDelegate,
                    HubAccessor,
                    Microsoft.Extensions.Options.Options.Create(Options),
                    HostingEnvironment,
                    MiddlewareLogger);

            public void Dispose() => _disposable.Dispose();
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public async Task InvokeAsync_LoggerMessage_AsBreadcrumb()
        {
            const string expectedCrumb = "expect this";
            _fixture.RequestDelegate = _ =>
            {
                _fixture.SentryLogger.LogInformation(expectedCrumb);
                throw new Exception();
            };
            var sut = _fixture.GetSut();

            _ = await Assert.ThrowsAsync<Exception>(async () => await sut.InvokeAsync(_fixture.HttpContext));

            _ = _fixture.Client.Received(1).CaptureEvent(
                    Arg.Any<SentryEvent>(),
                    Arg.Is<Scope>(e => e.Breadcrumbs.Any(b => b.Message == expectedCrumb)));
        }

        [Fact]
        public async Task InvokeAsync_LoggerPushesScope_LoggerMessage_AsBreadcrumb()
        {
            const string expectedCrumb = "expect this";
            _fixture.RequestDelegate = _ =>
            {
                using (_fixture.SentryLogger.BeginScope("scope"))
                {
                    _fixture.SentryLogger.LogInformation(expectedCrumb);
                }
                throw new Exception();
            };
            var sut = _fixture.GetSut();

            _ = await Assert.ThrowsAsync<Exception>(async () => await sut.InvokeAsync(_fixture.HttpContext));

            _ = _fixture.Client.Received(1).CaptureEvent(
                    Arg.Any<SentryEvent>(),
                    Arg.Is<Scope>(e => e.Breadcrumbs.Any(b => b.Message == expectedCrumb)));
        }

        [Fact]
        public async Task InvokeAsync_OptionsConfigureScope_AffectsAllRequests()
        {
            const SentryLevel expected = SentryLevel.Debug;
            _fixture.Options.ConfigureScope(s => s.Level = expected);
            _fixture.RequestDelegate = _ => throw new Exception();
            var sut = _fixture.GetSut();

            _ = await Assert.ThrowsAsync<Exception>(async () => await sut.InvokeAsync(_fixture.HttpContext));

            _ = _fixture.Client.Received(1).CaptureEvent(
                    Arg.Any<SentryEvent>(),
                    Arg.Is<Scope>(e => e.Level == expected));
        }

        public void Dispose() => _fixture.Dispose();
    }
}
