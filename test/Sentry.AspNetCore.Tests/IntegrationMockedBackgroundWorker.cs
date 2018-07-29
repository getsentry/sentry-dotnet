using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Testing;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    [Collection(nameof(SentrySdkCollection))]
    public class IntegrationMockedBackgroundWorker : SentrySdkTestFixture
    {
        protected IBackgroundWorker Worker { get; set; } = Substitute.For<IBackgroundWorker>();
        protected Action<SentryAspNetCoreOptions> Configure;

        protected override void ConfigureBuilder(WebHostBuilder builder)
        {
            builder.UseSentry(options =>
            {
                options.Dsn = DsnSamples.ValidDsnWithSecret;
                options.Init(i =>
                {
                    i.Worker(w => w.BackgroundWorker = Worker);
                });

                Configure?.Invoke(options);
            });
        }

        [Fact]
        public async Task Environment_OnOptions_ValueFromOptions()
        {
            const string expected = "environment";

            Configure = o => o.Environment = expected;

            Build();
            await HttpClient.GetAsync("/throw");

            Worker.Received(1).EnqueueEvent(Arg.Is<SentryEvent>(e => e.Environment == expected));
        }

        [Fact]
        public void Environment_NotOnOptions_ValueFromEnvVar()
        {
            const string expected = "environment";
            var target = new SentryOptions();

            EnvironmentVariableGuard.WithVariable("ASPNETCORE_ENVIRONMENT",
                expected,
                () =>
                {
                    Build();
                    HttpClient.GetAsync("/throw").GetAwaiter().GetResult();

                    Worker.Received(1).EnqueueEvent(Arg.Is<SentryEvent>(e => e.Environment == expected));
                });
        }

        [Fact]
        public void Environment_BothOnOptionsAndEnvVar_ValueFromOption()
        {
            const string expected = "environment";
            const string other = "other";

            Configure = o => o.Environment = expected;

            EnvironmentVariableGuard.WithVariable("ASPNETCORE_ENVIRONMENT",
                other,
                () =>
                {
                    Build();
                    HttpClient.GetAsync("/throw").GetAwaiter().GetResult();

                    Worker.Received(1).EnqueueEvent(Arg.Is<SentryEvent>(e => e.Environment == expected));
                });
        }
    }
}
