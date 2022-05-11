using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

#if !NETCOREAPP3_0_OR_GREATER
using IHostEnvironment = Microsoft.Extensions.Hosting.IHostingEnvironment;
#endif

namespace Sentry.Extensions.Logging.Tests
{
    public class SentryHostBuilderExtensionsTests
    {
        private class Fixture
        {
            public IHostBuilder HostBuilder { get; } = Substitute.For<IHostBuilder>();
            public ServiceCollection Services { get; } = new();

            public Fixture()
            {
                var context = new HostBuilderContext(new Dictionary<object, object>())
                {
                    Configuration = Substitute.For<IConfiguration>(),
                    HostingEnvironment = Substitute.For<IHostEnvironment>()
                };

                HostBuilder
                    .When(b => b.ConfigureServices(Arg.Any<Action<HostBuilderContext, IServiceCollection>>()))
                    .Do(i => i.Arg<Action<HostBuilderContext, IServiceCollection>>()(context, Services));
            }
        }

        private readonly Fixture _fixture = new();

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_ValidDsnString_ServicesRegistered(Action<IServiceCollection> assert)
        {
            _ = _fixture.HostBuilder.UseSentry(DsnSamples.ValidDsnWithoutSecret);
            assert(_fixture.Services);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_Parameterless_ServicesRegistered(Action<IServiceCollection> assert)
        {
            _ = _fixture.HostBuilder.UseSentry();
            assert(_fixture.Services);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_DisableDsnString_ServicesRegistered(Action<IServiceCollection> assert)
        {
            _ = _fixture.HostBuilder.UseSentry(Sentry.Constants.DisableSdkDsnValue);
            assert(_fixture.Services);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_Callback_ServicesRegistered(Action<IServiceCollection> assert)
        {
            _ = _fixture.HostBuilder.UseSentry(o => o.InitializeSdk = false);
            assert(_fixture.Services);
        }

        public static IEnumerable<object[]> ExpectedServices()
        {
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ServiceType == typeof(IHub)))};
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ImplementationType == typeof(SentryLoggerProvider)))};
        }
    }
}
