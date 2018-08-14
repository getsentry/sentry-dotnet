using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.Protocol;

namespace Sentry.Extensions.Logging
{
    public class SentryLoggingConfigurationOptions
    {
        /// <summary>
        /// Gets or sets the minimum breadcrumb level.
        /// </summary>
        /// <remarks>Events with this level or higher will be stored as <see cref="Breadcrumb"/></remarks>
        /// <value>
        /// The minimum breadcrumb level.
        /// </value>
        public LogLevel MinimumBreadcrumbLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets the minimum event level.
        /// </summary>
        /// <remarks>
        /// Events with this level or higher will be sent to Sentry
        /// </remarks>
        /// <value>
        /// The minimum event level.
        /// </value>
        public LogLevel MinimumEventLevel { get; set; } = LogLevel.Error;

        /// <summary>
        /// Whether to initialize the SDK via this logging integration
        /// </summary>
        /// <remarks>
        /// The SDK only needs to be initialized once. If you are explicitly calling
        /// <see cref="SentrySdk.Init(string)"/>, there's no need to initialize the SDK again through
        /// integrations.
        /// </remarks>
        public bool InitializeSdk { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum breadcrumbs.
        /// </summary>
        /// <remarks>
        /// When the number of events reach this configuration value,
        /// older breadcrumbs start dropping to make room for new ones.
        /// </remarks>
        /// <value>
        /// The maximum breadcrumbs per scope.
        /// </value>
        public int MaxBreadcrumbs { get; set; } = Sentry.Protocol.Constants.DefaultMaxBreadcrumbs;

        /// <summary>
        /// The rate to sample events
        /// </summary>
        /// <remarks>
        /// Can be anything between 0.01 (1%) and 1.0 (99.9%) or null (default), to disable it.
        /// </remarks>
        /// <example>
        /// 0.1 = 10% of events are sent
        /// </example>
        /// <see href="https://docs.sentry.io/clientdev/features/#event-sampling"/>
        private float? _sampleRate;
        public float? SampleRate
        {
            get => _sampleRate;
            set
            {
                if (value > 1 || value <= 0)
                {
                    throw new InvalidOperationException($"The value {value} is not valid. Use null to disable or values between 0.01 (inclusive) and 1.0 (exclusive) ");
                }
                _sampleRate = value;
            }
        }

        /// <summary>
        /// The release version of the application.
        /// </summary>
        /// <example>
        /// 721e41770371db95eee98ca2707686226b993eda
        /// 14.1.16.32451
        /// </example>
        /// <remarks>
        /// This value will generally be something along the lines of the git SHA for the given project.
        /// If not explicitly defined via configuration or environment variable (SENTRY_RELEASE).
        /// It will attempt o read it from:
        /// <see cref="System.Reflection.AssemblyInformationalVersionAttribute"/>
        ///
        /// Don't rely on discovery if your release is: '1.0.0' or '0.0.0'. Since those are
        /// default values for new projects, they are not considered valid by the discovery process.
        /// </remarks>
        /// <seealso href="https://docs.sentry.io/learn/releases/"/>
        public string Release { get; set; }

        /// <summary>
        /// The environment the application is running
        /// </summary>
        /// <remarks>
        /// This value can also be set via environment variable: SENTRY_ENVIRONMENT
        /// In some cases you don't need to set this manually since integrations, when possible, automatically fill this value.
        /// For ASP.NET Core which can read from IHostingEnvironment
        /// </remarks>
        /// <example>
        /// Production, Staging
        /// </example>
        /// <seealso href="https://docs.sentry.io/learn/environments/"/>
        public string Environment { get; set; }

        /// <summary>
        /// The Data Source Name of a given project in Sentry.
        /// </summary>
        public string Dsn { get; set; }
    }
}
