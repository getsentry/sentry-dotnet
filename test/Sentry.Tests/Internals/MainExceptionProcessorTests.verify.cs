namespace Sentry.Tests.Internals;

[UsesVerify]
public partial class MainExceptionProcessorTests
{
    [Fact]
    [UniqueForAot]
    public Task CreateSentryException_Aggregate()
    {
        var sut = _fixture.GetSut();
        var aggregateException = BuildAggregateException();

        var sentryException = sut.CreateSentryExceptions(aggregateException);

        return Verify(sentryException);
    }
}
