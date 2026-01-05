#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore.Tests;

public class SentryAspNetCoreLoggerProviderTests
{
    private class Fixture
    {
        public IOptions<SentryAspNetCoreOptions> Options { get; }
        public IHub Hub { get; }
        public MockClock Clock { get; }

        public Fixture()
        {
            var loggingOptions = new SentryAspNetCoreOptions();

            Options = Microsoft.Extensions.Options.Options.Create(loggingOptions);
            Hub = Substitute.For<IHub>();
            Clock = new MockClock();
        }

        public SentryAspNetCoreLoggerProvider GetSut()
        {
            return new SentryAspNetCoreLoggerProvider(Options.Value, Hub, Clock);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Type_CustomAttributes_HasProviderAliasAttribute()
    {
        var type = typeof(SentryAspNetCoreLoggerProvider);

        type.GetCustomAttributes<ProviderAliasAttribute>().Should()
            .ContainSingle().Which
            .Alias.Should().Be("Sentry");
    }

    [Fact]
    public void Ctor_DependencyInjection_CanCreate()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>()
            .AddSingleton(_fixture.Options)
            .AddSingleton(_fixture.Hub)
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<SentryAspNetCoreLoggerProviderTests>>();

        logger.Should().BeOfType<Logger<SentryAspNetCoreLoggerProviderTests>>();
    }

    [Fact]
    public void CreateLogger_OfType()
    {
        var provider = _fixture.GetSut();

        var logger = provider.CreateLogger("CategoryName");

        logger.Should().BeOfType<SentryLogger>()
            .Which.CategoryName.Should().Be("CategoryName");
    }
}
