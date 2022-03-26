using System.Collections.Concurrent;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using Effort.Provider;

namespace Sentry.EntityFramework.Tests;

public class SentryQueryPerformanceListenerTests
{
    internal const string DbReaderKey = SentryQueryPerformanceListener.DbReaderKey;
    internal const string DbNonQueryKey = SentryQueryPerformanceListener.DbNonQueryKey;
    internal const string DbScalarKey = SentryQueryPerformanceListener.DbScalarKey;

    private class Fixture
    {
        public DbConnection DbConnection { get; }
        public TestDbContext DbContext { get; }
        public IHub Hub { get; }
        public ITransaction Tracer { get; }

        public ConcurrentBag<ISpan> Spans = new();

        public Fixture()
        {
            DbConnection = Effort.DbConnectionFactory.CreateTransient();
            DbContext = new TestDbContext(DbConnection, true);
            Hub = Substitute.For<IHub>();
            Tracer = Substitute.For<ITransaction>();
            Tracer.StartChild(Arg.Any<string>()).ReturnsForAnyArgs(AddSpan);
            Hub.GetSpan().ReturnsForAnyArgs(Tracer);
        }

        private ISpan AddSpan(CallInfo callInfo)
        {
            var span = Substitute.For<ISpan>();
            span.Operation = callInfo.Arg<string>();
            Spans.Add(span);
            return span;
        }

        public SentryQueryPerformanceListener GetListener()
            => new(Hub, new SentryOptions());
    }

    private readonly Fixture _fixture = new();

    [Theory]
    [InlineData(DbScalarKey)]
    [InlineData(DbNonQueryKey)]
    [InlineData(DbReaderKey)]
    public void interceptorInvoked_WithException_StartsSpan(string expectedOperation)
    {
        // Arrange
        var expected = new
        {
            Query = "Expected query string"
        };

        var interceptor = _fixture.GetListener();

        var command = new EffortCommand
        {
            CommandText = expected.Query
        };

        // Act
        switch (expectedOperation)
        {
            case DbScalarKey:
                interceptor.ScalarExecuting(command, new DbCommandInterceptionContext<object> { Exception = new Exception() });
                break;
            case DbNonQueryKey:
                interceptor.NonQueryExecuting(command, new DbCommandInterceptionContext<int> { Exception = new Exception() });
                break;
            case DbReaderKey:
                interceptor.ReaderExecuting(command, new DbCommandInterceptionContext<DbDataReader> { Exception = new Exception() });
                break;
            default:
                throw new NotImplementedException();
        }

        // Assert
        var span = _fixture.Spans.First();
        Assert.Equal(expected.Query, span.Description);
        Assert.Equal(expectedOperation, span.Operation);
        span.Received(0).Finish(Arg.Any<SpanStatus>());
    }

    [Theory]
    [InlineData(DbScalarKey)]
    [InlineData(DbNonQueryKey)]
    [InlineData(DbReaderKey)]
    public void InterceptorInvokeExecuted_WithException_CloseSpanWithError(string expectedOperation)
    {
        // Arrange
        var expected = new
        {
            Query = "Expected query string"
        };

        var interceptor = _fixture.GetListener();

        var command = new EffortCommand
        {
            CommandText = expected.Query
        };

        // Act
        switch (expectedOperation)
        {
            case DbScalarKey:
                {
                    var context = new DbCommandInterceptionContext<object> { Exception = new Exception() };
                    interceptor.ScalarExecuting(command, context);
                    interceptor.ScalarExecuted(command, context);
                }
                break;
            case DbNonQueryKey:
                {
                    var context = new DbCommandInterceptionContext<int> { Exception = new Exception() };
                    interceptor.NonQueryExecuting(command, context);
                    interceptor.NonQueryExecuted(command, context);
                }
                break;
            case DbReaderKey:
                {
                    var context = new DbCommandInterceptionContext<DbDataReader> { Exception = new Exception() };
                    interceptor.ReaderExecuting(command, context);
                    interceptor.ReaderExecuted(command, context);
                }
                break;
            default:
                throw new NotImplementedException();
        }

        // Assert
        var span = _fixture.Spans.First();
        Assert.Equal(expected.Query, span.Description);
        Assert.Equal(expectedOperation, span.Operation);
        span.Received(1).Finish(Arg.Any<Exception>());
    }

