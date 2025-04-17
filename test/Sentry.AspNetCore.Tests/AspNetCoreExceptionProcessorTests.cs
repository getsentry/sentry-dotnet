namespace Sentry.AspNetCore.Tests;

public class AspNetCoreExceptionProcessorTests
{
    private readonly AspNetCoreExceptionProcessor _sut = new();

    [Fact]
    public void Process_Event()
    {
        var @event = new SentryEvent();
        @event.Logger = "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware";

        _sut.Process(null!, @event);

        foreach (var ex in @event.SentryExceptions)
        {
            ex.Mechanism.Should().NotBeNull();
            ex.Mechanism.Type.Should().Be("ExceptionHandlerMiddleware");
            ex.Mechanism.Handled.Should().BeFalse();
        }
    }


    [Fact]
    public void Process_ShouldNotOverwriteMechanism()
    {
        var @event = new SentryEvent();
        var mechanism = new Mechanism();
        mechanism.Data.Add("key", "value");

        @event.SentryExceptions = [new SentryException
        {
            Mechanism = mechanism
        }];
        @event.Logger = "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware";

        _sut.Process(null!, @event);

        var mech = @event.SentryExceptions.First().Mechanism;
        mech.Should().NotBeNull();
        mech.Data.First().Key.Should().Be("key");
        mech.Data.First().Value.Should().Be("value");
    }
}
