using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Sentry.AspNetCore.Extensions;

#if NETCOREAPP3_1_OR_GREATER
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif

namespace Sentry.AspNetCore.Tests;

public class SentryMiddlewareTests
{
    private class Fixture
    {
        public RequestDelegate RequestDelegate { get; set; } = _ => Task.CompletedTask;
        public IHub Hub { get; set; } = Substitute.For<IHub>();
        public Func<IHub> HubAccessor { get; set; }
        public SentryAspNetCoreOptions Options { get; set; } = new();
        public IHostingEnvironment HostingEnvironment { get; set; } = Substitute.For<IHostingEnvironment>();
        public ILogger<SentryMiddleware> Logger { get; set; } = Substitute.For<ILogger<SentryMiddleware>>();
        public HttpContext HttpContext { get; set; } = Substitute.For<HttpContext>();
        public IEnumerable<ISentryEventProcessor> EventProcessors { get; set; } = Substitute.For<IEnumerable<ISentryEventProcessor>>();
        public IEnumerable<ISentryEventExceptionProcessor> EventExceptionProcessors { get; set; } = Substitute.For<IEnumerable<ISentryEventExceptionProcessor>>();
        public IEnumerable<ISentryTransactionProcessor> TransactionProcessors { get; set; } = Substitute.For<IEnumerable<ISentryTransactionProcessor>>();
        public IFeatureCollection FeatureCollection { get; set; } = Substitute.For<IFeatureCollection>();
        public Scope Scope { get; set; }

        public Fixture()
        {
            Scope = new();
            Hub.When(hub => hub.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(callback => callback.Arg<Action<Scope>>().Invoke(Scope));

            Hub.When(hub => hub.CaptureEvent(Arg.Any<SentryEvent>(), scope: Arg.Any<Scope>()))
                .Do(_ => Scope.Evaluate());

            HubAccessor = () => Hub;
            _ = Hub.IsEnabled.Returns(true);
            _ = Hub.StartTransaction("", "").ReturnsForAnyArgs(new TransactionTracer(Hub, "test", "test"));
            _ = HttpContext.Features.Returns(FeatureCollection);
        }

        public SentryMiddleware GetSut()
            => new(
                HubAccessor,
                Microsoft.Extensions.Options.Options.Create(Options),
                HostingEnvironment,
                Logger,
                EventExceptionProcessors,
                EventProcessors,
                TransactionProcessors);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public async Task InvokeAsync_DisabledSdk_InvokesNextHandlers()
    {
        _ = _fixture.Hub.IsEnabled.Returns(false);
        _fixture.RequestDelegate = Substitute.For<RequestDelegate>();

        var sut = _fixture.GetSut();

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);
        await _fixture.RequestDelegate.Received(1).Invoke(_fixture.HttpContext);
    }

    [Fact]
    public async Task InvokeAsync_DisabledSdk_NoScopePushed()
    {
        _ = _fixture.Hub.IsEnabled.Returns(false);

        var sut = _fixture.GetSut();

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);
        _ = _fixture.Hub.DidNotReceive().PushScope();
    }