    [Theory]
    [InlineData(DbScalarKey)]
    [InlineData(DbNonQueryKey)]
    [InlineData(DbReaderKey)]
    public void InterceptorInvokeExecuted_WithoutException_CloseSpanWithOk(string expectedOperation)
    {
        // Arrange
        var expected = new
        {
            Query = "Expected query string"
        };

        var interceptor = _fixture.GetListener();

        var command = new EffortCommand
        {
            CommandText = expected.Query
        };

        // Act
        switch (expectedOperation)
        {
            case DbScalarKey:
                {
                    var context = new DbCommandInterceptionContext<object>();
                    interceptor.ScalarExecuting(command, context);
                    interceptor.ScalarExecuted(command, context);
                }
                break;
            case DbNonQueryKey:
                {
                    var context = new DbCommandInterceptionContext<int>();
                    interceptor.NonQueryExecuting(command, context);
                    interceptor.NonQueryExecuted(command, context);
                }
                break;
            case DbReaderKey:
                {
                    var context = new DbCommandInterceptionContext<DbDataReader>();
                    interceptor.ReaderExecuting(command, context);
                    interceptor.ReaderExecuted(command, context);
                }
                break;
            default:
                throw new NotImplementedException();
        }

        // Assert
        var span = _fixture.Spans.First();
        Assert.Equal(expected.Query, span.Description);
        Assert.Equal(expectedOperation, span.Operation);
        span.Received(1).Finish(Arg.Is<SpanStatus>(status => status == SpanStatus.Ok));
    }

    [Fact]
    public void FirstOrDefault_FromDatabase_CapturesQuery()
    {
        // Arrange
        var integration = new DbInterceptionIntegration();
        integration.Register(_fixture.Hub, new SentryOptions { TracesSampleRate = 1 });

        // Act
        _ = _fixture.DbContext.TestTable.FirstOrDefault();

        // Assert
        // This operation will result in one reading operation and two non scalar operations.
        _fixture.Hub.Received(3).GetSpan();
        // In-memory database doesn't have a CommandText so Description is expected to be null
        Assert.NotEmpty(_fixture.Spans.Where(
            span => DbNonQueryKey == span.Operation && span.Description is "CREATE SCHEMA (CodeFirstDatabase(dbo.__MigrationHistory(ContextKey(Effort.string)MigrationId(Effort.string)Model(Effort.binary)ProductVersion(Effort.string))))"));
        Assert.NotEmpty(_fixture.Spans.Where(
            span => DbNonQueryKey == span.Operation && span.Description is null));
        Assert.NotEmpty(_fixture.Spans.Where(
            span => DbReaderKey == span.Operation && span.Description is null));

        Assert.All(_fixture.Spans, span => span.Received(1).Finish(Arg.Is<SpanStatus>(status => SpanStatus.Ok == status)));
        integration.Unregister();
    }

    [Fact]
    public void Finish_NoActiveTransaction_LoggerNotCalled()
    {
        // Arrange
        var hub = _fixture.Hub;
        hub.GetSpan().Returns(_ => null);
        var logger = Substitute.For<ITestOutputHelper>();

        var options = new SentryOptions
        {
            Debug = true,
            DiagnosticLogger = new TestOutputDiagnosticLogger(logger)
        };

        var listener = new SentryQueryPerformanceListener(hub, options);

        // Act
        listener.ScalarExecuted(Substitute.For<DbCommand>(), Substitute.For<DbCommandInterceptionContext<object>>());

        // Assert
        logger.Received(0).WriteLine(Arg.Any<string>());
    }
}
