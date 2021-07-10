﻿using Google.Cloud.Functions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Reflection;

namespace Google.Cloud.Functions.Framework
{
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
            });

            logging.Services.AddSingleton<IConfigureOptions<SentryAspNetCoreOptions>, SentryAspNetCoreOptionsSetup>();
            logging.Services.AddSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>();

            logging.AddFilter<SentryAspNetCoreLoggerProvider>(
                "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                LogLevel.None);

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
}
