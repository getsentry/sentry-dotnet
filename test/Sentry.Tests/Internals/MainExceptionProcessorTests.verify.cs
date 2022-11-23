#if !__MOBILE__
namespace Sentry.Tests.Internals;

[UsesVerify]
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

    [Fact]
    public Task CreateSentryException_Aggregate_Keep()
    {
        _fixture.SentryOptions.KeepAggregateException = true;
        var sut = _fixture.GetSut();
        var aggregateException = BuildAggregateException();

        var sentryException = sut.CreateSentryExceptions(aggregateException);

        return Verify(sentryException)
            .ScrubLines(x => x.Contains("One or more errors occurred"));
    }
}
#endif
