using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class SentrySqlListenerTests
    {
        internal const string SqlDataWriteConnectionOpenBeforeCommand = SentrySqlListener.SqlDataWriteConnectionOpenBeforeCommand;
        internal const string SqlMicrosoftWriteConnectionOpenBeforeCommand = SentrySqlListener.SqlMicrosoftWriteConnectionOpenBeforeCommand;

        internal const string SqlDataWriteConnectionCloseBeforeCommand = SentrySqlListener.SqlDataWriteConnectionCloseBeforeCommand;
        internal const string SqlMicrosoftWriteConnectionCloseBeforeCommand = SentrySqlListener.SqlMicrosoftWriteConnectionCloseBeforeCommand;

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

        private Func<ISpan, bool> GetValidator(string type) =>
    type switch
    {
        var x when
                x == SqlDataWriteConnectionOpenBeforeCommand ||
                x == SqlMicrosoftWriteConnectionOpenBeforeCommand ||
                x == SqlMicrosoftWriteConnectionOpenAfterCommand ||
                x == SqlDataWriteConnectionOpenAfterCommand ||
                x == SqlDataWriteConnectionCloseBeforeCommand ||
                x == SqlMicrosoftWriteConnectionCloseBeforeCommand ||
                x == SqlMicrosoftWriteConnectionCloseAfterCommand ||
                x == SqlDataWriteConnectionCloseAfterCommand
            => (span) => span.Description is null && span.Operation == "db.connection",
        var x when
                x == SqlDataBeforeExecuteCommand ||
                x == SqlMicrosoftBeforeExecuteCommand ||
                x == SqlDataAfterExecuteCommand ||
                x == SqlMicrosoftAfterExecuteCommand ||
                x == SqlDataWriteCommandError ||
                x == SqlMicrosoftWriteCommandError
            => (span) => span.Operation == "db.query",
        _ => throw new NotSupportedException()
    };

        private class ThrowToOperationClass
        {
            private string _operationId;
            public string OperationId {
                get => throw new Exception();
                set => _operationId = value; }
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
                Hub.When(x => x.WithScope(Arg.Any<Action<Scope>>()))
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
            if(addConnectionSpan)
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

        [Fact]
        public void OnNext_HappyPath_IsValid()
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
            interceptor.OnNext(
                new(SqlMicrosoftAfterExecuteCommand,
                new { OperationId = queryOperationId, ConnectionId = connectionId, Command = new { CommandText = query } }));
            interceptor.OnNext(
                new(SqlMicrosoftWriteConnectionCloseAfterCommand,
                 new { OperationId = connectionOperationIdClosed, ConnectionId = connectionId }));

            // Assert
            _fixture.Spans.Should().HaveCount(2);
            var connectionSpan = _fixture.Spans.First(s => GetValidator(SqlMicrosoftWriteConnectionOpenBeforeCommand)(s));
            var commandSpan = _fixture.Spans.First(s => GetValidator(SqlMicrosoftBeforeExecuteCommand)(s));

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
