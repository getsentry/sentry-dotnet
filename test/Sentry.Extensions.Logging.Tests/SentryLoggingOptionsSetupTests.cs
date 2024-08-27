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
            TagFilters = new List<SubstringOrRegexPattern> { "tag1", "tag2" },
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
            ShutdownTimeout = TimeSpan.FromSeconds(13),
            FlushTimeout = TimeSpan.FromSeconds(17),
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
            FailedRequestTargets = new List<SubstringOrRegexPattern> { "target1", "target2" },
            InitCacheFlushTimeout = TimeSpan.FromSeconds(27),
            // DefaultTags = Dictionary<string,string>,
            TracesSampleRate = 0.8f,
            TracePropagationTargets = new List<SubstringOrRegexPattern> { "target3", "target4" },
            StackTraceMode = StackTraceMode.Enhanced,
            MaxAttachmentSize = 21478,
            DetectStartupTime = StartupTimeDetectionMode.Fast,
            AutoSessionTrackingInterval = TimeSpan.FromHours(3),
            AutoSessionTracking = true,
            UseAsyncFileIO = true,
            JsonPreserveReferences = true,

            MinimumBreadcrumbLevel = LogLevel.Debug,
            MinimumEventLevel = LogLevel.Error,
            InitializeSdk = true
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["IsGlobalModeEnabled"] = expected.IsGlobalModeEnabled.ToString(),
                ["EnableScopeSync"] = expected.EnableScopeSync.ToString(),
                ["TagFilters:0"] = "tag1",
                ["TagFilters:1"] = "tag2",
                ["SendDefaultPii"] = expected.SendDefaultPii.ToString(),
                ["IsEnvironmentUser"] = expected.IsEnvironmentUser.ToString(),
                ["ServerName"] = expected.ServerName,
                ["AttachStacktrace"] = expected.AttachStacktrace.ToString(),
                ["MaxBreadcrumbs"] = expected.MaxBreadcrumbs.ToString(),
                ["SampleRate"] = expected.SampleRate.Value.ToString(CultureInfo.InvariantCulture),
                ["Release"] = expected.Release,
                ["Distribution"] = expected.Distribution,
                ["Environment"] = expected.Environment,
                ["Dsn"] = expected.Dsn,
                ["MaxQueueItems"] = expected.MaxQueueItems.ToString(),
                ["MaxCacheItems"] = expected.MaxCacheItems.ToString(),
                ["ShutdownTimeout"] = expected.ShutdownTimeout.ToString(),
                ["FlushTimeout"] = expected.FlushTimeout.ToString(),
                ["DecompressionMethods"] = expected.DecompressionMethods.ToString(),
                ["RequestBodyCompressionLevel"] = expected.RequestBodyCompressionLevel.ToString(),
                ["RequestBodyCompressionBuffered"] = expected.RequestBodyCompressionBuffered.ToString(),
                ["SendClientReports"] = expected.SendClientReports.ToString(),
                ["Debug"] = expected.Debug.ToString(),
                ["DiagnosticLevel"] = expected.DiagnosticLevel.ToString(),
                ["ReportAssembliesMode"] = expected.ReportAssembliesMode.ToString(),
                ["DeduplicateMode"] = expected.DeduplicateMode.ToString(),
                ["CacheDirectoryPath"] = expected.CacheDirectoryPath.ToString(),
                ["CaptureFailedRequests"] = expected.CaptureFailedRequests.ToString(),
                ["FailedRequestStatusCodes"] = expected.FailedRequestStatusCodes.ToString(),
                ["FailedRequestTargets:0"] = expected.FailedRequestTargets.First().ToString(),
                ["FailedRequestTargets:1"] = expected.FailedRequestTargets.Last().ToString(),
                ["InitCacheFlushTimeout"] = expected.InitCacheFlushTimeout.ToString(),
                ["DefaultTags"] = expected.DefaultTags.ToString(),
                ["TracesSampleRate"] = expected.TracesSampleRate.Value.ToString(CultureInfo.InvariantCulture),
                ["TracePropagationTargets:0"] = expected.TracePropagationTargets.First().ToString(),
                ["TracePropagationTargets:1"] = expected.TracePropagationTargets.Last().ToString(),
                ["StackTraceMode"] = expected.StackTraceMode.ToString(),
                ["MaxAttachmentSize"] = expected.MaxAttachmentSize.ToString(),
                ["DetectStartupTime"] = expected.DetectStartupTime.ToString(),
                ["AutoSessionTrackingInterval"] = expected.AutoSessionTrackingInterval.ToString(),
                ["AutoSessionTracking"] = expected.AutoSessionTracking.ToString(),
                ["UseAsyncFileIO"] = expected.UseAsyncFileIO.ToString(),
                ["JsonPreserveReferences"] = expected.JsonPreserveReferences.ToString(),
                ["MinimumBreadcrumbLevel"] = expected.MinimumBreadcrumbLevel.ToString(),
                ["MinimumEventLevel"] = expected.MinimumEventLevel.ToString(),
                ["InitializeSdk"] = expected.InitializeSdk.ToString(),
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
            actual.TagFilters.Should().BeEquivalentTo(expected.TagFilters);
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
            actual.ShutdownTimeout.Should().Be(expected.ShutdownTimeout);
            actual.FlushTimeout.Should().Be(expected.FlushTimeout);
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
            actual.FailedRequestTargets.Should().BeEquivalentTo(expected.FailedRequestTargets);
            actual.InitCacheFlushTimeout.Should().Be(expected.InitCacheFlushTimeout);
            actual.TracesSampleRate.Should().Be(expected.TracesSampleRate);
            actual.TracePropagationTargets.Should().BeEquivalentTo(expected.TracePropagationTargets);
            actual.StackTraceMode.Should().Be(expected.StackTraceMode);
            actual.MaxAttachmentSize.Should().Be(expected.MaxAttachmentSize);
            actual.DetectStartupTime.Should().Be(expected.DetectStartupTime);
            actual.AutoSessionTrackingInterval.Should().Be(expected.AutoSessionTrackingInterval);
            actual.AutoSessionTracking.Should().Be(expected.AutoSessionTracking);
            actual.UseAsyncFileIO.Should().Be(expected.UseAsyncFileIO);
            actual.JsonPreserveReferences.Should().Be(expected.JsonPreserveReferences);

            actual.MinimumBreadcrumbLevel.Should().Be(expected.MinimumBreadcrumbLevel);
            actual.MinimumEventLevel.Should().Be(expected.MinimumEventLevel);
            actual.InitializeSdk.Should().Be(expected.InitializeSdk);
        }
    }
}
