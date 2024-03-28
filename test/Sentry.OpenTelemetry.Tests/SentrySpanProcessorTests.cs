using Sentry.Internal.OpenTelemetry;
using Sentry.Internal.Tracing;

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

        var span = sut.GetMappedSpan(parent.SpanId);

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
}
