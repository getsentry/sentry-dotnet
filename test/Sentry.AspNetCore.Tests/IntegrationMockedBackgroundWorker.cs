using System;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Testing;
using Xunit;
using Microsoft.AspNetCore.Builder;

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
                options.Worker(w => w.BackgroundWorker = Worker);

                Configure?.Invoke(options);
            });
        }

        [Fact]
        public async Task SendDefaultPii_FalseWithoutUserInRequest_NoUserNameSent()
        {
            Configure = o => o.SendDefaultPii = false;

            Build();
            await HttpClient.GetAsync("/throw");

            Worker.Received(1).EnqueueEvent(Arg.Is<SentryEvent>(e => e.User.Username == null));
        }

        [Fact]
        public async Task SendDefaultPii_TrueWithoutUserInRequest_NoUserNameSent()
        {
            Configure = o => o.SendDefaultPii = true; // Sentry package will set to Environment.UserName

            Build();
            await HttpClient.GetAsync("/throw");

            Worker.Received(1).EnqueueEvent(Arg.Is<SentryEvent>(e => e.User.Username == null));
        }

        [Fact]
        public async Task SendDefaultPii_TrueWithUserInRequest_UserNameSent()
        {
            const string expectedName = "sentry user";
            Configure = o => o.SendDefaultPii = true; // Sentry package will set to Environment.UserName
            ConfigureApp = app =>
            {
                app.Use(async (context, next) =>
                {
                    context.User = new GenericPrincipal(new GenericIdentity(expectedName), Array.Empty<string>());
                    await next();
                });
            };
            Build();
            await HttpClient.GetAsync("/throw");

            Worker.Received(1).EnqueueEvent(Arg.Is<SentryEvent>(e => e.User.Username == expectedName));
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
