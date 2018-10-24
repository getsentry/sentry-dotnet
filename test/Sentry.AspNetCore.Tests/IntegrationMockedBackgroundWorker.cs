using System;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Testing;
using Xunit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.AspNetCore;
using Sentry.AspNetCore.Tests;
using Sentry.Extensions.Logging;

// ReSharper disable once CheckNamespace - To test Logger emitting events:
// It filters events coming from 'Sentry.' namespace.
namespace Else.AspNetCore.Tests
{
    [Collection(nameof(SentrySdkCollection))]
    public class IntegrationMockedBackgroundWorker : SentrySdkTestFixture
    {
        protected IBackgroundWorker Worker { get; set; } = Substitute.For<IBackgroundWorker>();
        protected Action<SentryAspNetCoreOptions> Configure;

        public IntegrationMockedBackgroundWorker()
        {
            ConfigureWehHost = builder =>
            {
                builder.UseSentry(options =>
                {
                    options.Dsn = DsnSamples.ValidDsnWithSecret;
                    options.BackgroundWorker = Worker;

                    Configure?.Invoke(options);
                });
            };
        }

        [Fact]
        public async Task DisabledSdk_UnhandledException_NoEventCaptured()
        {
            Configure = o => o.InitializeSdk = false;

            Build();
            await HttpClient.GetAsync("/throw");

            Worker.DidNotReceive().EnqueueEvent(Arg.Any<SentryEvent>());
            Assert.False(ServiceProvider.GetRequiredService<IHub>().IsEnabled);
        }

        [Fact]
        public void DisabledSdk_WithLogger_NoEventCaptured()
        {
            Configure = o => o.InitializeSdk = false;

            Build();
            var logger = ServiceProvider.GetRequiredService<ILogger<IntegrationMockedBackgroundWorker>>();
            logger.LogCritical("test");

            Worker.DidNotReceive().EnqueueEvent(Arg.Any<SentryEvent>());
            Assert.False(ServiceProvider.GetRequiredService<IHub>().IsEnabled);
        }

        [Fact]
        public void LogError_ByDefault_EventCaptured()
        {
            const string expectedMessage = "test";

            Build();
            var logger = ServiceProvider.GetRequiredService<ILogger<IntegrationMockedBackgroundWorker>>();
            logger.LogError(expectedMessage);

            Worker.Received(1).EnqueueEvent(Arg.Is<SentryEvent>(p => p.LogEntry.Formatted == expectedMessage));
        }

        [Fact]
        public void LogError_WithFormat_EventCaptured()
        {
            const string expectedMessage = "Test {structured} log";
            const int param = 10;

            Build();
            var logger = ServiceProvider.GetRequiredService<ILogger<IntegrationMockedBackgroundWorker>>();
            logger.LogError(expectedMessage, param);

            Worker.Received(1).EnqueueEvent(Arg.Is<SentryEvent>(p =>
                p.LogEntry.Formatted == $"Test {param} log"
                && p.LogEntry.Message == expectedMessage));
        }

        [Fact]
        public void DiagnosticLogger_DebugEnabled_ReplacedWithMelLogger()
        {
            Configure = o => o.Debug = true;
            Build();
            var options = ServiceProvider.GetRequiredService<IOptions<SentryAspNetCoreOptions>>();
            Assert.IsType<MelDiagnosticLogger>(options.Value.DiagnosticLogger);
        }

        [Fact]
        public void DiagnosticLogger_ByDefault_IsNull()
        {
            Build();
            var options = ServiceProvider.GetRequiredService<IOptions<SentryAspNetCoreOptions>>();
            Assert.Null(options.Value.DiagnosticLogger);
        }

        [Fact]
        public void SentryClient_CaptureMessage_EventCaptured()
        {
            const string expectedMessage = "test";

            Build();
            var client = ServiceProvider.GetRequiredService<ISentryClient>();
            client.CaptureMessage(expectedMessage);

            Worker.Received(1).EnqueueEvent(Arg.Is<SentryEvent>(p => p.Message == expectedMessage));
        }

        [Fact]
        public void Hub_CaptureMessage_EventCaptured()
        {
            const string expectedMessage = "test";

            Build();
            var client = ServiceProvider.GetRequiredService<IHub>();
            client.CaptureMessage(expectedMessage);

            Worker.Received(1).EnqueueEvent(Arg.Is<SentryEvent>(p => p.Message == expectedMessage));
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
        public void AllSettingsViaJson()
        {
            ConfigureWehHost = b =>
            {
                b.ConfigureAppConfiguration(c =>
                {
                    c.SetBasePath(Directory.GetCurrentDirectory()); // fails on net462 without this
                    c.AddJsonFile("allsettings.json", optional: false);
                });
                b.UseSentry(o => o.BackgroundWorker = Worker);
            };

            Build();

            var options = ServiceProvider.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value;

            Assert.Equal("https://1@sentry.yo/1", ((SentryOptions)options).Dsn.ToString());
            Assert.True(options.IncludeRequestPayload);
            Assert.True(options.SendDefaultPii);
            Assert.True(options.IncludeActivityData);
            Assert.Equal(LogLevel.Error, options.MinimumBreadcrumbLevel);
            Assert.Equal(LogLevel.Critical, options.MinimumEventLevel);
            Assert.False(options.InitializeSdk);
            Assert.Equal(999, options.MaxBreadcrumbs);
            Assert.Equal(1, options.SampleRate.Value);
            Assert.Equal("7f5d9a1", options.Release);
            Assert.Equal("Staging", options.Environment);
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
