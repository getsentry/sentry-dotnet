using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

#if NETCOREAPP3_1_OR_GREATER
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif

namespace Sentry.AspNetCore.Tests;

public class SentryWebHostBuilderExtensionsTests
{
    public IWebHostBuilder WebHostBuilder { get; set; } = Substitute.For<IWebHostBuilder>();
    public ServiceCollection Services { get; set; } = new();
    public IConfiguration Configuration { get; set; } = Substitute.For<IConfiguration>();
    public IHostingEnvironment HostingEnvironment { get; set; } = Substitute.For<IHostingEnvironment>();

    public SentryWebHostBuilderExtensionsTests()
    {
        var context = new WebHostBuilderContext
        {
            Configuration = Configuration,
            HostingEnvironment = HostingEnvironment
        };

        WebHostBuilder
            .When(b => b.ConfigureServices(Arg.Any<Action<IServiceCollection>>()))
            .Do(i => i.Arg<Action<IServiceCollection>>()(Services));

        WebHostBuilder
            .When(b => b.ConfigureServices(Arg.Any<Action<WebHostBuilderContext, IServiceCollection>>()))
            .Do(i => i.Arg<Action<WebHostBuilderContext, IServiceCollection>>()(context, Services));
    }

    [Theory, MemberData(nameof(ExpectedServices))]
    public void UseSentry_ValidDsnString_ServicesRegistered(Action<IServiceCollection> assert)
    {
        _ = WebHostBuilder.UseSentry(ValidDsn);
        assert(Services);
    }

    [Theory, MemberData(nameof(ExpectedServices))]
    public void UseSentry_Parameterless_ServicesRegistered(Action<IServiceCollection> assert)
    {
        _ = WebHostBuilder.UseSentry();
        assert(Services);
    }

    [Theory, MemberData(nameof(ExpectedServices))]
    public void UseSentry_DisableDsnString_ServicesRegistered(Action<IServiceCollection> assert)
    {
        _ = WebHostBuilder.UseSentry(Sentry.SentryConstants.DisableSdkDsnValue);
        assert(Services);
    }

    [Theory, MemberData(nameof(ExpectedServices))]
    public void UseSentry_Callback_ServicesRegistered(Action<IServiceCollection> assert)
    {
        _ = WebHostBuilder.UseSentry(o => o.InitializeSdk = false);
        assert(Services);
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
