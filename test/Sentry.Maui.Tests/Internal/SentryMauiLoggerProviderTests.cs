#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;
using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests.Internal;

public class SentryMauiLoggerProviderTests
{
    private class Fixture
    {
        public IOptions<SentryMauiOptions> Options { get; }
        public IHub Hub { get; }
        public MockClock Clock { get; }

        public Fixture()
        {
            var loggingOptions = new SentryMauiOptions();

            Options = Microsoft.Extensions.Options.Options.Create(loggingOptions);
            Hub = Substitute.For<IHub>();
            Clock = new MockClock();
        }

        public SentryMauiLoggerProvider GetSut()
        {
            return new SentryMauiLoggerProvider(Options.Value, Hub, Clock);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Type_CustomAttributes_HasProviderAliasAttribute()
    {
        var type = typeof(SentryMauiLoggerProvider);

        type.GetCustomAttributes<ProviderAliasAttribute>().Should()
            .ContainSingle().Which
            .Alias.Should().Be("Sentry");
    }

    [Fact]
    public void Ctor_DependencyInjection_CanCreate()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ILoggerProvider, SentryMauiLoggerProvider>()
            .AddSingleton(_fixture.Options)
            .AddSingleton(_fixture.Hub)
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<SentryMauiLoggerProviderTests>>();

        logger.Should().BeOfType<Logger<SentryMauiLoggerProviderTests>>();
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
