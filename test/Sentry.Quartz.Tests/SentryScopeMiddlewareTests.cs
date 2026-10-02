using Quartz;

namespace Sentry.Quartz.Tests;

public class SentryScopeMiddlewareTests
{
    [Fact]
    public async Task Invoke_PushesScopeBeforeNext_AndDisposesItAfter()
    {
        var events = new List<string>();
        var hub = Substitute.For<IHub>();
        var scope = Substitute.For<IDisposable>();
        scope.When(s => s.Dispose()).Do(_ => events.Add("dispose"));
        hub.PushScope().Returns(_ =>
        {
            events.Add("push");
            return scope;
        });

        var sut = new SentryScopeMiddleware(hub);
        var context = Substitute.For<IJobExecutionContext>();
        JobExecutionDelegate next = (_, _) =>
        {
            events.Add("next");
            return default;
        };

        await sut.Invoke(context, next, CancellationToken.None);

        events.Should().Equal("push", "next", "dispose");
    }

    [Fact]
    public async Task Invoke_JobThrows_StillDisposesScope_AndRethrows()
    {
        var hub = Substitute.For<IHub>();
        var scope = Substitute.For<IDisposable>();
        hub.PushScope().Returns(scope);

        var sut = new SentryScopeMiddleware(hub);
        var context = Substitute.For<IJobExecutionContext>();
        var exception = new InvalidOperationException("boom");

        var act = () => sut.Invoke(context, MiddlewareTestHelpers.Next(exception), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>();
        scope.Received(1).Dispose();
    }
}
