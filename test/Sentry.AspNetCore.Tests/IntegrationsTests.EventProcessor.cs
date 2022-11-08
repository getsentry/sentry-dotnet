using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore.Tests;

public partial class IntegrationsTests
{
    [Fact]
    public async Task SentryEventProcessor_Transient_LifetimeRespected()
    {
        var processorsResolved = new List<ISentryEventProcessor>();
        ConfigureServices = c => c.AddTransient(_ =>
        {
            var processor = Substitute.For<ISentryEventProcessor>();
            processorsResolved.Add(processor);
            return processor;
        });

        Build();
        _ = await HttpClient.GetAsync("/throw");
        _ = await HttpClient.GetAsync("/throw");

        Assert.Equal(2, processorsResolved.Count);
        _ = processorsResolved[0].Received(1).Process(Arg.Any<SentryEvent>());
        _ = processorsResolved[1].Received(1).Process(Arg.Any<SentryEvent>());
    }

    [Fact]
    public async Task SentryEventExceptionProcessor_Transient_LifetimeRespected()
    {
        var exceptionProcessorsResolved = new List<ISentryEventExceptionProcessor>();
        ConfigureServices = c => c.AddTransient(_ =>
        {
            var processor = Substitute.For<ISentryEventExceptionProcessor>();
            exceptionProcessorsResolved.Add(processor);
            return processor;
        });

        Build();
        _ = await HttpClient.GetAsync("/throw");
        _ = await HttpClient.GetAsync("/throw");

        Assert.Equal(2, exceptionProcessorsResolved.Count);
        exceptionProcessorsResolved[0].Received(1).Process(Arg.Any<Exception>(), Arg.Any<SentryEvent>());
        exceptionProcessorsResolved[1].Received(1).Process(Arg.Any<Exception>(), Arg.Any<SentryEvent>());
    }
}
