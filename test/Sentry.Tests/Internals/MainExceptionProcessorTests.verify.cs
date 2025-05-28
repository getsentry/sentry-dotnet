namespace Sentry.Tests.Internals;

public partial class MainExceptionProcessorTests
{
    [Fact]
    public Task CreateSentryException_Aggregate()
    {
        var sut = _fixture.GetSut();
        var aggregateException = BuildAggregateException();

        var sentryException = sut.CreateSentryExceptions(aggregateException);

        return Verify(sentryException);
    }
}