    [Fact]
    public async Task InvokeAsync_ExceptionThrown_SameRethrown()
    {
        var expected = new Exception("test");
        _fixture.RequestDelegate = _ => throw expected;

        var sut = _fixture.GetSut();

        var actual = await Assert.ThrowsAsync<Exception>(
            async () => await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task InvokeAsync_ExceptionThrown_HandledSetFalse()
    {
        var expected = new Exception("test");
        _fixture.RequestDelegate = _ => throw expected;

        _fixture.Hub.When(h => h.CaptureEvent(Arg.Any<SentryEvent>()))
            .Do(c => Assert.False((bool?)c.Arg<SentryEvent>().Exception?.Data[Mechanism.HandledKey]));

        var sut = _fixture.GetSut();

        var actual = await Assert.ThrowsAsync<Exception>(
            async () => await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task InvokeAsync_FeatureFoundWithNoError_DoesNotCapturesEvent()
    {
        var feature = Substitute.For<IExceptionHandlerFeature>();
        _ = feature.Error.ReturnsNull();
        _ = _fixture.HttpContext.Features.Get<IExceptionHandlerFeature>().Returns(feature);

        var sut = _fixture.GetSut();

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        _ = _fixture.Hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public async Task InvokeAsync_FeatureFoundWithError_CapturesEvent()
    {
        var exception = new Exception();
        var feature = Substitute.For<IExceptionHandlerFeature>();
        _ = feature.Error.Returns(exception);
        _ = _fixture.HttpContext.Features.Get<IExceptionHandlerFeature>().Returns(feature);
        var sut = _fixture.GetSut();

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        _ = _fixture.Hub.Received().CaptureEvent(Arg.Any<SentryEvent>());
        Assert.Equal("IExceptionHandlerFeature", exception.Data[Mechanism.MechanismKey]);
    }

    [Fact]
    public async Task InvokeAsync_ScopePushed_BeforeConfiguringScope()
    {
        var scopePushed = false;
        _fixture.Hub.When(h => h.PushScope()).Do(_ => scopePushed = true);
        _fixture.Hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(_ => Assert.True(scopePushed));

        var sut = _fixture.GetSut();

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

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
                .Then(_ =>
                {
                    Assert.True(scope.Locked);
                    verified = true;
                }));

        var sut = _fixture.GetSut();

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        Assert.True(verified);
    }

    [Fact]
    public async Task InvokeAsync_OnEvaluating_HttpContextDataSet()
    {
        const string expectedTraceIdentifier = "trace id";
        _ = _fixture.HttpContext.TraceIdentifier.Returns(expectedTraceIdentifier);
        var scope = new Scope();
        _fixture.Hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(c => c.Arg<Action<Scope>>()(scope));

        var sut = _fixture.GetSut();

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        scope.Evaluate();

        Assert.Equal(expectedTraceIdentifier, scope.Tags[nameof(HttpContext.TraceIdentifier)]);
    }

    [Fact]
    public async Task InvokeAsync_OnEvaluatingClonedScope_HttpContextDataSet()
    {
        const string expectedTraceIdentifier = "trace id";
        _ = _fixture.HttpContext.TraceIdentifier.Returns(expectedTraceIdentifier);
        var scope = new Scope();
        _fixture.Hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(c => c.Arg<Action<Scope>>()(scope));

        var sut = _fixture.GetSut();

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        var clonedScope = scope.Clone();

        clonedScope.Evaluate();

        Assert.Equal(expectedTraceIdentifier, clonedScope.Tags[nameof(HttpContext.TraceIdentifier)]);
    }

    [Fact]
    public async Task InvokeAsync_ScopePushedAndPopped_OnHappyPath()
    {
        var disposable = Substitute.For<IDisposable>();
        _ = _fixture.Hub.PushScope().Returns(disposable);

        var sut = _fixture.GetSut();

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        _ = _fixture.Hub.Received(1).PushScope();
        disposable.Received(1).Dispose();
    }

    [Fact]
    public async Task InvokeAsync_ScopePushedAndPopped_OnError()
    {
        var expected = new Exception("test");
        _fixture.RequestDelegate = _ => throw expected;
        var disposable = Substitute.For<IDisposable>();
        _ = _fixture.Hub.PushScope().Returns(disposable);

        var sut = _fixture.GetSut();

        _ = await Assert.ThrowsAsync<Exception>(
            async () => await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate));

        _ = _fixture.Hub.Received(1).PushScope();
        disposable.Received(1).Dispose();
    }

    [Fact]
    public void PopulateScope_WithHostingEnvironment_WebRootSet()
    {
        const string expectedWebRoot = "root";
        _ = _fixture.HostingEnvironment.WebRootPath.Returns(expectedWebRoot);
        var scope = new Scope();

        var sut = _fixture.GetSut();
        sut.PopulateScope(_fixture.HttpContext, scope);

        Assert.True(scope.Request.Env.TryGetValue("DOCUMENT_ROOT", out var actualWebRoot));
        Assert.Equal(expectedWebRoot, actualWebRoot);
    }

    [Fact]
    public void PopulateScope_WithOptionsIncludeActivityFalse_NoActivitySet()
    {
        _fixture.Options.IncludeActivityData = false;
        var activity = new Activity("test");
        _ = activity.Start();
        _ = activity.AddTag("k", "v");

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
        _ = activity.Start();
        _ = activity.AddTag("k", "v");

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
        _ = activity.Start();
        _ = activity.AddTag("k", "v");
        activity.Stop();

        var scope = new Scope();

        var sut = _fixture.GetSut();
        sut.PopulateScope(_fixture.HttpContext, scope);

        Assert.DoesNotContain(scope.Tags, pair => pair.Key == "k");
    }

    [Fact]
    public void Ctor_NullHubAccessor_ThrowsArgumentNullException()
    {
        _fixture.HubAccessor = null;
        var ex = Assert.Throws<ArgumentNullException>(() => _fixture.GetSut());
        Assert.Equal("getHub", ex.ParamName);
    }

    [Fact]
    public async Task InvokeAsync_OptionsReadPayload_CanSeekStream()
    {
        _fixture.Options.MaxRequestBodySize = RequestSize.Always;
        var sut = _fixture.GetSut();
        var request = Substitute.For<HttpRequest>();
        var stream = Substitute.For<Stream>();
        _ = request.Body.Returns(stream);
        var response = Substitute.For<HttpResponse>();
        _ = _fixture.HttpContext.Response.Returns(response);
        _ = _fixture.HttpContext.Request.Returns(request);
        _ = request.HttpContext.Returns(_fixture.HttpContext);

        var invoked = false;
        request.When(w => w.Body = Arg.Any<Stream>())
            .Do(d =>
            {
                Assert.True(d.ArgAt<Stream>(0).CanSeek);
                invoked = true;
            });

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        Assert.True(invoked);
    }

    [Fact]
    public async Task InvokeAsync_OptionsMaxRequestSize_Small_CanSeekStream()
    {
        _fixture.Options.MaxRequestBodySize = RequestSize.Small;
        var sut = _fixture.GetSut();
        var request = Substitute.For<HttpRequest>();
        var stream = Substitute.For<Stream>();
        _ = request.Body.Returns(stream);
        var response = Substitute.For<HttpResponse>();
        _ = _fixture.HttpContext.Response.Returns(response);
        _ = _fixture.HttpContext.Request.Returns(request);
        _ = request.HttpContext.Returns(_fixture.HttpContext);

        var invoked = false;
        request.When(w => w.Body = Arg.Any<Stream>())
            .Do(d =>
            {
                Assert.True(d.ArgAt<Stream>(0).CanSeek);
                invoked = true;
            });

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        Assert.True(invoked);
    }

    [Fact]
    public async Task InvokeAsync_OptionsMaxRequestSize_Large_CanSeekStream()
    {
        _fixture.Options.MaxRequestBodySize = RequestSize.Small;
        var sut = _fixture.GetSut();
        var request = Substitute.For<HttpRequest>();
        var stream = Substitute.For<Stream>();
        _ = request.Body.Returns(stream);
        var response = Substitute.For<HttpResponse>();
        _ = _fixture.HttpContext.Response.Returns(response);
        _ = _fixture.HttpContext.Request.Returns(request);
        _ = request.HttpContext.Returns(_fixture.HttpContext);

        var invoked = false;
        request.When(w => w.Body = Arg.Any<Stream>())
            .Do(d =>
            {
                Assert.True(d.ArgAt<Stream>(0).CanSeek);
                invoked = true;
            });

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        Assert.True(invoked);
    }

    [Fact]
    public async Task InvokeAsync_DefaultOptions_CanNotSeekStream()
    {
        var sut = _fixture.GetSut();
        var request = Substitute.For<HttpRequest>();
        var stream = Substitute.For<Stream>();
        _ = request.Body.Returns(stream);
        _ = _fixture.HttpContext.Request.Returns(request);
        _ = request.HttpContext.Returns(_fixture.HttpContext);

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        request.DidNotReceive().Body = Arg.Any<Stream>();
    }

    [Fact]
    public async Task InvokeAsync_OptionsMaxRequestSize_None_CanNotSeekStream()
    {
        _fixture.Options.MaxRequestBodySize = RequestSize.None;
        var sut = _fixture.GetSut();
        var request = Substitute.For<HttpRequest>();
        var stream = Substitute.For<Stream>();
        _ = request.Body.Returns(stream);
        _ = _fixture.HttpContext.Request.Returns(request);
        _ = request.HttpContext.Returns(_fixture.HttpContext);

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

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

        Assert.Equal(Constants.SdkName, scope.Sdk.Name);
        Assert.Equal(SentryMiddleware.NameAndVersion.Version, scope.Sdk.Version);
    }

    [Fact]
    public async Task InvokeAsync_DefaultOptions_DoesNotCallFlushAsync()
    {
        var sut = _fixture.GetSut();
        var response = Substitute.For<HttpResponse>();
        _ = _fixture.HttpContext.Response.Returns(response);
        _ = response.HttpContext.Returns(_fixture.HttpContext);
        response.When(r => r.OnCompleted(Arg.Any<Func<Task>>())).Do(info => info.Arg<Func<Task>>()());

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        await _fixture.Hub.DidNotReceive().FlushAsync(Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task InvokeAsync_FlushOnCompletedRequestWhenFalse_DoesNotCallFlushAsync()
    {
        var sut = _fixture.GetSut();
        _fixture.Options.FlushOnCompletedRequest = false;
        var response = Substitute.For<HttpResponse>();
        _ = _fixture.HttpContext.Response.Returns(response);
        _ = response.HttpContext.Returns(_fixture.HttpContext);
        response.When(r => r.OnCompleted(Arg.Any<Func<Task>>())).Do(info => info.Arg<Func<Task>>()());

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        await _fixture.Hub.DidNotReceive().FlushAsync(Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task InvokeAsync_FlushBeforeRequestCompletedWhenFalse_DoesNotCallFlushAsync()
    {
        var sut = _fixture.GetSut();
        _fixture.Options.FlushBeforeRequestCompleted = false;
        var response = Substitute.For<HttpResponse>();
        _ = _fixture.HttpContext.Response.Returns(response);
        _ = response.HttpContext.Returns(_fixture.HttpContext);
        response.When(r => r.OnCompleted(Arg.Any<Func<Task>>())).Do(info => info.Arg<Func<Task>>()());

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        await _fixture.Hub.DidNotReceive().FlushAsync(Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task InvokeAsync_DisabledHub_DoesNotCallFlushAsync()
    {
        var sut = _fixture.GetSut();
        _fixture.Options.FlushOnCompletedRequest = true;
        _fixture.Options.FlushBeforeRequestCompleted = true;
        _ = _fixture.Hub.IsEnabled.Returns(false);
        var response = Substitute.For<HttpResponse>();
        _ = _fixture.HttpContext.Response.Returns(response);
        _ = response.HttpContext.Returns(_fixture.HttpContext);
        response.When(r => r.OnCompleted(Arg.Any<Func<Task>>())).Do(info => info.Arg<Func<Task>>()());

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        await _fixture.Hub.DidNotReceive().FlushAsync(Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task InvokeAsync_FlushOnCompletedRequestTrue_RespectsTimeout()
    {
        var timeout = TimeSpan.FromSeconds(10);
        var sut = _fixture.GetSut();
        _fixture.Options.FlushOnCompletedRequest = true;
        _fixture.Options.FlushTimeout = timeout;
        var response = Substitute.For<HttpResponse>();
        _ = _fixture.HttpContext.Response.Returns(response);
        _ = response.HttpContext.Returns(_fixture.HttpContext);
        response.When(r => r.OnCompleted(Arg.Any<Func<Task>>())).Do(info => info.Arg<Func<Task>>()());

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        await _fixture.Hub.Received(1).FlushAsync(timeout);
    }

    [Fact]
    public async Task InvokeAsync_FlushBeforeRequestCompletedTrue_RespectsTimeout()
    {
        var timeout = TimeSpan.FromSeconds(10);
        var sut = _fixture.GetSut();
        _fixture.Options.FlushBeforeRequestCompleted = true;
        _fixture.Options.FlushTimeout = timeout;
        var response = Substitute.For<HttpResponse>();
        _ = _fixture.HttpContext.Response.Returns(response);
        _ = response.HttpContext.Returns(_fixture.HttpContext);
        response.When(r => r.OnCompleted(Arg.Any<Func<Task>>())).Do(info => info.Arg<Func<Task>>()());

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        await _fixture.Hub.Received(1).FlushAsync(timeout);
    }

    [Fact]
    public async Task InvokeAsync_ScopeNotPopulated_CopyOptionsToScope()
    {
        // Arrange
        bool configured = false;
        _fixture.Options.ConfigureScope(_ => configured = true);
        var expectedExceptionMessage = "Expected Exception";
        _fixture.RequestDelegate = _ => throw new Exception(expectedExceptionMessage);
        var sut = _fixture.GetSut();

        // Act
        try
        {
            await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);
        }
        catch (Exception ex) when (ex.Message == expectedExceptionMessage)
        { }

        // Assert
        Assert.True(configured);
    }

    [Fact]
    public async Task InvokeAsync_SameMiddleWareWithSameHubs_CopyOptionsOnce()
    {
        // Arrange
        bool configured = false;
        var expectedExceptionMessage = "Expected Exception";
        _fixture.RequestDelegate = _ => throw new Exception(expectedExceptionMessage);
        _fixture.Options.ConfigureScope(_ => configured = true);
        var sut = _fixture.GetSut();

        // Act
        try
        {
            await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);
        }
        catch (Exception ex) when (ex.Message == expectedExceptionMessage)
        { }

        try
        {
            await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);
        }
        catch (Exception ex) when (ex.Message == expectedExceptionMessage)
        { }

        // Assert
        Assert.True(configured);
    }

    [Fact]
    public async Task InvokeAsync_SameMiddleWareWithDifferentHubs_CopyOptionsToAllHubs()
    {
        // Arrange
        int count = 0;
        var firstHub = _fixture.Hub;
        var expectedExceptionMessage = "Expected Exception";
        _fixture.RequestDelegate = _ => throw new Exception(expectedExceptionMessage);
        _fixture.Options.ConfigureScope(_=> count++);
        var sut = _fixture.GetSut();

        // Act
        try
        {
            await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);
        }
        catch (Exception ex) when (ex.Message == expectedExceptionMessage)
        { }

        // Replacing the Hub
        // Arrange
        var secondHub = new Fixture().Hub;
        _fixture.Hub = secondHub;

        // Act
        try
        {
            await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);
        }
        catch
        {
            //
        }

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task InvokeAsync_AlwaysSetsLastEventIdOnScope()
    {
        var sut = _fixture.GetSut();

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        Assert.NotEqual(SentryId.Empty, _fixture.Scope.LastEventId);
    }

    [Fact]
    public async Task InvokeAsync_SetsEventIdOnEvent()
    {
        var expected = new Exception("test");
        _fixture.RequestDelegate = _ => throw expected;

        var sut = _fixture.GetSut();

        try
        {
            await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);
        }
        catch
        {
            // swallow exception for this test
        }

        var eventId = _fixture.Scope.LastEventId;
        _ = _fixture.Hub.Received().CaptureEvent(Arg.Is<SentryEvent>(e => e.EventId.Equals(eventId)));
    }

    [Fact]
    public async Task InvokeAsync_InstrumenterOpenTelemetry_SavesScope()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var scope = new Scope();
        _fixture.Hub.ConfigureScope(Arg.Do<Action<Scope>>(action => action.Invoke(scope)));
        var sut = _fixture.GetSut();
        var activity = new Activity("test").Start();

        try
        {
            // Act
            await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

            // Assert
            activity.GetFused<Scope>().Should().Be(scope);
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public async Task InvokeAsync_RequestContainsSentryHeaders_ContinuesTrace()
    {
        SentryTraceHeader capturedTraceHeader = null;
        BaggageHeader capturedBaggageHeader = null;
        _fixture.Hub.When(hub => hub.ContinueTrace(Arg.Any<SentryTraceHeader>(), Arg.Any<BaggageHeader>()))
            .Do(callInfo =>
            {
                capturedTraceHeader = callInfo.Arg<SentryTraceHeader>();
                capturedBaggageHeader = callInfo.Arg<BaggageHeader>();
            });

        var sut = _fixture.GetSut();
        var request = Substitute.For<HttpRequest>();
        var fakeHeaders = new HeaderDictionary
        {
            { "Sentry-Trace", "4b4d2878507b43d3af7dd8c4ab7a96d9-3cc6fd1337d243de"},
            { "Baggage", "sentry-trace_id=4b4d2878507b43d3af7dd8c4ab7a96d9, sentry-public_key=eb18e953812b41c3aeb042e666fd3b5c"},
        };
        _ = request.Headers.Returns(fakeHeaders);
        _ = _fixture.HttpContext.Request.Returns(request);
        _ = request.HttpContext.Returns(_fixture.HttpContext);

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        _fixture.Hub.Received().ContinueTrace(Arg.Any<SentryTraceHeader>(),Arg.Any<BaggageHeader>());

        Assert.NotNull(capturedTraceHeader);
        Assert.Equal("4b4d2878507b43d3af7dd8c4ab7a96d9", capturedTraceHeader.TraceId.ToString());
        Assert.NotNull(capturedBaggageHeader);
        Assert.Equal("sentry-trace_id=4b4d2878507b43d3af7dd8c4ab7a96d9, sentry-public_key=eb18e953812b41c3aeb042e666fd3b5c", capturedBaggageHeader.ToString());
    }

    [Fact]
    public async Task InvokeAsync_RequestContainsSentryHeaders_AddsHeadersAndTransactionContextToItems()
    {
        var sut = _fixture.GetSut();
        var request = Substitute.For<HttpRequest>();
        var fakeHeaders = new HeaderDictionary
        {
            { "Sentry-Trace", "4b4d2878507b43d3af7dd8c4ab7a96d9-3cc6fd1337d243de"},
            { "Baggage", "sentry-trace_id=4b4d2878507b43d3af7dd8c4ab7a96d9, sentry-public_key=eb18e953812b41c3aeb042e666fd3b5c"},
        };
        var contextItems = new Dictionary<object, object>();
        _fixture.HttpContext.Items.When(items => items.Add(Arg.Any<object>(), Arg.Any<object>()))
            .Do(info =>
            {
                contextItems.Add(info.Args()[0], info.Args()[1]);
            });
        _ = request.Headers.Returns(fakeHeaders);
        _ = _fixture.HttpContext.Request.Returns(request);
        _ = request.HttpContext.Returns(_fixture.HttpContext);

        await sut.InvokeAsync(_fixture.HttpContext, _fixture.RequestDelegate);

        _fixture.HttpContext.Items.Received(3);

        var traceHeader = contextItems[SentryMiddleware.TraceHeaderItemKey] as SentryTraceHeader;
        Assert.NotNull(traceHeader);
        Assert.Equal("4b4d2878507b43d3af7dd8c4ab7a96d9", traceHeader.TraceId.ToString());
        var baggageHeader = contextItems[SentryMiddleware.BaggageHeaderItemKey] as BaggageHeader;
        Assert.NotNull(baggageHeader);
        Assert.Equal("sentry-trace_id=4b4d2878507b43d3af7dd8c4ab7a96d9, sentry-public_key=eb18e953812b41c3aeb042e666fd3b5c", baggageHeader.ToString());
        var transactionContext = contextItems[SentryMiddleware.BaggageHeaderItemKey] as BaggageHeader;
        Assert.NotNull(transactionContext);
    }

    [Fact]
    public void PopulateScope_AddEventProcessors()
    {
        var customProcessor = Substitute.For<ISentryEventProcessor>();
        var eventProcessors = new List<ISentryEventProcessor>() {
            customProcessor
        };
        _fixture.EventProcessors = eventProcessors;

        var sut = _fixture.GetSut();

        var scope = new Scope();
        sut.PopulateScope(_fixture.HttpContext, scope);

        Assert.Contains(customProcessor, scope.GetAllEventProcessors());
    }

    [Fact]
    public void PopulateScope_DoesNotDuplicateEventProcessorsWhenPopulateMultipleTimes()
    {
        var customProcessor = Substitute.For<ISentryEventProcessor>();
        var eventProcessors = new List<ISentryEventProcessor>() {
            customProcessor
        };
        _fixture.EventProcessors = eventProcessors;

        var sut = _fixture.GetSut();

        var scope = new Scope();
        sut.PopulateScope(_fixture.HttpContext, scope);
        sut.PopulateScope(_fixture.HttpContext, scope);

        Assert.Single(scope.GetAllEventProcessors().Where(c => c == customProcessor));
    }

    [Fact]
    public void PopulateScope_AddExceptionEventProcessors()
    {
        var customEventExceptionProcessor = Substitute.For<ISentryEventExceptionProcessor>();
        var eventExceptionProcessors = new List<ISentryEventExceptionProcessor>() {
            customEventExceptionProcessor
        };
        _fixture.EventExceptionProcessors = eventExceptionProcessors;

        var sut = _fixture.GetSut();

        var scope = new Scope();
        sut.PopulateScope(_fixture.HttpContext, scope);

        Assert.Contains(customEventExceptionProcessor, scope.GetAllExceptionProcessors());
    }

    [Fact]
    public void PopulateScope_DoesNotDuplicateExceptionEventProcessorsWhenPopulateMultipleTimes()
    {
        var customEventExceptionProcessor = Substitute.For<ISentryEventExceptionProcessor>();
        var eventExceptionProcessors = new List<ISentryEventExceptionProcessor>() {
            customEventExceptionProcessor
        };
        _fixture.EventExceptionProcessors = eventExceptionProcessors;

        var sut = _fixture.GetSut();

        var scope = new Scope();
        sut.PopulateScope(_fixture.HttpContext, scope);
        sut.PopulateScope(_fixture.HttpContext, scope);

        Assert.Single(scope.GetAllExceptionProcessors().Where(c => c == customEventExceptionProcessor));
    }

    [Fact]
    public void PopulateScope_AddTransactionProcessors()
    {
        var customTransactionProcessor = Substitute.For<ISentryTransactionProcessor>();
        var transactionProcessors = new List<ISentryTransactionProcessor>() {
            customTransactionProcessor
        };
        _fixture.TransactionProcessors = transactionProcessors;

        var sut = _fixture.GetSut();

        var scope = new Scope();
        sut.PopulateScope(_fixture.HttpContext, scope);

        Assert.Contains(customTransactionProcessor, scope.GetAllTransactionProcessors());
    }

    [Fact]
    public void PopulateScope_DoesNotDuplicateTransactionProcessorsWhenPopulateMultipleTimes()
    {
        var customTransactionProcessor = Substitute.For<ISentryTransactionProcessor>();
        var transactionProcessors = new List<ISentryTransactionProcessor>() {
            customTransactionProcessor
        };
        _fixture.TransactionProcessors = transactionProcessors;

        var sut = _fixture.GetSut();

        var scope = new Scope();
        sut.PopulateScope(_fixture.HttpContext, scope);
        sut.PopulateScope(_fixture.HttpContext, scope);

        Assert.Single(scope.GetAllTransactionProcessors().Where(c => c == customTransactionProcessor));
    }
}
