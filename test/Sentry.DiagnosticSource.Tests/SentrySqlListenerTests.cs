using Sentry.Internals.DiagnosticSource;
using Sentry.Testing;

namespace Sentry.DiagnosticSource.Tests;

internal static class SentrySqlListenerExtensions
{
    public static void OpenConnectionStart(this SentrySqlListener listener, Guid operationId)
        => listener.OnNext(new KeyValuePair<string, object>(
            SentrySqlListener.SqlMicrosoftWriteConnectionOpenBeforeCommand,
            new { OperationId = operationId }));

    public static void OpenConnectionStarted(this SentrySqlListener listener, Guid operationId, Guid connectionId)
        => listener.OnNext(new KeyValuePair<string, object>(
            SentrySqlListener.SqlMicrosoftWriteConnectionOpenAfterCommand,
            new { OperationId = operationId, ConnectionId = connectionId }));

    public static void OpenConnectionClose(this SentrySqlListener listener, Guid operationId, Guid connectionId)
        => listener.OnNext(new KeyValuePair<string, object>(
            SentrySqlListener.SqlMicrosoftWriteConnectionCloseAfterCommand,
            new { OperationId = operationId, ConnectionId = connectionId }));

    public static void ExecuteQueryStart(this SentrySqlListener listener, Guid operationId, Guid connectionId)
        => listener.OnNext(new KeyValuePair<string, object>(
            SentrySqlListener.SqlDataBeforeExecuteCommand,
            new { OperationId = operationId, ConnectionId = connectionId }));

    public static void ExecuteQueryFinish(this SentrySqlListener listener, Guid operationId, Guid connectionId, string query)
        => listener.OnNext(new KeyValuePair<string, object>(
            SentrySqlListener.SqlDataAfterExecuteCommand,
            new { OperationId = operationId, ConnectionId = connectionId, Command = new { CommandText = query } }));

    public static void ExecuteQueryFinishWithError(this SentrySqlListener listener, Guid operationId, Guid connectionId, string query)
        => listener.OnNext(new KeyValuePair<string, object>(
            SentrySqlListener.SqlDataWriteCommandError,
            new { OperationId = operationId, ConnectionId = connectionId, Command = new { CommandText = query } }));
}

public class SentrySqlListenerTests
{
    internal const string SqlDataWriteConnectionOpenBeforeCommand = SentrySqlListener.SqlDataWriteConnectionOpenBeforeCommand;
    internal const string SqlMicrosoftWriteConnectionOpenBeforeCommand = SentrySqlListener.SqlMicrosoftWriteConnectionOpenBeforeCommand;

    internal const string SqlMicrosoftWriteConnectionOpenAfterCommand = SentrySqlListener.SqlMicrosoftWriteConnectionOpenAfterCommand;
    internal const string SqlDataWriteConnectionOpenAfterCommand = SentrySqlListener.SqlDataWriteConnectionOpenAfterCommand;

    internal const string SqlMicrosoftWriteConnectionCloseAfterCommand = SentrySqlListener.SqlMicrosoftWriteConnectionCloseAfterCommand;
    internal const string SqlDataWriteConnectionCloseAfterCommand = SentrySqlListener.SqlDataWriteConnectionCloseAfterCommand;

    internal const string SqlDataBeforeExecuteCommand = SentrySqlListener.SqlDataBeforeExecuteCommand;
    internal const string SqlMicrosoftBeforeExecuteCommand = SentrySqlListener.SqlMicrosoftBeforeExecuteCommand;

    internal const string SqlDataAfterExecuteCommand = SentrySqlListener.SqlDataAfterExecuteCommand;
    internal const string SqlMicrosoftAfterExecuteCommand = SentrySqlListener.SqlMicrosoftAfterExecuteCommand;

    internal const string SqlDataWriteCommandError = SentrySqlListener.SqlDataWriteCommandError;
    internal const string SqlMicrosoftWriteCommandError = SentrySqlListener.SqlMicrosoftWriteCommandError;

