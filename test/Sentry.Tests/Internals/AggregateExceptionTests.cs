using Sentry.PlatformAbstractions;

namespace Sentry.Tests.Internals;

public class AggregateExceptionTests
{
    private static readonly string DefaultAggregateExceptionMessage = new AggregateException().Message;

    [Fact]
    public void AggregateException_GetRawMessage_Empty()
    {
        var exception = new AggregateException();

        var rawMessage = exception.GetRawMessage();

        Assert.Equal(DefaultAggregateExceptionMessage, rawMessage);
    }

    [Fact]
    public void AggregateException_GetRawMessage_WithInnerExceptions()
    {
        var exception = GetTestAggregateException();

        var rawMessage = exception.GetRawMessage();

        Assert.Equal(DefaultAggregateExceptionMessage, rawMessage);
    }

    [SkippableFact]
    public void AggregateException_GetRawMessage_DiffersFromMessage()
    {
        // Sanity check: The message should be different than the raw message, except on full .NET Framework.
        // .NET, .NET Core, and Mono all override the Message property to append messages from the inner exceptions.
        // .NET Framework does not.

        Skip.If(RuntimeInfo.GetRuntime().IsNetFx());

        var exception = GetTestAggregateException();

        var rawMessage = exception.GetRawMessage();

        Assert.NotEqual(exception.Message, rawMessage);
    }

    private static AggregateException GetTestAggregateException() =>
        Assert.Throws<AggregateException>(() =>
        {
            var t1 = Task.Run(() => throw new Exception("Test 1"));
            var t2 = Task.Run(() => throw new Exception("Test 2"));
            Task.WaitAll(t1, t2);
        });
}
