namespace Sentry.EntityFramework.Tests;

[Collection("Sequential")]
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
        public ITransactionTracer Tracer { get; }

        public ConcurrentBag<ISpan> Spans = new();

        public Fixture()
        {
            DbConnection = Effort.DbConnectionFactory.CreateTransient();
            DbContext = new TestDbContext(DbConnection, true);
            Hub = Substitute.For<IHub>();
            Tracer = Substitute.For<ITransactionTracer>();
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
    private readonly IDiagnosticLogger _logger;

    public SentryQueryPerformanceListenerTests(ITestOutputHelper output)
    {
        _logger = Substitute.ForPartsOf<TestOutputDiagnosticLogger>(output, SentryLevel.Debug);
    }

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
                interceptor.ScalarExecuting(command, new DbCommandInterceptionContext<object> { Exception = new() });
                break;
            case DbNonQueryKey:
                interceptor.NonQueryExecuting(command, new DbCommandInterceptionContext<int> { Exception = new() });
                break;
            case DbReaderKey:
                interceptor.ReaderExecuting(command, new DbCommandInterceptionContext<DbDataReader> { Exception = new() });
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
                    var context = new DbCommandInterceptionContext<object> { Exception = new() };
                    interceptor.ScalarExecuting(command, context);
                    interceptor.ScalarExecuted(command, context);
                }
                break;
            case DbNonQueryKey:
                {
                    var context = new DbCommandInterceptionContext<int> { Exception = new() };
                    interceptor.NonQueryExecuting(command, context);
                    interceptor.NonQueryExecuted(command, context);
                }
                break;
            case DbReaderKey:
                {
                    var context = new DbCommandInterceptionContext<DbDataReader> { Exception = new() };
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
        Assert.Contains(_fixture.Spans, span => DbNonQueryKey == span.Operation && span.Description is "CREATE SCHEMA (CodeFirstDatabase(dbo.__MigrationHistory(ContextKey(Effort.string)MigrationId(Effort.string)Model(Effort.binary)ProductVersion(Effort.string))))");
        Assert.Contains(_fixture.Spans, span => DbNonQueryKey == span.Operation && span.Description is null);
        Assert.Contains(_fixture.Spans, span => DbReaderKey == span.Operation && span.Description is null);

        Assert.All(_fixture.Spans, span => span.Received(1).Finish(Arg.Is<SpanStatus>(status => SpanStatus.Ok == status)));
        integration.Unregister();
    }

    [Fact]
    public void Finish_NoActiveTransaction_LoggerNotCalled()
    {
        // Arrange
        var hub = _fixture.Hub;
        hub.GetSpan().ReturnsNull();

        var options = new SentryOptions
        {
            Debug = true,
            DiagnosticLogger = _logger
        };

        var listener = new SentryQueryPerformanceListener(hub, options);

        // Act
        listener.ScalarExecuted(Substitute.For<DbCommand>(), Substitute.For<DbCommandInterceptionContext<object>>());

        // Assert
        _logger.DidNotReceiveWithAnyArgs().Log(default, default!);
    }
}
