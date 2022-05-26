using Sentry.Internals.DiagnosticSource;
using static Sentry.Internals.DiagnosticSource.SentryEFCoreListener;

namespace Sentry.DiagnosticSource.Tests;

public class SentryEFCoreListenerTests
{
    private static Func<ISpan, bool> GetValidator(string type)
        => type switch
        {
            EFQueryCompiling or EFQueryCompiled =>
                span => span.Description != null &&
                        span.Operation == "db.query.compile",
            EFConnectionOpening or EFConnectionClosed =>
                span => span.Description == null &&
                        span.Operation == "db.connection",
            EFCommandExecuting or EFCommandExecuting or EFCommandFailed =>
                span => span.Description != null &&
                        span.Operation == "db.query",
            _ => throw new NotSupportedException()
        };

    private class ThrowToStringClass
    {
        public override string ToString() => throw new Exception("ThrowToStringClass");
    }

    private class Fixture
    {
        internal TransactionTracer Tracer { get; }
        public SentryOptions Options { get; }
        public IReadOnlyCollection<ISpan> Spans => Tracer?.Spans;
        public IHub Hub { get; set; }
        private Scope _scope { get; }
        public Fixture()
        {
            Hub = Substitute.For<IHub>();
            Tracer = new TransactionTracer(Hub, "foo", "bar")
            {
                IsSampled = true
            };
            _scope = new Scope
            {
                Transaction = Tracer
            };

            var logger = Substitute.For<IDiagnosticLogger>();
            logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);

            Options = new SentryOptions
            {
                TracesSampleRate = 1.0,
                Debug = true,
                DiagnosticLogger = logger
            };
            Hub.GetSpan().ReturnsForAnyArgs(_ => Spans?.LastOrDefault(s => !s.IsFinished) ?? Tracer);
            Hub.CaptureEvent(Arg.Any<SentryEvent>(), Arg.Any<Scope>()).Returns(_ =>
            {
                Spans.LastOrDefault(s => !s.IsFinished)?.Finish(SpanStatus.InternalError);
                return SentryId.Empty;
            });
            Hub.When(hub => hub.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(callback => callback.Arg<Action<Scope>>().Invoke(_scope));
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void OnNext_UnknownKey_SpanNotInvoked()
    {
        // Assert
        var hub = _fixture.Hub;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);

        // Act
        interceptor.OnNext(new("Unknown", null));

        // Assert
        hub.DidNotReceive().ConfigureScope(Arg.Any<Action<Scope>>());
    }

    [Theory]
    [InlineData(EFQueryCompiling, "data")]
    [InlineData(EFConnectionOpening, null)]
    [InlineData(EFCommandExecuting, "data")]
    public void OnNext_KnownKey_GetSpanInvoked_OnMainTransaction(string key, string value)
    {
        // Arrange
        var interceptor = new SentryEFCoreListener(_fixture.Hub, _fixture.Options);

        // Act
        interceptor.OnNext(new(key, value));

        // Assert
        var child = _fixture.Spans.First(s => GetValidator(key)(s));
        Assert.Equal(_fixture.Tracer.SpanId, child.ParentSpanId);
    }

    [Theory]
    [InlineData(EFQueryCompiling, "data")]
    [InlineData(EFConnectionOpening, null)]
    [InlineData(EFCommandExecuting, "data")]
    public void OnNext_KnownKey_GetSpanInvoked_WithPreviousSpan(string key, string value)
    {
        var mainSpan = _fixture.Tracer.StartChild("hello-world");
        // Arrange
        var interceptor = new SentryEFCoreListener(_fixture.Hub, _fixture.Options);

        // Act
        interceptor.OnNext(new(key, value));

        // Assert
        var child = _fixture.Spans.First(s => GetValidator(key)(s));
        Assert.Equal(mainSpan.SpanId, child.ParentSpanId);
    }

    [Theory]
    [InlineData(EFConnectionOpening, null)]
    [InlineData(EFCommandExecuting, "data")]
    public void OnNext_KnownKeyButDisabled_GetSpanNotInvoked(string key, string value)
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);
        if (key == EFCommandExecuting)
        {
            interceptor.DisableQuerySpan();
        }
        else
        {
            interceptor.DisableConnectionSpan();
        }