    internal const string SqlDataWriteTransactionCommitAfter = SentrySqlListener.SqlDataWriteTransactionCommitAfter;
    internal const string SqlMicrosoftWriteTransactionCommitAfter = SentrySqlListener.SqlMicrosoftWriteTransactionCommitAfter;

    private Func<ISpan, bool> GetValidator(string type)
        => type switch
        {
            _ when
                type == SqlDataWriteConnectionOpenBeforeCommand ||
                type == SqlMicrosoftWriteConnectionOpenBeforeCommand ||
                type == SqlMicrosoftWriteConnectionOpenAfterCommand ||
                type == SqlDataWriteConnectionOpenAfterCommand ||
                type == SqlMicrosoftWriteConnectionCloseAfterCommand ||
                type == SqlDataWriteConnectionCloseAfterCommand ||
                type == SqlDataWriteTransactionCommitAfter ||
                type == SqlMicrosoftWriteTransactionCommitAfter
                => span => span.Description is null && span.Operation == "db.connection",
            _ when
                type == SqlDataBeforeExecuteCommand ||
                type == SqlMicrosoftBeforeExecuteCommand ||
                type == SqlDataAfterExecuteCommand ||
                type == SqlMicrosoftAfterExecuteCommand ||
                type == SqlDataWriteCommandError ||
                type == SqlMicrosoftWriteCommandError
                => span => span.Operation == "db.query",
            _ => throw new NotSupportedException()
        };

    private class ThrowToOperationClass
    {
        private string _operationId;
        public string OperationId
        {
            get => throw new Exception();
            set => _operationId = value;
        }
        public string ConnectionId { get; set; }
    }

    private class Fixture
    {
        private Scope _scope { get; }
        internal TransactionTracer Tracer { get; }

        public InMemoryDiagnosticLogger Logger { get; }

        public SentryOptions Options { get; }

        public IReadOnlyCollection<ISpan> Spans => Tracer?.Spans;
        public IHub Hub { get; set; }
        public Fixture()
        {
            Logger = new InMemoryDiagnosticLogger();

            Options = new SentryOptions
            {
                Debug = true,
                DiagnosticLogger = Logger,
                DiagnosticLevel = SentryLevel.Debug,
                TracesSampleRate = 1,
            };
            Tracer = new TransactionTracer(Hub, "foo", "bar")
            {
                IsSampled = true
            };
            _scope = new Scope
            {
                Transaction = Tracer
            };
            Hub = Substitute.For<IHub>();
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
        var interceptor = new SentrySqlListener(hub, new SentryOptions());

        // Act
        interceptor.OnNext(new("Unknown", null));

        // Assert
        hub.DidNotReceive().GetSpan();
    }

    [Theory]
    [InlineData(SqlMicrosoftBeforeExecuteCommand, true)]
    [InlineData(SqlDataBeforeExecuteCommand, true)]
    [InlineData(SqlMicrosoftWriteConnectionOpenBeforeCommand, false)]
    [InlineData(SqlDataWriteConnectionOpenBeforeCommand, false)]
    public void OnNext_KnownKey_GetSpanInvoked(string key, bool addConnectionSpan)
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentrySqlListener(hub, new SentryOptions());
        if (addConnectionSpan)
        {
            _fixture.Tracer.StartChild("abc").SetExtra(SentrySqlListener.ConnectionExtraKey, Guid.Empty);
        }

        // Act
        interceptor.OnNext(
            new(key,
                new { OperationId = Guid.Empty, ConnectionId = Guid.Empty, Command = new { CommandText = "" } }));

        // Assert
        var spans = _fixture.Spans.Where(s => s.Operation != "abc");
        Assert.NotEmpty(spans);
        Assert.True(GetValidator(key)(_fixture.Spans.First()));
    }

