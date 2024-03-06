using Sentry.Internal.OpenTelemetry;

namespace Sentry.OpenTelemetry.Tests;

public class SentrySpanProcessorTests : ActivitySourceTests
{
    private class Fixture
    {
        public SentryOptions Options { get; }

        public ISentryClient Client { get; }

        public ISessionManager SessionManager { get; set; }

        public IInternalScopeManager ScopeManager { get; set; }

        public ISystemClock Clock { get; set; }

        public List<IOpenTelemetryEnricher> Enrichers { get; set; } = new();

        public Fixture()
        {
            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                EnableTracing = true,
                AutoSessionTracking = false
            };

            Client = Substitute.For<ISentryClient>();
        }

        public Hub Hub { get; private set; }

        public Hub GetHub() => Hub ??= new Hub(Options, Client, SessionManager, Clock, ScopeManager);

        public SentrySpanProcessor GetSut()
        {
            return new SentrySpanProcessor(GetHub(), Enrichers);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Ctor_Instrumenter_OpenTelemetry_DoesNotThrowException()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;

        // Act
        var sut = _fixture.GetSut();

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public void Ctor_Instrumenter_Not_OpenTelemetry_Throws()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.Sentry;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _fixture.GetSut());
    }

    [Fact]
    public void GetSpanStatus()
    {
        using (new AssertionScope())
        {
            var noAttributes = new Dictionary<string, object>();

            // Unset and OK -> OK
            SentrySpanProcessor.GetSpanStatus(ActivityStatusCode.Unset, noAttributes).Should().Be(SpanStatus.Ok);
            SentrySpanProcessor.GetSpanStatus(ActivityStatusCode.Ok, noAttributes).Should().Be(SpanStatus.Ok);

            // Error (no attributes) -> UnknownError
            SentrySpanProcessor.GetSpanStatus(ActivityStatusCode.Error, noAttributes)
                .Should().Be(SpanStatus.UnknownError);

            // Unknown status code -> UnknownError
            SentrySpanProcessor.GetSpanStatus((ActivityStatusCode)42, noAttributes)
                .Should().Be(SpanStatus.UnknownError);

            // We only test one http scenario, just to make sure the SpanStatusConverter is called for these headers.
            // Tests for SpanStatusConverter ensure other http status codes would also work though
            var notFoundAttributes = new Dictionary<string, object> { ["http.status_code"] = 404 };
            SentrySpanProcessor.GetSpanStatus(ActivityStatusCode.Error, notFoundAttributes)
                .Should().Be(SpanStatus.NotFound);

            // We only test one grpc scenario, just to make sure the SpanStatusConverter is called for these headers.
            // Tests for SpanStatusConverter ensure other grpc status codes would also work though
            var grpcAttributes = new Dictionary<string, object> { ["rpc.grpc.status_code"] = 7 };
            SentrySpanProcessor.GetSpanStatus(ActivityStatusCode.Error, grpcAttributes)
                .Should().Be(SpanStatus.PermissionDenied);

            var errorAttributes = new Dictionary<string, object> { [OtelSpanAttributeConstants.StatusCodeKey] = OtelStatusTags.ErrorStatusCodeTagValue };
            SentrySpanProcessor.GetSpanStatus(ActivityStatusCode.Ok, errorAttributes).Should().Be(SpanStatus.UnknownError);
            SentrySpanProcessor.GetSpanStatus(ActivityStatusCode.Unset, errorAttributes).Should().Be(SpanStatus.UnknownError);
        }
    }

    [Fact]
    public void OnStart_Transaction_With_DynamicSamplingContext()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var sut = _fixture.GetSut();

        var expected = new Dictionary<string, string>()
        {
            { "trace_id", SentryId.Create().ToString() },
            { "public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff" },
            { "sample_rate", "0.5" },
        };
        var data = Tracer.StartActivity("test op")!;
        data.AddBaggage($"{BaggageHeader.SentryKeyPrefix}trace_id", expected["trace_id"]);
        data.AddBaggage($"{BaggageHeader.SentryKeyPrefix}public_key", expected["public_key"]);
        data.AddBaggage($"{BaggageHeader.SentryKeyPrefix}sample_rate", expected["sample_rate"]);

        // Act
        sut.OnStart(data!);

        // Assert
        Assert.True(sut._map.TryGetValue(data.SpanId, out var span));
        if (span is not TransactionTracer transaction)
        {
            Assert.Fail("Span is not a transaction tracer");
            return;
        }
        if (transaction.DynamicSamplingContext is not { } actual)
        {
            Assert.Fail("Transaction does not have a dynamic sampling context");
            return;
        }
        using (new AssertionScope())
        {
            actual.Items["trace_id"].Should().Be(expected["trace_id"]);
            actual.Items["public_key"].Should().Be(expected["public_key"]);
            actual.Items["sample_rate"].Should().Be(expected["sample_rate"]);
        }
    }

    [Fact]
    public void OnStart_WithParentSpanId_StartsChildSpan()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var sut = _fixture.GetSut();

        using var parent = Tracer.StartActivity("Parent");
        sut.OnStart(parent);

        using var data = Tracer.StartActivity("TestActivity");

        // Act
        sut.OnStart(data!);

        // Assert
        Assert.True(sut._map.TryGetValue(data.SpanId, out var span));
        using (new AssertionScope())
        {
            span.Should().BeOfType<SpanTracer>();
            span.SpanId.Should().Be(data.SpanId.AsSentrySpanId());
            span.ParentSpanId.Should().Be(data.ParentSpanId.AsSentrySpanId());
            if (span is not SpanTracer spanTracer)
            {
                Assert.Fail("Span is not a span tracer");
                return;
            }
            using (new AssertionScope())
            {
                spanTracer.SpanId.Should().Be(data.SpanId.AsSentrySpanId());
                spanTracer.ParentSpanId.Should().Be(data.ParentSpanId.AsSentrySpanId());
                spanTracer.TraceId.Should().Be(data.TraceId.AsSentryId());
                spanTracer.Operation.Should().Be(data.OperationName);
                spanTracer.Description.Should().Be(data.DisplayName);
                spanTracer.Status.Should().BeNull();
                spanTracer.StartTimestamp.Should().Be(data.StartTimeUtc);
            }
        }
    }

    [Fact]
    public void OnStart_WithoutParentSpanId_StartsNewTransaction()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        _fixture.ScopeManager = Substitute.For<IInternalScopeManager>();
        var sut = _fixture.GetSut();

        var data = Tracer.StartActivity("test op");

        // Act
        sut.OnStart(data!);

        // Assert
        Assert.True(sut._map.TryGetValue(data.SpanId, out var span));
        if (span is not TransactionTracer transaction)
        {
            Assert.Fail("Span is not a transaction tracer");
            return;
        }
        using (new AssertionScope())
        {
            transaction.SpanId.Should().Be(data.SpanId.AsSentrySpanId());
            transaction.ParentSpanId.Should().Be(new ActivitySpanId().AsSentrySpanId());
            transaction.TraceId.Should().Be(data.TraceId.AsSentryId());
            transaction.Name.Should().Be(data.DisplayName);
            transaction.Operation.Should().Be(data.OperationName);
            transaction.Description.Should().Be(data.DisplayName);
            transaction.Status.Should().BeNull();
            transaction.StartTimestamp.Should().Be(data.StartTimeUtc);
            _fixture.ScopeManager.Received(1).ConfigureScope(Arg.Any<Action<Scope>>());
        }
    }

    [Fact]
    public void OnEnd_FinishesSpan()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var sut = _fixture.GetSut();

        var parent = Tracer.StartActivity(name: "transaction")!;
        sut.OnStart(parent);

        var tags = new Dictionary<string, object> {
            { "foo", "bar" }
        };
        var data = Tracer.StartActivity(name: "test operation", kind: ActivityKind.Internal, parentContext: default, tags)!;
        data.DisplayName = "test display name";
        sut.OnStart(data);

        sut._map.TryGetValue(data.SpanId, out var span);

        // Act
        sut.OnEnd(data);

        // Assert
        if (span is not SpanTracer spanTracer)
        {
            Assert.Fail("Span is not a span tracer");
            return;
        }

        using (new AssertionScope())
        {
            spanTracer.ParentSpanId.Should().Be(parent.SpanId.AsSentrySpanId());
            spanTracer.Operation.Should().Be(data.OperationName);
            spanTracer.Description.Should().Be(data.DisplayName);
            spanTracer.EndTimestamp.Should().NotBeNull();
            spanTracer.Extra["otel.kind"].Should().Be(data.Kind);
            foreach (var keyValuePair in tags)
            {
                span.Extra[keyValuePair.Key].Should().Be(keyValuePair.Value);
            }

            spanTracer.Status.Should().Be(SpanStatus.Ok);
        }
    }

    [Fact]
    public void OnEnd_Transaction_RestoresSavedScope()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        _fixture.ScopeManager = Substitute.For<IInternalScopeManager>();
        var sut = _fixture.GetSut();

        var scope = new Scope();
        var data = Tracer.StartActivity("transaction")!;
        data.SetFused(scope);
        sut.OnStart(data);

        // Act
        sut.OnEnd(data);

        // Assert
        _fixture.ScopeManager.Received(1).RestoreScope(scope);
    }

    [Fact]
    public void OnEnd_Span_RestoresSavedScope()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        _fixture.ScopeManager = Substitute.For<IInternalScopeManager>();
        var sut = _fixture.GetSut();

        var scope = new Scope();
        var parent = Tracer.StartActivity("transaction")!;
        parent.SetFused(scope);
        sut.OnStart(parent);

        var data = Tracer.StartActivity("test operation")!;
        data.DisplayName = "test display name";
        sut.OnStart(data);

        // Act
        sut.OnEnd(data);

        // Assert
        _fixture.ScopeManager.Received(1).RestoreScope(scope);
    }

    [Fact]
    public void OnEnd_SpansEnriched()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var mockEnricher = Substitute.For<IOpenTelemetryEnricher>();
        mockEnricher.Enrich(Arg.Do<ISpan>(s => s.SetTag("foo", "bar")), Arg.Any<Activity>(), Arg.Any<IHub>(), Arg.Any<SentryOptions>());
        _fixture.Enrichers.Add(mockEnricher);
        var sut = _fixture.GetSut();

        var parent = Tracer.StartActivity(name: "transaction")!;
        sut.OnStart(parent);

        sut._map.TryGetValue(parent.SpanId, out var span);

        // Act
        sut.OnEnd(parent);

        // Assert
        if (span is not TransactionTracer transactionTracer)
        {
            Assert.Fail("Span is not a transaction tracer");
            return;
        }

        transactionTracer.Tags.TryGetValue("foo", out var foo).Should().BeTrue();
        foo.Should().Be("bar");
    }

    [Fact]
    public void OnEnd_FinishesTransaction()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var sut = _fixture.GetSut();

        var tags = new Dictionary<string, object> {
            { "foo", "bar" }
        };
        var data = Tracer.StartActivity(name: "test operation", kind: ActivityKind.Internal, parentContext: default, tags)!;
        data.DisplayName = "test display name";
        sut.OnStart(data);

        sut._map.TryGetValue(data.SpanId, out var span);

        // Act
        sut.OnEnd(data);

        // Assert
        if (span is not TransactionTracer transaction)
        {
            Assert.Fail("Span is not a transaction tracer");
            return;
        }

        using (new AssertionScope())
        {
            transaction.ParentSpanId.Should().Be(new ActivitySpanId().AsSentrySpanId());
            transaction.Operation.Should().Be(data.OperationName);
            transaction.Description.Should().Be(data.DisplayName);
            transaction.Name.Should().Be(data.DisplayName);
            transaction.NameSource.Should().Be(TransactionNameSource.Custom);
            transaction.EndTimestamp.Should().NotBeNull();
            transaction.Contexts["otel"].Should().BeEquivalentTo(new Dictionary<string, object>
            {
                { "attributes", tags }
            });
            transaction.Status.Should().Be(SpanStatus.Ok);
        }
    }

    [Theory]
    [InlineData(OtelSemanticConventions.AttributeUrlFull)]
    [InlineData(OtelSemanticConventions.AttributeHttpUrl)]
    public void OnEnd_IsSentryRequest_DoesNotFinishTransaction(string urlKey)
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var sut = _fixture.GetSut();

        var tags = new Dictionary<string, object> { { "foo", "bar" }, { urlKey, _fixture.Options.Dsn } };
        var data = Tracer.StartActivity(name: "test operation", kind: ActivityKind.Internal, parentContext: default, tags)!;
        data.DisplayName = "test display name";
        sut.OnStart(data);

        sut._map.TryGetValue(data.SpanId, out var span);

        // Act
        sut.OnEnd(data);

        // Assert
        if (span is not TransactionTracer transaction)
        {
            Assert.Fail("Span is not a transaction tracer");
            return;
        }

        transaction.IsSentryRequest.Should().BeTrue();
    }

    private static void FilterActivity(Activity activity)
    {
        // Simulates filtering an activity - see https://github.com/getsentry/sentry-dotnet/pull/3198
        activity.IsAllDataRequested = false;
        activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
    }

    [Fact]
    public void PruneFilteredSpans_FilteredTransactions_Pruned()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var sut = _fixture.GetSut();

        using var parent = Tracer.StartActivity();
        sut.OnStart(parent!);

        FilterActivity(parent);

        // Act
        sut.PruneFilteredSpans(true);

        // Assert
        Assert.False(sut._map.TryGetValue(parent.SpanId, out var _));
    }

    [Fact]
    public void PruneFilteredSpans_UnFilteredTransactions_NotPruned()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var sut = _fixture.GetSut();

        using var parent = Tracer.StartActivity();
        sut.OnStart(parent!);

        // Act
        sut.PruneFilteredSpans(true);

        // Assert
        Assert.True(sut._map.TryGetValue(parent.SpanId, out var _));
    }

    [Fact]
    public void PruneFilteredSpans_FilteredSpans_Pruned()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var sut = _fixture.GetSut();

        using var parent = Tracer.StartActivity();
        sut.OnStart(parent!);

        using var activity1 = Tracer.StartActivity();
        sut.OnStart(activity1!);

        using var activity2 = Tracer.StartActivity();
        sut.OnStart(activity2!);

        FilterActivity(activity2);

        // Act
        sut.PruneFilteredSpans(true);

        // Assert
        Assert.True(sut._map.TryGetValue(activity1.SpanId, out var _));
        Assert.False(sut._map.TryGetValue(activity2.SpanId, out var _));
    }

    [Fact]
    public void PruneFilteredSpans_RecentlyPruned_DoesNothing()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var sut = _fixture.GetSut();

        sut._lastPruned = DateTimeOffset.MaxValue; // fake a recent prune

        using var parent = Tracer.StartActivity();
        sut.OnStart(parent!);

        using var activity1 = Tracer.StartActivity();
        sut.OnStart(activity1!);

        using var activity2 = Tracer.StartActivity();
        sut.OnStart(activity2!);

        FilterActivity(activity2);

        // Act
        sut.PruneFilteredSpans();

        // Assert
        Assert.True(sut._map.TryGetValue(activity1.SpanId, out var _));
        Assert.True(sut._map.TryGetValue(activity2.SpanId, out var _));
    }
}
