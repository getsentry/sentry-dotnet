#nullable enable
using System;
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Google.Cloud.Functions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Reflection;

[assembly: FunctionsStartup(typeof(SimpleHttpFunction.SentryStartup))]

namespace SimpleHttpFunction
{
    public class Function : IHttpFunction
    {
        private readonly ILogger<Function> _logger;
        public Function(ILogger<Function> logger) => _logger = logger;

        public Task HandleAsync(HttpContext context)
        {
            _logger.LogInformation("Useful info that is added to the breadcrumb list.");
            _logger.LogError("Is Sentry enabled? " + SentrySdk.IsEnabled);
            SentrySdk.CaptureMessage("hello from GCP Functions");
            throw new Exception("Bad function");
        }
    }

 /// <summary>
    /// Starts up the GCP Function integration.
    /// </summary>
    public class SentryStartup : FunctionsStartup
    {
        /// <summary>
        /// Configure Sentry logging.
        /// </summary>
        public override void ConfigureLogging(WebHostBuilderContext context, ILoggingBuilder logging)
        {
            base.ConfigureLogging(context, logging);
            logging.AddConfiguration(context.Configuration);

            logging.Services.AddSingleton<ISentryEventProcessor, SentryGoogleCloudFunctionEventProcessor>();

            // TODO: refactor this with SentryWebHostBuilderExtensions
            var section = context.Configuration.GetSection("Sentry");
            logging.Services.Configure<SentryAspNetCoreOptions>(section);

            logging.Services.Configure<SentryAspNetCoreOptions>(options =>
            {
                // Make sure all events are flushed out
                options.FlushOnCompletedRequest = true;
                options.DiagnosticLogger = new TestConsoleDiagnosticLogger(options.DiagnosticLevel);
            });

            logging.Services.AddSingleton<IConfigureOptions<SentryAspNetCoreOptions>, SentryAspNetCoreOptionsSetup>();
            logging.Services.AddSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>();

            logging.AddFilter<SentryAspNetCoreLoggerProvider>(
                "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                LogLevel.None);
            logging.AddFilter<SentryAspNetCoreLoggerProvider>(
                "Sentry*",
                LogLevel.Debug);
            logging.Services.AddSentry();
        }

        /// <summary>
        /// Configure Sentry services.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="services"></param>
        public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            base.ConfigureServices(context, services);
            services.AddTransient<IStartupFilter, SentryStartupFilter>();
        }

        private class SentryGoogleCloudFunctionEventProcessor : ISentryEventProcessor
        {
            private static readonly SdkVersion NameAndVersion
                = typeof(SentryStartup).Assembly.GetNameAndVersion();

            private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;
            private const string SdkName = "sentry.dotnet.google-cloud-function";

            public SentryEvent Process(SentryEvent @event)
            {
                // Take over the SDK name since this wraps ASP.NET Core
                @event.Sdk.Name = SdkName;
                @event.Sdk.Version = NameAndVersion.Version;

                if (NameAndVersion.Version != null)
                {
                    @event.Sdk.AddPackage(ProtocolPackageName, NameAndVersion.Version);
                }

                return @event;
            }
        }
    }
    public class TestConsoleDiagnosticLogger : IDiagnosticLogger
    {
        private readonly SentryLevel _minimalLevel;

        /// <summary>
        /// Creates a new instance of <see cref="ConsoleDiagnosticLogger"/>.
        /// </summary>
        public TestConsoleDiagnosticLogger(SentryLevel minimalLevel) {
            _minimalLevel = minimalLevel;
        }

        /// <summary>
        /// Whether the logger is enabled to the defined level.
        /// </summary>
        public bool IsEnabled(SentryLevel level) {
            return level >= _minimalLevel;
        }

        /// <summary>
        /// Log message with level, exception and parameters.
        /// </summary>
        public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
        {
Console.Write($@"{logLevel,7}: {string.Format(message, args)}
{exception}");
        }

    }
}