        // Act
        interceptor.OnNext(new(key, value));

        // Assert
        hub.DidNotReceive().ConfigureScope(Arg.Any<Action<Scope>>());
    }

    [Theory]
    [InlineData(EFConnectionOpening, null)]
    [InlineData(EFCommandExecuting, "data")]
    public void OnNext_KnownKeyButNotSampled_SpanNotCreated(string key, string value)
    {
        // Arrange
        var hub = _fixture.Hub;
        _fixture.Tracer.IsSampled = false;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);

        // Act
        interceptor.OnNext(new(key, value));

        // Assert
        hub.Received(1).ConfigureScope(Arg.Any<Action<Scope>>());
        Assert.Empty(_fixture.Tracer.Spans);
    }

    [Theory]
    [InlineData(EFConnectionClosed, null)]
    [InlineData(EFQueryCompiled, "data")]
    [InlineData(EFCommandFailed, "data")]
    [InlineData(EFCommandExecuted, "data")]
    public void OnNext_TakeSpanButNotSampled_LogWarningNotInvoked(string key, string value)
    {
        // Arrange
        var hub = _fixture.Hub;
        _fixture.Tracer.IsSampled = false;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);

        // Act
        interceptor.OnNext(new(key, value));

        // Assert
        hub.Received(1).ConfigureScope(Arg.Any<Action<Scope>>());
        _fixture.Options.DiagnosticLogger.DidNotReceive().Log(
            Arg.Any<SentryLevel>(),
            Arg.Any<string>(),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());
    }
    [Theory]
    [InlineData(EFQueryStartCompiling)]
    [InlineData(EFQueryCompiling)]
    [InlineData(EFQueryCompiled)]
    [InlineData(EFConnectionOpening)]
    [InlineData(EFConnectionClosed)]
    [InlineData(EFCommandExecuting)]
    [InlineData(EFCommandFailed)]
    [InlineData(EFCommandExecuted)]
    public void OnNext_ConfigureScopeInvokedOnce(string key)
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);

        // Act
        interceptor.OnNext(new(key, null));

        // Assert
        hub.Received(1).ConfigureScope(Arg.Any<Action<Scope>>());
    }

    [Fact]
    public void OnNext_HappyPath_IsValid()
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);
        var expectedSql = "SELECT * FROM ...";
        var efSql = "ef Junk\r\nSELECT * FROM ...";

        // Act
        interceptor.OnNext(new(EFQueryCompiling, efSql));
        interceptor.OnNext(new(EFQueryCompiled, efSql));
        interceptor.OnNext(new(EFConnectionOpening, null));
        interceptor.OnNext(new(EFCommandExecuting, efSql));
        interceptor.OnNext(new(EFCommandExecuted, efSql));
        interceptor.OnNext(new(EFConnectionClosed, efSql));

        // Assert
        var compilerSpan = _fixture.Spans.First(s => GetValidator(EFQueryCompiling)(s));
        var connectionSpan = _fixture.Spans.First(s => GetValidator(EFConnectionOpening)(s));
        var commandSpan = _fixture.Spans.First(s => GetValidator(EFCommandExecuting)(s));
        // Validate if all spans were finished.
        Assert.All(_fixture.Spans, span =>
        {
            Assert.True(span.IsFinished);
            if (span.Operation == "db.connection")
            {
                Assert.Null(span.Description);
            }
            else
            {
                Assert.Equal(expectedSql, span.Description);
            }
            Assert.Equal(SpanStatus.Ok, span.Status);
        });
        // Check connections between spans.
        Assert.Equal(_fixture.Tracer.SpanId, compilerSpan.ParentSpanId);
        Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
        Assert.Equal(connectionSpan.SpanId, commandSpan.ParentSpanId);
        _fixture.Options.DiagnosticLogger.Received(0).Log(Arg.Is(SentryLevel.Warning), Arg.Is("Trying to close a span that was already garbage collected. {0}"), null, Arg.Any<object[]>());
    }

    [Fact]
    public void OnNext_HappyPathWithError_TransactionWithErroredCommand()
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);
        var expectedSql = "SELECT * FROM ...";
        var efSql = "ef Junk\r\nSELECT * FROM ...";

        // Act
        interceptor.OnNext(new(EFQueryCompiling, efSql));
        interceptor.OnNext(new(EFQueryCompiled, efSql));
        interceptor.OnNext(new(EFConnectionOpening, null));
        interceptor.OnNext(new(EFCommandExecuting, efSql));
        interceptor.OnNext(new(EFCommandFailed, efSql));
        interceptor.OnNext(new(EFConnectionClosed, efSql));

        // Assert
        var compilerSpan = _fixture.Spans.First(s => GetValidator(EFQueryCompiling)(s));
        var connectionSpan = _fixture.Spans.First(s => GetValidator(EFConnectionOpening)(s));
        var commandSpan = _fixture.Spans.First(s => GetValidator(EFCommandFailed)(s));

        // Validate if all spans were finished.
        Assert.All(new[] { compilerSpan, connectionSpan },
            span =>
            {
                Assert.True(span.IsFinished);
                Assert.Equal(SpanStatus.Ok, span.Status);
            });

        // Assert the failed command.
        Assert.True(commandSpan.IsFinished);
        Assert.Equal(SpanStatus.InternalError, commandSpan.Status);

        // Check connections between spans.
        Assert.Equal(_fixture.Tracer.SpanId, compilerSpan.ParentSpanId);
        Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
        Assert.Equal(connectionSpan.SpanId, commandSpan.ParentSpanId);

        Assert.Equal(expectedSql, commandSpan.Description);
    }

    [Fact]
    public void OnNext_HappyPathWithError_TransactionWithErroredCompiler()
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);
        var expectedSql = "SELECT * FROM ...";
        var efSql = "ef Junk\r\nSELECT * FROM ...";

        // Act
        interceptor.OnNext(new(EFQueryCompiling, efSql));
        hub.CaptureEvent(new SentryEvent(), null as Scope);

        // Assert
        var compilerSpan = _fixture.Spans.First(s => GetValidator(EFQueryCompiling)(s));

        Assert.True(compilerSpan.IsFinished);
        Assert.Equal(SpanStatus.InternalError, compilerSpan.Status);

        Assert.Equal(_fixture.Tracer.SpanId, compilerSpan.ParentSpanId);

        Assert.Equal(expectedSql, compilerSpan.Description);
    }

    [Fact]
    public void OnNext_ThrowsException_ExceptionIsolated()
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);
        var exceptionReceived = false;

        // Act
        try
        {
            interceptor.OnNext(new(EFQueryCompiling, new ThrowToStringClass()));
        }
        catch (Exception)
        {
            exceptionReceived = true;
        }

        // Assert
        Assert.False(exceptionReceived);
    }

    [Theory]
    [InlineData(EFCommandExecuted)]
    [InlineData(EFConnectionClosed)]
    [InlineData(EFQueryCompiled)]
    public void OnNext_TakeSpanWithoutSpan_ShowsGarbageCollectorError(string operation)
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);

        // Act
        interceptor.OnNext(new(operation, "ef Junk\r\nSELECT * FROM ..."));

        // Assert
        _fixture.Options.DiagnosticLogger.Received(1).Log(Arg.Is(SentryLevel.Warning), Arg.Is("Trying to close a span that was already garbage collected. {0}"), null, Arg.Any<object[]>());
    }

    [Fact]
    public void FilterNewLineValue_StringWithNewLine_SubStringAfterNewLine()
    {
        // Arrange
        var text = "1234\r\nSELECT *...\n FROM ...";
        var expectedText = "SELECT *...\n FROM ...";

        // Act
        var value = FilterNewLineValue(text);

        // Assert
        Assert.Equal(expectedText, value);
    }

    [Fact]
    public void FilterNewLineValue_NullObject_NullString()
    {
        // Act
        var value = FilterNewLineValue(null);

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void FilterNewLineValue_OneLineString_OneLineString()
    {
        // Arrange
        var text = "1234";
        var expectedText = "1234";

        // Act
        var value = FilterNewLineValue(text);

        // Assert
        Assert.Equal(expectedText, value);
    }

    [Fact]
    public void FilterNewLineValue_EmptyString_EmptyString()
    {
        // Arrange
        var text = "";
        var expectedText = "";

        // Act
        var value = FilterNewLineValue(text);

        // Assert
        Assert.Equal(expectedText, value);
    }
}