    [Theory]
    [InlineData(SqlMicrosoftWriteConnectionOpenBeforeCommand, SqlMicrosoftWriteConnectionOpenAfterCommand, SqlMicrosoftWriteConnectionCloseAfterCommand, SqlMicrosoftBeforeExecuteCommand, SqlMicrosoftAfterExecuteCommand)]
    [InlineData(SqlDataWriteConnectionOpenBeforeCommand, SqlDataWriteConnectionOpenAfterCommand, SqlDataWriteConnectionCloseAfterCommand, SqlDataBeforeExecuteCommand, SqlDataAfterExecuteCommand)]
    public void OnNext_HappyPathsWithoutTransaction_IsValid(string connectionOpenKey, string connectionUpdateKey, string connectionCloseKey, string queryStartKey, string queryEndKey)
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentrySqlListener(hub, new SentryOptions());
        var query = "SELECT * FROM ...";
        var connectionId = Guid.NewGuid();
        var connectionOperationId = Guid.NewGuid();
        var connectionOperationIdClosed = Guid.NewGuid();
        var queryOperationId = Guid.NewGuid();

        // Act
        interceptor.OnNext(
            new(connectionOpenKey,
                new { OperationId = connectionOperationId }));
        interceptor.OnNext(
            new(connectionUpdateKey,
                new { OperationId = connectionOperationId, ConnectionId = connectionId }));
        interceptor.OnNext(
            new(queryStartKey,
                new { OperationId = queryOperationId, ConnectionId = connectionId }));
        interceptor.OnNext(
            new(queryEndKey,
                new { OperationId = queryOperationId, ConnectionId = connectionId, Command = new { CommandText = query } }));
        interceptor.OnNext(
            new(connectionCloseKey,
                new { OperationId = connectionOperationIdClosed, ConnectionId = connectionId }));
        //Connection", "ClientConnectionId
        // Assert
        _fixture.Spans.Should().HaveCount(2);
        var connectionSpan = _fixture.Spans.First(s => GetValidator(connectionOpenKey)(s));
        var commandSpan = _fixture.Spans.First(s => GetValidator(queryStartKey)(s));

        // Validate if all spans were finished.
        Assert.All(_fixture.Spans, span =>
        {
            Assert.True(span.IsFinished);
            Assert.Equal(SpanStatus.Ok, span.Status);
        });
        // Check connections between spans.
        Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
        Assert.Equal(connectionSpan.SpanId, commandSpan.ParentSpanId);

