#if NET6_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentry.AspNetCore.TestUtils;

namespace Sentry.Serilog.Tests;

[Collection(nameof(SentrySdkCollection))]
public class AspNetCoreIntegrationTests : SerilogAspNetSentrySdkTestFixture
{
    [Fact]
    public async Task UnhandledException_MarkedAsUnhandled()
    {
        var handler = new RequestHandler
        {
            Path = "/throw",
            Handler = _ => throw new Exception("test")
        };

        Handlers = new[] { handler };
        Build();
        await HttpClient.GetAsync(handler.Path);

        Assert.Contains(Events, e => e.Logger == "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware");
        Assert.Collection(Events, @event => Assert.Collection(@event.SentryExceptions, x => Assert.False(x.Mechanism?.Handled)));
    }

    [Fact]
    public async Task StructuredLogging_Disabled()
    {
        Assert.False(EnableLogs);

        var handler = new RequestHandler
        {
            Path = "/log",
            Handler = context =>
            {
                context.RequestServices.GetRequiredService<ILogger<AspNetCoreIntegrationTests>>().LogInformation("Hello, World!");
                return Task.CompletedTask;
            }
        };

        Handlers = new[] { handler };
        Build();
        await HttpClient.GetAsync(handler.Path);
        await ServiceProvider.GetRequiredService<IHub>().FlushAsync();

        Assert.Empty(Logs);
    }

    [Fact]
    public async Task StructuredLogging_Enabled()
    {
        EnableLogs = true;

        var handler = new RequestHandler
        {
            Path = "/log",
            Handler = context =>
            {
                context.RequestServices.GetRequiredService<ILogger<AspNetCoreIntegrationTests>>().LogInformation("Hello, World!");
                return Task.CompletedTask;
            }
        };

        Handlers = new[] { handler };
        Build();
        await HttpClient.GetAsync(handler.Path);
        await ServiceProvider.GetRequiredService<IHub>().FlushAsync();

        Assert.NotEmpty(Logs);
        Assert.Contains(Logs, log => log.Level == SentryLogLevel.Info && log.Message == "Hello, World!");
    }

    // Logging through ILogger from a namespace that merely starts with "Sentry" used to be discarded
    // by the sink, so nothing was captured and event processors never ran.
    // https://github.com/getsentry/sentry-dotnet/issues/5265
    [Fact]
    public async Task ILoggerFromApplicationNamespaceStartingWithSentry_CapturesEventAndRunsEventProcessors()
    {
        // The namespace our own ASP.NET Core Serilog sample uses, which is where the report came from.
        const string category = "Sentry.Samples.AspNetCore.Serilog.Program";

        var processor = new RecordingEventProcessor();
        ConfigureServices = services => services.AddSingleton<ISentryEventProcessor>(processor);

        var handler = new RequestHandler
        {
            Path = "/log",
            Handler = context =>
            {
                context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(category)
                    .LogError(new InvalidOperationException("hello?"), "This is a problem");
                return Task.CompletedTask;
            }
        };

        Handlers = new[] { handler };
        Build();
        await HttpClient.GetAsync(handler.Path);
        await ServiceProvider.GetRequiredService<IHub>().FlushAsync();

        Assert.Contains(Events, e => e.Logger == category);
        Assert.True(processor.Invoked);
    }

    private class RecordingEventProcessor : ISentryEventProcessor
    {
        public bool Invoked { get; private set; }

        public SentryEvent Process(SentryEvent @event)
        {
            Invoked = true;
            return @event;
        }
    }
}
#endif
