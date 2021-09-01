using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Sentry.Internals.DiagnosticSource;
using Xunit;

namespace Sentry.DiagnosticSource.Tests
{
    internal static class SentrySqlListenerExtensions
    {
        public static void OpenConnectionStart(this SentrySqlListener listener, Guid operation)
            => listener.OnNext(new KeyValuePair<string, object>(
                SentrySqlListener.SqlMicrosoftWriteConnectionOpenBeforeCommand,
            new { OperationId = operation }));

        public static void OpenConnectionFinish(this SentrySqlListener listener, Guid operationId, Guid connectionId)
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
                    => (span) => span.Description is null && span.Operation == "db.connection",
                _ when
                        type == SqlDataBeforeExecuteCommand ||
                        type == SqlMicrosoftBeforeExecuteCommand ||
                        type == SqlDataAfterExecuteCommand ||
                        type == SqlMicrosoftAfterExecuteCommand ||
                        type == SqlDataWriteCommandError ||
                        type == SqlMicrosoftWriteCommandError
                    => (span) => span.Operation == "db.query",
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

            public IReadOnlyCollection<ISpan> Spans => Tracer?.Spans;
            public IHub Hub { get; set; }
            public Fixture()
            {
                Tracer = new TransactionTracer(Hub, "foo", "bar");
                _scope = new Scope();
                _scope.Transaction = Tracer;
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
            Assert.All(_fixture.Spans, (span) =>
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
            Assert.All(_fixture.Spans, (span) =>
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
            Assert.All(_fixture.Spans, (span) =>
            {
                Assert.True(span.IsFinished);
                Assert.Equal(SpanStatus.Ok, span.Status);
            });
            // Check connections between spans.
            Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
            Assert.Equal(connectionSpan.SpanId, commandSpan.ParentSpanId);

            Assert.Equal(query, commandSpan.Description);
        }

        [Fact]
        public async Task OnNext_ParallelExecution_IsValid()
        {
            // Arrange
            var hub = _fixture.Hub;
            var interceptor = new SentrySqlListener(hub, new SentryOptions());
            int maxItems = 20;
            var query = "SELECT * FROM ...";
            var connectionsId = Enumerable.Range(1, maxItems).Select((_) => Guid.NewGuid()).ToList();
            var connectionOperationsId = Enumerable.Range(1, maxItems).Select((_) => Guid.NewGuid()).ToList();
            var connectionOperations2Id = Enumerable.Range(1, maxItems).Select((_) => Guid.NewGuid()).ToList();
            var queryOperationsId = Enumerable.Range(1, maxItems).Select((_) => Guid.NewGuid()).ToList();
            var evt = new ManualResetEvent(false);
            var ready = new ManualResetEvent(false);
            int counter = 0;
            // Act
            var taskList = Enumerable.Range(1, maxItems).Select((_) => Task.Run(async () =>
            {
                await Task.Delay(0);
                var threadId = Interlocked.Increment(ref counter) - 1;
                if (threadId == maxItems - 1)
                {
                    ready.Set();
                }
                // 2 repeated connections  with 2 queries where the first query will start before connection span gets the connectionId.
                evt.WaitOne();
                interceptor.OpenConnectionStart(connectionOperationsId[threadId]);
                interceptor.ExecuteQueryStart(queryOperationsId[threadId], connectionsId[threadId]);
                interceptor.OpenConnectionFinish(connectionOperationsId[threadId], connectionsId[threadId]);
                interceptor.ExecuteQueryFinish(queryOperationsId[threadId], connectionsId[threadId], query);
                interceptor.OpenConnectionClose(queryOperationsId[threadId], connectionsId[threadId]);

                // Next Connection Span will have the same Connection ID but different OperationId.
                interceptor.OpenConnectionStart(connectionOperations2Id[threadId]);
                interceptor.ExecuteQueryStart(queryOperationsId[threadId], connectionsId[threadId]);
                interceptor.OpenConnectionFinish(connectionOperations2Id[threadId], connectionsId[threadId]);
                interceptor.ExecuteQueryFinish(queryOperationsId[threadId], connectionsId[threadId], query);
                interceptor.OpenConnectionClose(connectionOperations2Id[threadId], connectionsId[threadId]);
            })).ToList();
            ready.WaitOne();
            evt.Set();
            await Task.WhenAll(taskList.AsParallel().Select(async task => await task));

            // Assert
            _fixture.Spans.Should().HaveCount(4 * maxItems);

            var openSpans = _fixture.Spans.Where(span => span.IsFinished is false);
            var closedSpans = _fixture.Spans.Where(span => span.IsFinished is true);
            var connectionSpans = _fixture.Spans.Where(span => span.Operation is "db.connection");
            var closedConnectionSpans = connectionSpans.Where(span => span.IsFinished);
            var querySpans = _fixture.Spans.Where(span => span.Operation is "db.query");
            Assert.All(openSpans, (span) => Assert.False(span.Extra.ContainsKey(SentrySqlListener.ConnectionExtraKey)));
            Assert.All(closedSpans, (span) => Assert.Equal(SpanStatus.Ok, span.Status));
            // Assert that all connectionIds will belong to a single connection Span and ParentId set to Trace.
            Assert.All(connectionsId, (connectionId) =>
            {
                var connectionSpan = Assert.Single(closedConnectionSpans.Where(span => (Guid)span.Extra[SentrySqlListener.ConnectionExtraKey] == connectionId));
                Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
            });
            // Assert all Query spans have the correct ParentId Set
            Assert.All(querySpans, (querySpan) =>
            {
                Assert.True(querySpan.IsFinished);
                var connectionId = (Guid)querySpan.Extra[SentrySqlListener.ConnectionExtraKey];
                var connectionSpan = connectionSpans.Where(span => span.Extra.ContainsKey(SentrySqlListener.ConnectionExtraKey) && (Guid)span.Extra[SentrySqlListener.ConnectionExtraKey] == connectionId);
                Assert.Single(connectionSpan);
                Assert.Equal(connectionSpan.First().SpanId, querySpan.ParentSpanId);
            });
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
}
