namespace Sentry.Serilog.Tests;

public class SentryOptionExtensionsTests
{
    [Fact]
    public void UseSerilog_AddsSerilogScopeEventProcessor()
    {
        var options = new SentryOptions();

        options.UseSerilog();

        options.GetAllEventProcessors().OfType<SerilogScopeEventProcessor>().Should().ContainSingle();
    }

    [Fact]
    public void UseSerilog_CalledConcurrently_AddsProcessorOnce()
    {
        var options = new SentryOptions();

        Parallel.For(0, 64, _ => options.UseSerilog());

        options.GetAllEventProcessors().OfType<SerilogScopeEventProcessor>().Should().ContainSingle();
    }

    [Fact]
    public void UseSerilog_CalledTwice_AddsProcessorOnce()
    {
        var options = new SentryOptions();

        options.UseSerilog();
        options.UseSerilog();

        options.GetAllEventProcessors().OfType<SerilogScopeEventProcessor>().Should().ContainSingle();
    }
}
