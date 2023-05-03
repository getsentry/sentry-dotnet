using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Sentry.AzureFunctions.Worker.Tests;

public class SentryFunctionsWorkerMiddlewareTests
{
    private class Fixture
    {
        //public RequestDelegate RequestDelegate { get; set; } = _ => Task.CompletedTask;
        public IHub Hub { get; set; } = Substitute.For<IHub>();
        public Scope Scope { get; set; }

        public Fixture()
        {
            Scope = new();
            Hub.When(hub => hub.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(callback => callback.Arg<Action<Scope>>().Invoke(Scope));

            // Hub.When(hub => hub.CaptureEvent(Arg.Any<SentryEvent>(), Arg.Any<Scope>()))
            //     .Do(_ => Scope.Evaluate());

            _ = Hub.IsEnabled.Returns(true);
        }

        public SentryFunctionsWorkerMiddleware GetSut() => new(Hub);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public async Task Original_exception_rethrown()
    {
        var hub = Substitute.For<IHub>();
        var functionContext = Substitute.For<FunctionContext>();

        var expected = new Exception("Kaboom, Riko!");
        FunctionExecutionDelegate functionExecutionDelegate = context => Task.FromException(expected);

        var sut = _fixture.GetSut();

        var actual = await Assert.ThrowsAsync<Exception>(async () => await sut.Invoke(functionContext, functionExecutionDelegate));

        actual.Should().BeSameAs(expected);
    }
}
