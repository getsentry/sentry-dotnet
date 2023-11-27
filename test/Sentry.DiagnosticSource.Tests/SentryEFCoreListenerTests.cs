using Sentry.Internal.DiagnosticSource;
using static Sentry.Internal.DiagnosticSource.SentryEFCoreListener;

namespace Sentry.DiagnosticSource.Tests;

public class SentryEFCoreListenerTests
{
    private static Func<ISpan, bool> GetValidator(string type)
        => type switch
        {
            EFQueryCompiling or EFQueryCompiled =>
                span => span.Operation == "db.query.compile",
            EFConnectionOpening or EFConnectionClosed =>
                span => span.Operation == "db.connection",
            EFCommandExecuting or EFCommandExecuting or EFCommandFailed =>
                span => span.Operation == "db.query",
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

        public IHub Hub { get; }

        public Fixture()
        {
            Hub = Substitute.For<IHub>();
            Tracer = new TransactionTracer(Hub, "foo", "bar")
            {
                IsSampled = true
            };

            var scope = new Scope
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
                .Do(callback => callback.Arg<Action<Scope>>().Invoke(scope));
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
    public void OnNext_KnownKey_GetSpanInvoked(string key, string value)
    {
        // Arrange
        var interceptor = new SentryEFCoreListener(_fixture.Hub, _fixture.Options);

        // Act
        interceptor.OnNext(new(key, value));

        // Assert
        var child = _fixture.Spans.FirstOrDefault(s => GetValidator(key)(s));
        Assert.NotNull(child);
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
        _fixture.Options.DiagnosticLogger.DidNotReceiveWithAnyArgs()?.Log(default, default!);
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

    private class FakeDiagnosticEventData
    {
        public FakeDiagnosticEventData(string value) { _value = value; }
        private readonly string _value;
        public override string ToString()=> _value;

        public class ConnectionInfo
        {
            public string Database { get; } = "rentals";
            public string DataSource { get; } = "127.0.0.1";
        }
        public ConnectionInfo Connection { get; } = new();

        public class ContextInfo
        {
            public class DatabaseInfo
            {
                public string ProviderName { get; } = "Microsoft.EntityFrameworkCore.SqlServer";
            }
            public DatabaseInfo Database { get; } = new();
        }
        public ContextInfo Context { get; } = new();
    }

    private class FakeDiagnosticConnectionEventData : FakeDiagnosticEventData
    {
        public FakeDiagnosticConnectionEventData(string value) : base(value) { }
        public Guid ConnectionId { get; } = Guid.NewGuid();
    }

    private class FakeDiagnosticCommandEventData : FakeDiagnosticEventData
    {
        public FakeDiagnosticCommandEventData(FakeDiagnosticConnectionEventData connection, string value) : base(value)
        {
            ConnectionId = connection.ConnectionId;
        }

        public Guid ConnectionId { get; set; }
        public Guid CommandId { get; set; } = Guid.NewGuid();
    }

    [Fact]
    public void OnNext_HappyPath_IsValid()
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);
        var expectedSql = "SELECT * FROM ...";
        var efSql = "ef Junk\r\nSELECT * FROM ...";
        var efConn = "db username : password";
        var expectedDbName = "rentals";
        var expectedDbSystem = "mssql";
        var expectedDbAddress = "127.0.0.1";

        var queryEventData = new FakeDiagnosticEventData(efSql);
        var connectionEventData = new FakeDiagnosticConnectionEventData(efConn);
        var commandEventData = new FakeDiagnosticCommandEventData(connectionEventData, efSql);

        // Act
        interceptor.OnNext(new(EFQueryCompiling, queryEventData));
        interceptor.OnNext(new(EFQueryCompiled, queryEventData));
        interceptor.OnNext(new(EFConnectionOpening, connectionEventData));
        interceptor.OnNext(new(EFCommandExecuting, commandEventData));
        interceptor.OnNext(new(EFCommandExecuted, commandEventData));
        interceptor.OnNext(new(EFConnectionClosed, connectionEventData));

        // Assert
        var compilerSpan = _fixture.Spans.First(s => GetValidator(EFQueryCompiling)(s));
        var connectionSpan = _fixture.Spans.First(s => GetValidator(EFConnectionOpening)(s));
        var commandSpan = _fixture.Spans.First(s => GetValidator(EFCommandExecuting)(s));

        // Validate if all spans were finished.
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        Assert.All(_fixture.Spans, span =>
        {
            Assert.True(span.IsFinished);
            Assert.Equal(SpanStatus.Ok, span.Status);
        });

        // Assert span descriptions
        Assert.Equal(expectedDbName,connectionSpan.Description);
        Assert.Equal(expectedSql, compilerSpan.Description);
        Assert.Equal(expectedSql, commandSpan.Description);

        // Check DB Name is stored correctly
        Assert.Equal(expectedDbName, compilerSpan.Extra.TryGetValue<string, string>(OTelKeys.DbName));
        Assert.Equal(expectedDbName, connectionSpan.Extra.TryGetValue<string, string>(OTelKeys.DbName));
        Assert.Equal(expectedDbName, commandSpan.Extra.TryGetValue<string, string>(OTelKeys.DbName));

        // Check DB System is stored correctly
        Assert.Equal(expectedDbSystem, compilerSpan.Extra.TryGetValue<string, string>(OTelKeys.DbSystem));
        Assert.Equal(expectedDbSystem, connectionSpan.Extra.TryGetValue<string, string>(OTelKeys.DbSystem));
        Assert.Equal(expectedDbSystem, commandSpan.Extra.TryGetValue<string, string>(OTelKeys.DbSystem));

        // Check DB Server is stored correctly. The compiler span does not have access to the connection so it gets to skip
        Assert.Equal(expectedDbAddress, connectionSpan.Extra.TryGetValue<string, string>(OTelKeys.DbServer));
        Assert.Equal(expectedDbAddress, commandSpan.Extra.TryGetValue<string, string>(OTelKeys.DbServer));

        // Check connections between spans.
        Assert.Equal(_fixture.Tracer.SpanId, compilerSpan.ParentSpanId);
        Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
        Assert.Equal(_fixture.Tracer.SpanId, commandSpan.ParentSpanId);
        _fixture.Options.DiagnosticLogger.DidNotReceive()?
            .Log(Arg.Is(SentryLevel.Warning), Arg.Is("Trying to close a span that was already garbage collected. {0}"),
                null, Arg.Any<object[]>());
    }

    [Fact]
    public void OnNext_HappyPathInsideChildSpan_IsValid()
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);
        var expectedSql = "SELECT * FROM ...";
        var efSql = "ef Junk\r\nSELECT * FROM ...";
        var efConn = "db username : password";
        var expectedDbName = "rentals";
        var expectedDbSystem = "mssql";
        var expectedDbAddress = "127.0.0.1";

