using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore.Tests;

public class SentryWebHostBuilderExtensionsTests
{
    private class Fixture
    {
        public IWebHostBuilder WebHostBuilder { get; } = Substitute.For<IWebHostBuilder>();
        public ServiceCollection Services { get; } = new();

        public Fixture()
        {
            var context = new WebHostBuilderContext
            {
                Configuration = Substitute.For<IConfiguration>(),
                HostingEnvironment = Substitute.For<IWebHostEnvironment>()
            };

            WebHostBuilder
                .When(b => b.ConfigureServices(Arg.Any<Action<IServiceCollection>>()))
                .Do(i => i.Arg<Action<IServiceCollection>>()(Services));

            WebHostBuilder
                .When(b => b.ConfigureServices(Arg.Any<Action<WebHostBuilderContext, IServiceCollection>>()))
                .Do(i => i.Arg<Action<WebHostBuilderContext, IServiceCollection>>()(context, Services));
        }
    }

    private readonly Fixture _fixture = new();

    [Theory, MemberData(nameof(ExpectedServices))]
    public void UseSentry_ValidDsnString_ServicesRegistered(Action<IServiceCollection> assert)
    {
        _ = _fixture.WebHostBuilder.UseSentry(DsnSamples.ValidDsnWithoutSecret);
        assert(_fixture.Services);
    }

    [Theory, MemberData(nameof(ExpectedServices))]
    public void UseSentry_Parameterless_ServicesRegistered(Action<IServiceCollection> assert)
    {
        _ = _fixture.WebHostBuilder.UseSentry();
        assert(_fixture.Services);
    }

    [Theory, MemberData(nameof(ExpectedServices))]
    public void UseSentry_DisableDsnString_ServicesRegistered(Action<IServiceCollection> assert)
    {
        _ = _fixture.WebHostBuilder.UseSentry(Sentry.Constants.DisableSdkDsnValue);
        assert(_fixture.Services);
    }

    [Theory, MemberData(nameof(ExpectedServices))]
    public void UseSentry_Callback_ServicesRegistered(Action<IServiceCollection> assert)
    {
        _ = _fixture.WebHostBuilder.UseSentry(o => o.InitializeSdk = false);
        assert(_fixture.Services);
    }

    public static IEnumerable<object[]> ExpectedServices()
    {
        yield return new object[] {
            new Action<IServiceCollection>(c =>
                Assert.Single(c, d => d.ServiceType == typeof(IHub)))};
        yield return new object[] {
            new Action<IServiceCollection>(c =>
                Assert.Single(c, d => d.ImplementationType == typeof(SentryAspNetCoreLoggerProvider)))};
        yield return new object[] {
            new Action<IServiceCollection>(c =>
                Assert.Single(c, d => d.ImplementationType == typeof(SentryStartupFilter)))};
    }
}
