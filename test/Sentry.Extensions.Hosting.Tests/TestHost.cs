using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Hosting.Tests;

public enum BuilderKind
{
    HostBuilder,
    HostApplicationBuilder,
}

internal sealed class TestHost
{
    private readonly List<Action<IHostBuilder>> _hostBuilderSteps = new();
#if NET8_0_OR_GREATER
    private readonly List<Action<IHostApplicationBuilder>> _applicationBuilderSteps = new();
#endif

    public string EnvironmentName { get; set; } = "Production";

    public Dictionary<string, string?> Settings { get; } = new();

    public string? Json { get; set; }

    public static TheoryData<BuilderKind> Builders
    {
        get
        {
            var builders = new TheoryData<BuilderKind> { BuilderKind.HostBuilder };
#if NET8_0_OR_GREATER
            builders.Add(BuilderKind.HostApplicationBuilder);
#endif
            return builders;
        }
    }

    public TestHost UseSentry(Action<SentryGenericHostOptions>? configureOptions = null)
    {
        _hostBuilderSteps.Add(builder => builder.UseSentry(configureOptions));
#if NET8_0_OR_GREATER
        _applicationBuilderSteps.Add(builder => builder.UseSentry(configureOptions));
#endif
        return this;
    }

    public TestHost ConfigureServices(Action<IServiceCollection> configureServices)
    {
        _hostBuilderSteps.Add(builder => builder.ConfigureServices((_, services) => configureServices(services)));
#if NET8_0_OR_GREATER
        _applicationBuilderSteps.Add(builder => configureServices(builder.Services));
#endif
        return this;
    }

    public TestHost ClearLoggerProviders()
        => ConfigureServices(services => services.AddLogging(logging => logging.ClearProviders()));

    public IHost Build(BuilderKind kind)
    {
        if (kind == BuilderKind.HostBuilder)
        {
            var hostBuilder = new HostBuilder()
                .UseEnvironment(EnvironmentName)
                .ConfigureAppConfiguration(configuration => AddConfiguration(configuration));
            foreach (var step in _hostBuilderSteps)
            {
                step(hostBuilder);
            }

            return hostBuilder.Build();
        }

#if NET8_0_OR_GREATER
        var applicationBuilder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = EnvironmentName,
            DisableDefaults = true,
        });
        AddConfiguration(applicationBuilder.Configuration);
        foreach (var step in _applicationBuilderSteps)
        {
            step(applicationBuilder);
        }

        return applicationBuilder.Build();
#else
        throw new NotSupportedException($"{kind} needs .NET 8 or later.");
#endif
    }

    private void AddConfiguration(IConfigurationBuilder configuration)
    {
        configuration.AddInMemoryCollection(Settings);
        if (Json is not null)
        {
            configuration.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(Json)));
        }
    }
}