        var queryEventData = new FakeDiagnosticEventData(efSql);
        var connectionEventData = new FakeDiagnosticConnectionEventData(efConn);
        var commandEventData = new FakeDiagnosticCommandEventData(connectionEventData, efSql);

        // Act
        var childSpan = _fixture.Tracer.StartChild("Child Span");
        interceptor.OnNext(new(EFQueryCompiling, queryEventData));
        interceptor.OnNext(new(EFQueryCompiled, queryEventData));
        interceptor.OnNext(new(EFConnectionOpening, connectionEventData));
        interceptor.OnNext(new(EFCommandExecuting, commandEventData));
        interceptor.OnNext(new(EFCommandExecuted, commandEventData));
        interceptor.OnNext(new(EFConnectionClosed, connectionEventData));
        childSpan.Finish();

        // Assert
        var compilerSpan = _fixture.Spans.First(s => GetValidator(EFQueryCompiling)(s));
        var connectionSpan = _fixture.Spans.First(s => GetValidator(EFConnectionOpening)(s));
        var commandSpan = _fixture.Spans.First(s => GetValidator(EFCommandExecuting)(s));

        // Validate if all spans were finished.
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        Assert.All(_fixture.Spans, span =>
        {
            Assert.True(span.IsFinished);
            Assert.Equal(SpanStatus.Ok, span.Status);
        });

        // Assert span descriptions
        Assert.Equal(expectedDbName, connectionSpan.Description);
        Assert.Equal(expectedSql, compilerSpan.Description);
        Assert.Equal(expectedSql, commandSpan.Description);

        // Check DB Name is stored correctly
        Assert.Equal(expectedDbName, compilerSpan.Extra.TryGetValue<string, string>(OTelKeys.DbName));
        Assert.Equal(expectedDbName, connectionSpan.Extra.TryGetValue<string, string>(OTelKeys.DbName));
        Assert.Equal(expectedDbName, commandSpan.Extra.TryGetValue<string, string>(OTelKeys.DbName));

