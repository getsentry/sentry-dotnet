namespace Sentry.EntityFramework.Tests;

public class SentryOptionsExtensionsTests
{
    [Fact]
    public void AddEntityFramework_UsedMoreThanOnce_RegisterOnce()
    {
        var options = new SentryOptions();

        options.AddEntityFramework();
        options.AddEntityFramework();

        Assert.Single(options.ExceptionProcessors!, x => x.Lazy.Value is DbEntityValidationExceptionProcessor);
    }

    [Fact]
    public void AddEntityFramework_UsedMoreThanOnce_LogWarning()
    {
        var options = new SentryOptions();
        var logger = new InMemoryDiagnosticLogger();

        options.DiagnosticLogger = logger;
        options.Debug = true;

        options.AddEntityFramework();
        options.AddEntityFramework();

        Assert.Single(logger.Entries, x => x.Level == SentryLevel.Warning
                                           && x.Message.Contains("Subsequent call will be ignored."));
    }
}
