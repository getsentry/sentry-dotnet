using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;

#if NETCOREAPP3_1_OR_GREATER
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif

namespace Sentry.AspNetCore.Tests;

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
        public SentryAspNetCoreOptions Options { get; set; } = new();
        public IHostingEnvironment HostingEnvironment { get; set; } = Substitute.For<IHostingEnvironment>();
        public ILogger<SentryMiddleware> MiddlewareLogger { get; set; } = Substitute.For<ILogger<SentryMiddleware>>();
        public ILogger SentryLogger { get; set; }
        public HttpContext HttpContext { get; set; } = Substitute.For<HttpContext>();
        public IFeatureCollection FeatureCollection { get; set; } = Substitute.For<IFeatureCollection>();
        public IEnumerable<ISentryEventProcessor> EventProcessors { get; set; } = Substitute.For<IEnumerable<ISentryEventProcessor>>();
        public IEnumerable<ISentryEventExceptionProcessor> EventExceptionProcessors { get; set; } = Substitute.For<IEnumerable<ISentryEventExceptionProcessor>>();
        public IEnumerable<ISentryTransactionProcessor> TransactionProcessors { get; set; } = Substitute.For<IEnumerable<ISentryTransactionProcessor>>();
        private readonly IDisposable _disposable;

        public Fixture()
        {
            HubAccessor = () => Hub;
            var loggingOptions = new SentryLoggingOptions
            {
                InitializeSdk = false,
            };
            loggingOptions.InitializeSdk = false;

            Client.When(client => client.CaptureEvent(Arg.Any<SentryEvent>(), Arg.Any<Scope>(), Arg.Any<Hint>()))
                .Do(callback => callback.Arg<Scope>().Evaluate());

            var hub = new Hub(new SentryOptions { Dsn = ValidDsn });
            hub.BindClient(Client);
            Hub = hub;
            var provider = new SentryLoggerProvider(hub, new MockClock(), loggingOptions);
            _disposable = provider;
            SentryLogger = provider.CreateLogger(nameof(SentryLogger));
            _ = HttpContext.Features.Returns(FeatureCollection);
        }

        public SentryMiddleware GetSut()
            => new(
                HubAccessor,
                Microsoft.Extensions.Options.Options.Create(Options),
                HostingEnvironment,
                MiddlewareLogger,
                EventExceptionProcessors,
                EventProcessors,
                TransactionProcessors);

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

        _ = await Assert.ThrowsAsync<Exception>(async () => await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate));

        _ = _fixture.Client.Received(1).CaptureEvent(
            Arg.Any<SentryEvent>(),
            Arg.Is<Scope>(e => e.Breadcrumbs.Any(b => b.Message == expectedCrumb)), Arg.Any<Hint>());
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

        _ = await Assert.ThrowsAsync<Exception>(async () => await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate));

        _ = _fixture.Client.Received(1).CaptureEvent(
            Arg.Any<SentryEvent>(),
            Arg.Is<Scope>(e => e.Breadcrumbs.Any(b => b.Message == expectedCrumb)), Arg.Any<Hint>());
    }

    [Fact]
    public async Task InvokeAsync_OptionsConfigureScope_AffectsAllRequests()
    {
        const SentryLevel expected = SentryLevel.Debug;
        _fixture.Options.ConfigureScope(s => s.Level = expected);
        _fixture.RequestDelegate = _ => throw new Exception();
        var sut = _fixture.GetSut();

        _ = await Assert.ThrowsAsync<Exception>(async () => await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate));

        _ = _fixture.Client.Received(1).CaptureEvent(
            Arg.Any<SentryEvent>(),
            Arg.Is<Scope>(e => e.Level == expected), Arg.Any<Hint>());
    }

    public void Dispose() => _fixture.Dispose();
}
