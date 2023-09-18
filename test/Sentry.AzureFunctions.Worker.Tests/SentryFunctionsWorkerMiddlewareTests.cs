using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Sentry.AzureFunctions.Worker.Tests;

public class SentryFunctionsWorkerMiddlewareTests
{
    private class Fixture
    {
        public IHub Hub { get; set; }
        public IInternalScopeManager ScopeManager { get; }
        public Transaction Transaction { get; set; }

        public Fixture()
        {
            var options = new SentryOptions
            {
                Dsn = ValidDsn,
                EnableTracing = true,
            };

            var client = Substitute.For<ISentryClient>();
            var sessionManager = Substitute.For<ISessionManager>();

            client.When(x => x.CaptureTransaction(Arg.Any<Transaction>(), Arg.Any<Hint>()))
                .Do(callback => Transaction = callback.Arg<Transaction>());

            ScopeManager = new SentryScopeManager(options, client);
            Hub = new Hub(options, client, sessionManager, new MockClock(), ScopeManager);
        }

        public SentryFunctionsWorkerMiddleware GetSut() => new(Hub);
    }

    private readonly Fixture _fixture = new();

    [Function(nameof(ThrowingHttpFunction))]
    private Task<HttpResponseData> ThrowingHttpFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        throw new Exception("Kaboom, Riko!");
    }

    [Function(nameof(HttpFunction))]
    private Task<HttpResponseData> HttpFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        return Task.FromResult(Substitute.For<HttpResponseData>(executionContext));
    }

    [Fact]
    public async Task Original_exception_rethrown()
    {
        var functionContext = Substitute.For<FunctionContext>();
        var expected = "Kaboom, Riko!";
        Task FunctionExecutionDelegate(FunctionContext context) => ThrowingHttpFunction(null, context);

        var sut = _fixture.GetSut();

        var actual = await Assert.ThrowsAsync<Exception>(async () => await sut.Invoke(functionContext, FunctionExecutionDelegate));

        actual.Message.Should().Be(expected);
    }

    [Fact]
    public async Task Transaction_name_and_operation_set()
    {
        var functionContext = Substitute.For<FunctionContext>();
        var functionDefinition = Substitute.For<FunctionDefinition>();
        functionContext.FunctionDefinition.Returns(functionDefinition);
        functionDefinition.Name.Returns(nameof(HttpFunction));

        var sut = _fixture.GetSut();

        await sut.Invoke(functionContext, context => HttpFunction(null, context));

        var transaction = _fixture.Transaction;

        transaction.Should().NotBeNull();
        transaction.Name.Should().Be(functionDefinition.Name);
        transaction.Operation.Should().Be("function");
    }

    [Fact]
    public async Task Tags_set()
    {
        var functionContext = Substitute.For<FunctionContext>();
        var functionDefinition = Substitute.For<FunctionDefinition>();
        functionContext.FunctionDefinition.Returns(functionDefinition);
        functionDefinition.Name.Returns(nameof(HttpFunction));

        var sut = _fixture.GetSut();

        await sut.Invoke(functionContext, context => HttpFunction(null, context));

        var scope = _fixture.ScopeManager.GetCurrent().Key;
        var context = scope.Contexts["function"].As<Dictionary<string, string>>();

        context.Should().NotBeNull();
        context.Count.Should().Be(3);
        context["name"].Should().Be(functionDefinition.Name);
        context["entryPoint"].Should().Be(functionDefinition.EntryPoint);
        context["invocationId"].Should().Be(functionContext.InvocationId);
    }

    [Fact]
    public async Task Unhandled_exception_sets_mechanism()
    {
        var functionContext = Substitute.For<FunctionContext>();

        var sut = _fixture.GetSut();

        var actual = await Assert.ThrowsAsync<Exception>(async () => await sut.Invoke(functionContext, context => ThrowingHttpFunction(null, context)));

        actual.Data[Mechanism.MechanismKey].Should().Be(nameof(SentryFunctionsWorkerMiddleware));
        actual.Data[Mechanism.HandledKey].Should().Be(false);
        actual.Data[Mechanism.DescriptionKey].Should().NotBeNull();
    }

    [Fact]
    public async Task Skips_function_invocation_when_cancellation_requested()
    {
        var functionContext = Substitute.For<FunctionContext>();
        functionContext.CancellationToken.Returns(new CancellationToken(canceled: true));
        var functionInvoked = false;

        var sut = _fixture.GetSut();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => sut.Invoke(functionContext, _ =>
        {
            functionInvoked = true;
            return Task.CompletedTask;
        }));

        functionInvoked.Should().BeFalse();
    }

    [Fact]
    public void StartOrContinueTraceAsync_HeadersPresentInContext_ContinuesTrace()
    {
        var transactionName = "test-name";
        var traceId = SentryId.Parse("38cd75cb85944900b68b79d61b195606");
        var spanId = SpanId.Parse("9f7dd7a8c909ff80");

        var functionContext = Substitute.For<FunctionContext>();
        var functionDefinition = Substitute.For<FunctionDefinition>();
        functionContext.FunctionDefinition.Returns(functionDefinition);
        functionDefinition.Name.Returns(nameof(HttpFunction));
        var requestData = Substitute.For<HttpRequestData>(functionContext);
        requestData.Method.Returns("GET");
        requestData.Headers.Returns(new HttpHeadersCollection {{ "sentry-trace", $"{traceId}-{spanId}" }});
        // To skip the whole loading of assembly shenanigans
        SentryFunctionsWorkerMiddleware.TransactionNameCache.Add($"{functionDefinition.EntryPoint}-GET", transactionName);

        var sut = _fixture.GetSut();

        var transactionContext = sut.StartOrContinueTrace(functionContext, requestData);

        transactionContext.Name.Should().Be(transactionName);
        transactionContext.TraceId.Should().Be(traceId);
        transactionContext.ParentSpanId.Should().Be(spanId);
    }
}