        // Check DB System is stored correctly
        Assert.Equal(expectedDbSystem, compilerSpan.Extra.TryGetValue<string, string>(OTelKeys.DbSystem));
        Assert.Equal(expectedDbSystem, connectionSpan.Extra.TryGetValue<string, string>(OTelKeys.DbSystem));
        Assert.Equal(expectedDbSystem, commandSpan.Extra.TryGetValue<string, string>(OTelKeys.DbSystem));

        // Check DB Server is stored correctly. The compiler span does not have access to the connection so it gets to skip
        Assert.Equal(expectedDbAddress, connectionSpan.Extra.TryGetValue<string, string>(OTelKeys.DbServer));
        Assert.Equal(expectedDbAddress, commandSpan.Extra.TryGetValue<string, string>(OTelKeys.DbServer));

        // Check connections between spans.
        Assert.Equal(childSpan.SpanId, compilerSpan.ParentSpanId);
        Assert.Equal(childSpan.SpanId, connectionSpan.ParentSpanId);
        Assert.Equal(childSpan.SpanId, commandSpan.ParentSpanId);
        _fixture.Options.DiagnosticLogger.DidNotReceive()?
            .Log(Arg.Is(SentryLevel.Warning), Arg.Is("Trying to close a span that was already garbage collected. {0}"),
                null, Arg.Any<object[]>());
    }

    [Fact]
    public void OnNext_HappyPathWithError_TransactionWithErroredCommand()
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);
        var expectedSql = "SELECT * FROM ...";
        var efSql = "ef Junk\r\nSELECT * FROM ...";
        var efConn = "db username : password";

        var queryEventData = new FakeDiagnosticEventData(efSql);
        var connectionEventData = new FakeDiagnosticConnectionEventData(efConn);
        var commandEventData = new FakeDiagnosticCommandEventData(connectionEventData, efSql);

        // Act
        interceptor.OnNext(new(EFQueryCompiling, queryEventData));
        interceptor.OnNext(new(EFQueryCompiled, queryEventData));
        interceptor.OnNext(new(EFConnectionOpening, connectionEventData));
        interceptor.OnNext(new(EFCommandExecuting, commandEventData));
        interceptor.OnNext(new(EFCommandFailed, commandEventData));
        interceptor.OnNext(new(EFConnectionClosed, connectionEventData));

        // Assert
        var compilerSpan = _fixture.Spans.First(s => GetValidator(EFQueryCompiling)(s));
        var connectionSpan = _fixture.Spans.First(s => GetValidator(EFConnectionOpening)(s));
        var commandSpan = _fixture.Spans.First(s => GetValidator(EFCommandFailed)(s));

        // Validate if all spans were finished.
        Assert.True(compilerSpan.IsFinished);
        Assert.True(connectionSpan.IsFinished);
        Assert.Equal(SpanStatus.Ok, compilerSpan.Status);
        Assert.Equal(SpanStatus.Ok, connectionSpan.Status);

        // Assert the failed command.
        Assert.True(commandSpan.IsFinished);
        Assert.Equal(SpanStatus.InternalError, commandSpan.Status);

        // Check connections between spans.
        Assert.Equal(_fixture.Tracer.SpanId, compilerSpan.ParentSpanId);
        Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
        Assert.Equal(_fixture.Tracer.SpanId, commandSpan.ParentSpanId);

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
        hub.CaptureEvent(new SentryEvent());

        // Assert
        var compilerSpan = _fixture.Spans.First(s => GetValidator(EFQueryCompiling)(s));

        Assert.True(compilerSpan.IsFinished);
        Assert.Equal(SpanStatus.InternalError, compilerSpan.Status);

        Assert.Equal(_fixture.Tracer.SpanId, compilerSpan.ParentSpanId);

        Assert.Equal(expectedSql, compilerSpan.Description);
    }

    [Fact]
    public void OnNext_Same_Connections_Consolidated()
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentryEFCoreListener(hub, _fixture.Options);
        var efSql = "ef Junk\r\nSELECT * FROM ...";
        var efConn = "db username : password";

        // Fake a connection pool with two connections
        var connectionA = new FakeDiagnosticConnectionEventData(efConn);
        var connectionB = new FakeDiagnosticConnectionEventData(efConn);
        var commandA = new FakeDiagnosticCommandEventData(connectionA, efSql);
        var commandB = new FakeDiagnosticCommandEventData(connectionB, efSql);
        var commandC = new FakeDiagnosticCommandEventData(connectionA, efSql);

        void Pause() => Thread.Sleep(200);

        // Act
        interceptor.OnNext(new(EFConnectionOpening, connectionA));
        Pause();
        interceptor.OnNext(new(EFCommandExecuting, commandA));
        interceptor.OnNext(new(EFCommandExecuted, commandA));
        interceptor.OnNext(new(EFConnectionClosed, connectionA));

        interceptor.OnNext(new(EFConnectionOpening, connectionA));

            // These are for Connection B... interleaved somewhat
            interceptor.OnNext(new(EFConnectionOpening, connectionB));
            Pause();
            interceptor.OnNext(new(EFCommandExecuting, commandB));
            interceptor.OnNext(new(EFCommandExecuted, commandB));

        interceptor.OnNext(new(EFCommandExecuting, commandC));

            // These are for Connection B... interleaved somewhat
            Pause();
            interceptor.OnNext(new(EFConnectionClosed, connectionB));

        interceptor.OnNext(new(EFCommandExecuted, commandC));
        Pause();
        interceptor.OnNext(new(EFConnectionClosed, connectionA));

        // Assert
        bool IsDbSpan(ISpan s) => s.Operation == "db.connection";
        bool IsCommandSpan(ISpan s) => s.Operation == "db.query";
        Func<ISpan, FakeDiagnosticConnectionEventData, bool> forConnection = (s, e) =>
            s.Extra.ContainsKey(EFKeys.DbConnectionId)
            && s.Extra[EFKeys.DbConnectionId] is Guid connectionId
            && connectionId == e.ConnectionId;
        Func<ISpan, FakeDiagnosticCommandEventData, bool> forCommand = (s, e) =>
            s.Extra.ContainsKey(EFKeys.DbCommandId)
            && s.Extra[EFKeys.DbCommandId] is Guid commandId
            && commandId == e.CommandId;

        using (new AssertionScope())
        {
            var dbSpans = _fixture.Spans.Where(IsDbSpan).ToArray();
            dbSpans.Count().Should().Be(2);
            dbSpans.Should().Contain(s => forConnection(s, connectionA));
            dbSpans.Should().Contain(s => forConnection(s, connectionB));

            var commandSpans = _fixture.Spans.Where(IsCommandSpan).ToArray();
            commandSpans.Count().Should().Be(3);
            commandSpans.Should().Contain(s => forCommand(s, commandA));
            commandSpans.Should().Contain(s => forCommand(s, commandB));
            commandSpans.Should().Contain(s => forCommand(s, commandC));

            var connectionASpan = dbSpans.Single(s => forConnection(s, connectionA));
            var connectionBSpan = dbSpans.Single(s => forConnection(s, connectionB));
            var commandASpan = commandSpans.Single(s => forCommand(s, commandA));
            var commandBSpan = commandSpans.Single(s => forCommand(s, commandB));
            var commandCSpan = commandSpans.Single(s => forCommand(s, commandC));

            // Commands for connectionA should take place after it starts and before it finishes
            connectionASpan.StartTimestamp.Should().BeBefore(commandASpan.StartTimestamp);
            connectionASpan.StartTimestamp.Should().BeBefore(commandCSpan.StartTimestamp);
            connectionASpan.EndTimestamp.Should().BeAfter(commandASpan.EndTimestamp ?? DateTimeOffset.MinValue);
            connectionASpan.EndTimestamp.Should().BeAfter(commandCSpan.EndTimestamp ?? DateTimeOffset.MinValue);

            // Commands for connectionB should take place after it starts and before it finishes
            connectionBSpan.StartTimestamp.Should().BeBefore(commandBSpan.StartTimestamp);
            connectionBSpan.EndTimestamp.Should().BeAfter(commandBSpan.EndTimestamp ?? DateTimeOffset.MinValue);

            // Connection B starts after Connection A and finishes before Connection A
            connectionBSpan.StartTimestamp.Should().BeAfter(connectionASpan.StartTimestamp);
            connectionBSpan.EndTimestamp.Should().BeBefore(connectionASpan.EndTimestamp ?? DateTimeOffset.MinValue);
        }
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
        _fixture.Options.DiagnosticLogger.Received(1)?
            .Log(Arg.Is(SentryLevel.Warning), Arg.Is("Tried to close {0} span but no matching span could be found."),
                null, Arg.Any<object[]>());
    }
}
