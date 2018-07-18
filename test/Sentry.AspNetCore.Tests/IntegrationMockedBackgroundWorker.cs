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

        public IntegrationMockedBackgroundWorker()
        {
            ConfigureBuilder = b => b.UseSentry(options =>
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
        public async Task EnvironmentFromOptions()
        {
            const string expected = "environment";

            Configure = o => o.Environment = expected;

            Build();
            await HttpClient.GetAsync("/throw");

            Worker.Received(1).EnqueueEvent(Arg.Is<SentryEvent>(e => e.Environment == expected));
        }

        [Fact]
        public async Task ReleaseFromOptions()
        {
            const string expected = "release";

            Configure = o => o.Release = expected;

            Build();
            await HttpClient.GetAsync("/throw");

            Worker.Received(1).EnqueueEvent(Arg.Is<SentryEvent>(e => e.Release == expected));
        }
    }
}
