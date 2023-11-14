using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggingOptionsSetupTests
{
    [Fact]
    public void Configure_BindsConfigurationToOptions()
    {
        // Arrange
        var expected = new SentryLoggingOptions
        {
            IsGlobalModeEnabled = true,
            EnableScopeSync = true,
            // TagFilters = ICollection<SubstringOrRegexPattern>,
            SendDefaultPii = true,
            IsEnvironmentUser = true,
            ServerName = "FakeServerName",
            AttachStacktrace = true,
            MaxBreadcrumbs = 7,
            SampleRate = 0.7f,
            Release = "FakeRelease",
            Distribution = "FakeDistribution",
            Environment = "Test",
            Dsn = "https://d4d82fc1c2c4032a83f3a29aa3a3aff@fake-sentry.io:65535/2147483647",
            MaxQueueItems = 8,
            MaxCacheItems = 9,
            // ShutdownTimeout = TimeSpan,
            // FlushTimeout = TimeSpan,
            DecompressionMethods = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            RequestBodyCompressionLevel = CompressionLevel.Fastest,
            RequestBodyCompressionBuffered = true,
            SendClientReports = true,
            Debug = true,
            DiagnosticLevel = SentryLevel.Warning,
            ReportAssembliesMode = ReportAssembliesMode.InformationalVersion,
            DeduplicateMode = DeduplicateMode.AggregateException,
            CacheDirectoryPath = "~/test",
            CaptureFailedRequests = true,
            // FailedRequestStatusCodes = IList<HttpStatusCodeRange>,
            // FailedRequestTargets = IList<SubstringOrRegexPattern>,
            // InitCacheFlushTimeout = TimeSpan,
            // DefaultTags = Dictionary<string,string>,
            EnableTracing = true,
            TracesSampleRate = 0.8f,
            // TracePropagationTargets = IList<SubstringOrRegexPattern>,
            StackTraceMode = StackTraceMode.Enhanced,
            MaxAttachmentSize = 21478,
            DetectStartupTime = StartupTimeDetectionMode.Fast,
            // AutoSessionTrackingInterval = TimeSpan,
            AutoSessionTracking = true,
            UseAsyncFileIO = true,
            JsonPreserveReferences = true,

            MinimumBreadcrumbLevel = LogLevel.Debug,
            MinimumEventLevel = LogLevel.Error
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("IsGlobalModeEnabled", expected.IsGlobalModeEnabled.ToString()),
                new KeyValuePair<string, string>("EnableScopeSync", expected.EnableScopeSync.ToString()),
                // new KeyValuePair<string, string>("TagFilters", expected.TagFilters.ToString()),
                new KeyValuePair<string, string>("SendDefaultPii", expected.SendDefaultPii.ToString()),
                new KeyValuePair<string, string>("IsEnvironmentUser", expected.IsEnvironmentUser.ToString()),
                new KeyValuePair<string, string>("ServerName", expected.ServerName),
                new KeyValuePair<string, string>("AttachStacktrace", expected.AttachStacktrace.ToString()),
                new KeyValuePair<string, string>("MaxBreadcrumbs", expected.MaxBreadcrumbs.ToString()),
                new KeyValuePair<string, string>("SampleRate", expected.SampleRate.ToString()),
                new KeyValuePair<string, string>("Release", expected.Release),
                new KeyValuePair<string, string>("Distribution", expected.Distribution),
                new KeyValuePair<string, string>("Environment", expected.Environment),
                new KeyValuePair<string, string>("Dsn", expected.Dsn),
                new KeyValuePair<string, string>("MaxQueueItems", expected.MaxQueueItems.ToString()),
                new KeyValuePair<string, string>("MaxCacheItems", expected.MaxCacheItems.ToString()),
                // new KeyValuePair<string, string>("ShutdownTimeout", expected.ShutdownTimeout.ToString()),
                // new KeyValuePair<string, string>("FlushTimeout", expected.FlushTimeout.ToString()),
                new KeyValuePair<string, string>("DecompressionMethods", expected.DecompressionMethods.ToString()),
                new KeyValuePair<string, string>("RequestBodyCompressionLevel", expected.RequestBodyCompressionLevel.ToString()),
                new KeyValuePair<string, string>("RequestBodyCompressionBuffered", expected.RequestBodyCompressionBuffered.ToString()),
                new KeyValuePair<string, string>("SendClientReports", expected.SendClientReports.ToString()),
                new KeyValuePair<string, string>("Debug", expected.Debug.ToString()),
                new KeyValuePair<string, string>("DiagnosticLevel", expected.DiagnosticLevel.ToString()),
                new KeyValuePair<string, string>("ReportAssembliesMode", expected.ReportAssembliesMode.ToString()),
                new KeyValuePair<string, string>("DeduplicateMode", expected.DeduplicateMode.ToString()),
                new KeyValuePair<string, string>("CacheDirectoryPath", expected.CacheDirectoryPath.ToString()),
                new KeyValuePair<string, string>("CaptureFailedRequests", expected.CaptureFailedRequests.ToString()),
                // new KeyValuePair<string, string>("FailedRequestStatusCodes", expected.FailedRequestStatusCodes.ToString()),
                // new KeyValuePair<string, string>("FailedRequestTargets", expected.FailedRequestTargets.ToString()),
                // new KeyValuePair<string, string>("InitCacheFlushTimeout", expected.InitCacheFlushTimeout.ToString()),
                // new KeyValuePair<string, string>("DefaultTags", expected.DefaultTags.ToString()),
                new KeyValuePair<string, string>("EnableTracing", expected.EnableTracing.ToString()),
                new KeyValuePair<string, string>("TracesSampleRate", expected.TracesSampleRate.ToString()),
                // new KeyValuePair<string, string>("TracePropagationTargets", expected.TracePropagationTargets.ToString()),
                new KeyValuePair<string, string>("StackTraceMode", expected.StackTraceMode.ToString()),
                new KeyValuePair<string, string>("MaxAttachmentSize", expected.MaxAttachmentSize.ToString()),
                new KeyValuePair<string, string>("DetectStartupTime", expected.DetectStartupTime.ToString()),
                // new KeyValuePair<string, string>("AutoSessionTrackingInterval", expected.AutoSessionTrackingInterval.ToString()),
                new KeyValuePair<string, string>("AutoSessionTracking", expected.AutoSessionTracking.ToString()),
                new KeyValuePair<string, string>("UseAsyncFileIO", expected.UseAsyncFileIO.ToString()),
                new KeyValuePair<string, string>("JsonPreserveReferences", expected.JsonPreserveReferences.ToString()),

                new KeyValuePair<string, string>("MinimumBreadcrumbLevel", expected.MinimumBreadcrumbLevel.ToString()),
                new KeyValuePair<string, string>("MinimumEventLevel", expected.MinimumEventLevel.ToString()),
            })
            .Build();

        var providerConfig = Substitute.For<ILoggerProviderConfiguration<SentryLoggerProvider>>();
        providerConfig.Configuration.Returns(config);
        var actual = new SentryLoggingOptions();

        var setup = new SentryLoggingOptionsSetup(providerConfig);

        // Act
        setup.Configure(actual);

        // Assert
        using (new AssertionScope())
        {
            actual.IsGlobalModeEnabled.Should().Be(expected.IsGlobalModeEnabled);
            actual.EnableScopeSync.Should().Be(expected.EnableScopeSync);
            // Add assertion for TagFilters here if needed
            actual.SendDefaultPii.Should().Be(expected.SendDefaultPii);
            actual.IsEnvironmentUser.Should().Be(expected.IsEnvironmentUser);
            actual.ServerName.Should().Be(expected.ServerName);
            actual.AttachStacktrace.Should().Be(expected.AttachStacktrace);
            actual.MaxBreadcrumbs.Should().Be(expected.MaxBreadcrumbs);
            actual.SampleRate.Should().Be(expected.SampleRate);
            actual.Release.Should().Be(expected.Release);
            actual.Distribution.Should().Be(expected.Distribution);
            actual.Environment.Should().Be(expected.Environment);
            actual.Dsn.Should().Be(expected.Dsn);
            actual.MaxQueueItems.Should().Be(expected.MaxQueueItems);
            actual.MaxCacheItems.Should().Be(expected.MaxCacheItems);
            // Add assertion for ShutdownTimeout here if needed
            // Add assertion for FlushTimeout here if needed
            actual.DecompressionMethods.Should().Be(expected.DecompressionMethods);
            actual.RequestBodyCompressionLevel.Should().Be(expected.RequestBodyCompressionLevel);
            actual.RequestBodyCompressionBuffered.Should().Be(expected.RequestBodyCompressionBuffered);
            actual.SendClientReports.Should().Be(expected.SendClientReports);
            actual.Debug.Should().Be(expected.Debug);
            actual.DiagnosticLevel.Should().Be(expected.DiagnosticLevel);
            actual.ReportAssembliesMode.Should().Be(expected.ReportAssembliesMode);
            actual.DeduplicateMode.Should().Be(expected.DeduplicateMode);
            actual.CacheDirectoryPath.Should().Be(expected.CacheDirectoryPath);
            actual.CaptureFailedRequests.Should().Be(expected.CaptureFailedRequests);
            // Add assertion for FailedRequestStatusCodes here if needed
            // Add assertion for FailedRequestTargets here if needed
            // Add assertion for InitCacheFlushTimeout here if needed
            // Add assertion for DefaultTags here if needed
            actual.EnableTracing.Should().Be(expected.EnableTracing);
            actual.TracesSampleRate.Should().Be(expected.TracesSampleRate);
            // Add assertion for TracePropagationTargets here if needed
            actual.StackTraceMode.Should().Be(expected.StackTraceMode);
            actual.MaxAttachmentSize.Should().Be(expected.MaxAttachmentSize);
            actual.DetectStartupTime.Should().Be(expected.DetectStartupTime);
            // Add assertion for AutoSessionTrackingInterval here if needed
            actual.AutoSessionTracking.Should().Be(expected.AutoSessionTracking);
            actual.UseAsyncFileIO.Should().Be(expected.UseAsyncFileIO);
            actual.JsonPreserveReferences.Should().Be(expected.JsonPreserveReferences);

            actual.MinimumBreadcrumbLevel.Should().Be(expected.MinimumBreadcrumbLevel);
            actual.MinimumEventLevel.Should().Be(expected.MinimumEventLevel);
        }
    }
}