        Assert.Equal(query, commandSpan.Description);
    }

    [Theory]
    [InlineData(SqlMicrosoftWriteConnectionOpenBeforeCommand, SqlMicrosoftWriteConnectionOpenAfterCommand, SqlMicrosoftWriteTransactionCommitAfter, SqlMicrosoftBeforeExecuteCommand, SqlMicrosoftAfterExecuteCommand)]
    [InlineData(SqlDataWriteConnectionOpenBeforeCommand, SqlDataWriteConnectionOpenAfterCommand, SqlDataWriteTransactionCommitAfter, SqlDataBeforeExecuteCommand, SqlDataAfterExecuteCommand)]
    public void OnNext_HappyPathsWithTransaction_IsValid(string connectionOpenKey, string connectionUpdateKey, string connectionCloseKey, string queryStartKey, string queryEndKey)
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentrySqlListener(hub, new SentryOptions());
        var query = "SELECT * FROM ...";
        var connectionId = Guid.NewGuid();
        var connectionOperationId = Guid.NewGuid();
        var connectionOperationIdClosed = Guid.NewGuid();
        var queryOperationId = Guid.NewGuid();

        // Act
        interceptor.OnNext(
            new(connectionOpenKey,
                new { OperationId = connectionOperationId }));
        interceptor.OnNext(
            new(connectionUpdateKey,
                new { OperationId = connectionOperationId, ConnectionId = connectionId }));
        interceptor.OnNext(
            new(queryStartKey,
                new { OperationId = queryOperationId, ConnectionId = connectionId }));
        interceptor.OnNext(
            new(queryEndKey,
                new { OperationId = queryOperationId, ConnectionId = connectionId, Command = new { CommandText = query } }));
        interceptor.OnNext(
            new(connectionCloseKey,
                new { OperationId = connectionOperationIdClosed, Connection = new { ClientConnectionId = connectionId } }));

        // Assert
        _fixture.Spans.Should().HaveCount(2);
        var connectionSpan = _fixture.Spans.First(s => GetValidator(connectionOpenKey)(s));
        var commandSpan = _fixture.Spans.First(s => GetValidator(queryStartKey)(s));

        // Validate if all spans were finished.
        Assert.All(_fixture.Spans, span =>
        {
            Assert.True(span.IsFinished);
            Assert.Equal(SpanStatus.Ok, span.Status);
        });
        // Check connections between spans.
        Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
        Assert.Equal(connectionSpan.SpanId, commandSpan.ParentSpanId);

        Assert.Equal(query, commandSpan.Description);
    }

    [Theory]
    [InlineData(SqlMicrosoftWriteConnectionOpenBeforeCommand, SqlMicrosoftWriteConnectionOpenAfterCommand, SqlMicrosoftWriteConnectionCloseAfterCommand)]
    [InlineData(SqlDataWriteConnectionOpenBeforeCommand, SqlDataWriteConnectionOpenAfterCommand, SqlDataWriteConnectionCloseAfterCommand)]
    public void OnNext_TwoConnectionSpansWithSameId_FinishBothWithOk(string connectionBeforeKey, string connectionUpdate, string connctionClose)
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentrySqlListener(hub, new SentryOptions());
        var connectionId = Guid.NewGuid();
        var connectionOperationIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        for (var i = 0; i < 2; i++)
        {
            interceptor.OnNext(
                new(connectionBeforeKey,
                    new { OperationId = connectionOperationIds[i] }));
            // Connection Id is set.
            interceptor.OnNext(
                new(connectionUpdate,
                    new { OperationId = connectionOperationIds[i], ConnectionId = connectionId }));
            interceptor.OnNext(
                new(connctionClose,
                    new { OperationId = connectionOperationIds[i], ConnectionId = connectionId }));
        }

        // Assert
        _fixture.Spans.Should().HaveCount(2);

        // Validate if all spans were finished.
        Assert.All(_fixture.Spans, span =>
        {
            Assert.True(span.IsFinished);
            Assert.Equal(SpanStatus.Ok, span.Status);
            Assert.Equal(connectionId, (Guid)span.Extra[SentrySqlListener.ConnectionExtraKey]);
        });
    }

    [Theory]
    [InlineData(SqlMicrosoftWriteConnectionOpenBeforeCommand, SqlMicrosoftWriteConnectionOpenAfterCommand, SqlMicrosoftWriteConnectionCloseAfterCommand, SqlMicrosoftBeforeExecuteCommand, SqlMicrosoftAfterExecuteCommand)]
    [InlineData(SqlDataWriteConnectionOpenBeforeCommand, SqlDataWriteConnectionOpenAfterCommand, SqlDataWriteConnectionCloseAfterCommand, SqlDataBeforeExecuteCommand, SqlDataAfterExecuteCommand)]
    public void OnNext_ExecuteQueryCalledBeforeConnectionId_ExecuteParentIsConnectionSpan(string connectionBeforeKey, string connectionUpdate, string connctionClose, string executeBeforeKey, string executeAfterKey)
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentrySqlListener(hub, new SentryOptions());
        var query = "SELECT * FROM ...";
        var connectionId = Guid.NewGuid();
        var connectionOperationId = Guid.NewGuid();
        var queryOperationId = Guid.NewGuid();

        // Act
        interceptor.OnNext(
            new(connectionBeforeKey,
                new { OperationId = connectionOperationId }));
        // Connection span has no connection ID and query will temporarily have Transaction as parent.
        interceptor.OnNext(
            new(executeBeforeKey,
                new { OperationId = queryOperationId, ConnectionId = connectionId }));
        // Connection Id is set.
        interceptor.OnNext(
            new(connectionUpdate,
                new { OperationId = connectionOperationId, ConnectionId = connectionId }));
        // Query sets ParentId to ConnectionId.
        interceptor.OnNext(
            new(executeAfterKey,
                new { OperationId = queryOperationId, ConnectionId = connectionId, Command = new { CommandText = query } }));
        interceptor.OnNext(
            new(connctionClose,
                new { OperationId = connectionOperationId, ConnectionId = connectionId }));

        // Assert
        _fixture.Spans.Should().HaveCount(2);
        var connectionSpan = _fixture.Spans.First(s => GetValidator(connectionBeforeKey)(s));
        var commandSpan = _fixture.Spans.First(s => GetValidator(executeBeforeKey)(s));

        // Validate if all spans were finished.
        Assert.All(_fixture.Spans, span =>
        {
            Assert.True(span.IsFinished);
            Assert.Equal(SpanStatus.Ok, span.Status);
        });
        // Check connections between spans.
        Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
        Assert.Equal(connectionSpan.SpanId, commandSpan.ParentSpanId);

        Assert.Equal(query, commandSpan.Description);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    public async Task OnNext_ParallelExecution_IsValid(int testNumber)
    {
        _ = testNumber;
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentrySqlListener(hub, _fixture.Options);
        var maxItems = 8;
        var query = "SELECT * FROM ...";
        var connectionsIds = Enumerable.Range(0, maxItems).Select(_ => Guid.NewGuid()).ToList();
        var connectionOperationsIds = Enumerable.Range(0, maxItems).Select(_ => Guid.NewGuid()).ToList();
        var connectionOperations2Ids = Enumerable.Range(0, maxItems).Select(_ => Guid.NewGuid()).ToList();
        var queryOperationsIds = Enumerable.Range(0, maxItems).Select(_ => Guid.NewGuid()).ToList();
        var queryOperations2Ids = Enumerable.Range(0, maxItems).Select(_ => Guid.NewGuid()).ToList();
        var evt = new ManualResetEvent(false);
        var ready = new ManualResetEvent(false);
        var counter = 0;

        // Act
        var taskList = Enumerable.Range(1, maxItems).Select(_ => Task.Run(() =>
        {
            var threadId = Interlocked.Increment(ref counter) - 1;

            if (threadId == maxItems - 1)
            {
                ready.Set();
            }
            evt.WaitOne();

            // 1 repeated connection  with 1 query where the first query will start before connection span gets the connectionId.
            void SimulateDbRequest(List<Guid> connectionOperationIds, List<Guid> queryOperationIds)
            {
                interceptor.OpenConnectionStart(connectionOperationIds[threadId]);
                interceptor.ExecuteQueryStart(queryOperationIds[threadId], connectionsIds[threadId]);
                interceptor.OpenConnectionStarted(connectionOperationIds[threadId], connectionsIds[threadId]);
                interceptor.ExecuteQueryFinish(queryOperationIds[threadId], connectionsIds[threadId], query);
                interceptor.OpenConnectionClose(connectionOperationIds[threadId], connectionsIds[threadId]);
            }
            SimulateDbRequest(connectionOperationsIds, queryOperationsIds);
            SimulateDbRequest(connectionOperations2Ids, queryOperations2Ids);
        })).ToList();
        ready.WaitOne();
        evt.Set();
        await Task.WhenAll(taskList);

        // Assert
        // 1 connection span and 1 query span, executed twice for 11 threads.
        _fixture.Spans.Should().HaveCount(2 * 2 * maxItems);

        var connectionSpans = _fixture.Spans.Where(span => span.Operation is "db.connection");
        var closedConnectionSpans = connectionSpans.Where(span => span.IsFinished);
        var querySpans = _fixture.Spans.Where(span => span.Operation is "db.query");
        var openSpans = _fixture.Spans.Where(span => !span.IsFinished);
        var closedSpans = _fixture.Spans.Where(span => span.IsFinished);

        // We have two connections per thread, despite having the same ConnectionId, both will be closed.
        closedConnectionSpans.Should().HaveCount(2 * maxItems);
        querySpans.Should().HaveCount(2 * maxItems);

        // Open Spans should not have any Connection key.
        Assert.All(openSpans, span => Assert.False(span.Extra.ContainsKey(SentrySqlListener.ConnectionExtraKey)));
        Assert.All(closedSpans, span => Assert.Equal(SpanStatus.Ok, span.Status));

        // Assert that all connectionIds is set and ParentId set to Trace.
        Assert.All(closedConnectionSpans, connectionSpan =>
        {
            Assert.NotNull(connectionSpan.Extra[SentrySqlListener.ConnectionExtraKey]);
            Assert.NotNull(connectionSpan.Extra[SentrySqlListener.OperationExtraKey]);
            Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
        });

        // Assert all Query spans have the correct ParentId Set and not the Transaction Id.
        Assert.All(querySpans, querySpan =>
        {
            Assert.True(querySpan.IsFinished);
            var connectionId = (Guid)querySpan.Extra[SentrySqlListener.ConnectionExtraKey];
            var connectionSpan = connectionSpans.Single(span => span.SpanId == querySpan.ParentSpanId);

            Assert.NotEqual(_fixture.Tracer.SpanId, querySpan.ParentSpanId);
            Assert.Equal((Guid)connectionSpan.Extra[SentrySqlListener.ConnectionExtraKey], connectionId);
        });

        _fixture.Logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public void OnNext_HappyPathWithError_TransactionWithErroredCommand()
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentrySqlListener(hub, new SentryOptions());
        var query = "SELECT * FROM ...";
        var connectionId = Guid.NewGuid();
        var connectionOperationId = Guid.NewGuid();
        var connectionOperationIdClosed = Guid.NewGuid();
        var queryOperationId = Guid.NewGuid();

        // Act
        interceptor.OnNext(
            new(SqlMicrosoftWriteConnectionOpenBeforeCommand,
                new { OperationId = connectionOperationId }));
        interceptor.OnNext(
            new(SqlMicrosoftWriteConnectionOpenAfterCommand,
                new { OperationId = connectionOperationId, ConnectionId = connectionId }));
        interceptor.OnNext(
            new(SqlMicrosoftBeforeExecuteCommand,
                new { OperationId = queryOperationId, ConnectionId = connectionId }));
        //Errored Query
        interceptor.OnNext(
            new(SqlMicrosoftWriteCommandError,
                new { OperationId = queryOperationId, ConnectionId = connectionId, Command = new { CommandText = query } }));
        interceptor.OnNext(
            new(SqlMicrosoftWriteConnectionCloseAfterCommand,
                new { OperationId = connectionOperationIdClosed, ConnectionId = connectionId }));

        // Assert
        _fixture.Spans.Should().HaveCount(2);

        var connectionSpan = _fixture.Spans.First(s => GetValidator(SqlMicrosoftWriteConnectionOpenBeforeCommand)(s));
        var commandSpan = _fixture.Spans.First(s => GetValidator(SqlMicrosoftBeforeExecuteCommand)(s));

        Assert.True(connectionSpan.IsFinished);
        Assert.Equal(SpanStatus.Ok, connectionSpan.Status);

        // Assert the failed command.
        Assert.True(commandSpan.IsFinished);
        Assert.Equal(SpanStatus.InternalError, commandSpan.Status);

        // Check connections between spans.
        Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
        Assert.Equal(connectionSpan.SpanId, commandSpan.ParentSpanId);

        Assert.Equal(query, commandSpan.Description);
    }

    [Fact]
    public void OnNext_ThrowsException_ExceptionIsolated()
    {
        // Arrange
        var hub = _fixture.Hub;
        var interceptor = new SentrySqlListener(hub, new SentryOptions());
        var exceptionReceived = false;

        // Act
        try
        {
            interceptor.OnNext(
                new(SqlMicrosoftWriteConnectionOpenBeforeCommand,
                    new ThrowToOperationClass()));
        }
        catch (Exception)
        {
            exceptionReceived = true;
        }

        // Assert
        Assert.False(exceptionReceived);
    }
}
