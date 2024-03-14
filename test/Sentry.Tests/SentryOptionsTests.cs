namespace Sentry.Tests;
#if NETFRAMEWORK
using Sentry.PlatformAbstractions;
#endif
public partial class SentryOptionsTests
{
    [Fact]
    public void DecompressionMethods_ByDefault_AllBitsSet()
    {
        var sut = new SentryOptions();
        Assert.Equal(~DecompressionMethods.None, sut.DecompressionMethods);
    }

    [Fact]
    public void RequestBodyCompressionLevel_ByDefault_Optimal()
    {
        var sut = new SentryOptions();
        Assert.Equal(CompressionLevel.Optimal, sut.RequestBodyCompressionLevel);
    }

    [Fact]
    public void Transport_ByDefault_IsNull()
    {
        var sut = new SentryOptions();
        Assert.Null(sut.Transport);
    }

    [Fact]
    public void AttachStackTrace_ByDefault_True()
    {
        var sut = new SentryOptions();
        Assert.True(sut.AttachStacktrace);
    }

    [Fact]
    public void EnableTracing_Default_Null()
    {
        var sut = new SentryOptions();
        Assert.Null(sut.EnableTracing);
    }

    [Fact]
    public void TracesSampleRate_Default_Null()
    {
        var sut = new SentryOptions();
        Assert.Null(sut.TracesSampleRate);
    }

    [Fact]
    public void TracesSampler_Default_Null()
    {
        var sut = new SentryOptions();
        Assert.Null(sut.TracesSampler);
    }

    [Fact]
    public void IsPerformanceMonitoringEnabled_Default_False()
    {
        var sut = new SentryOptions();
        Assert.False(sut.IsPerformanceMonitoringEnabled);
    }

    [Fact]
    public void IsPerformanceMonitoringEnabled_EnableTracing_True()
    {
        var sut = new SentryOptions
        {
            EnableTracing = true
        };

        Assert.True(sut.IsPerformanceMonitoringEnabled);
    }

    [Fact]
    public void IsPerformanceMonitoringEnabled_EnableTracing_False()
    {
        var sut = new SentryOptions
        {
            EnableTracing = false
        };

        Assert.False(sut.IsPerformanceMonitoringEnabled);
    }

    [Fact]
    public void IsPerformanceMonitoringEnabled_TracesSampleRate_Zero()
    {
        var sut = new SentryOptions
        {
            TracesSampleRate = 0.0
        };

        Assert.False(sut.IsPerformanceMonitoringEnabled);
    }

    [Fact]
    public void IsPerformanceMonitoringEnabled_TracesSampleRate_GreaterThanZero()
    {
        var sut = new SentryOptions
        {
            TracesSampleRate = double.Epsilon
        };

        Assert.True(sut.IsPerformanceMonitoringEnabled);
    }

    [Fact]
    public void IsPerformanceMonitoringEnabled_TracesSampleRate_LessThanZero()
    {
        var sut = new SentryOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sut.TracesSampleRate = -double.Epsilon);
    }

    [Fact]
    public void IsPerformanceMonitoringEnabled_TracesSampleRate_GreaterThanOne()
    {
        var sut = new SentryOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sut.TracesSampleRate = 1.0000000000000002);
    }

    [Fact]
    public void IsPerformanceMonitoringEnabled_TracesSampler_Provided()
    {
        var sut = new SentryOptions
        {
            TracesSampler = _ => null
        };

        Assert.True(sut.IsPerformanceMonitoringEnabled);
    }

    [Fact]
    public void IsPerformanceMonitoringEnabled_EnableTracing_True_TracesSampleRate_Zero()
    {
        // Edge Case:
        //   Tracing enabled, but sample rate set to zero, and no sampler function, should be treated as disabled.

        var sut = new SentryOptions
        {
            EnableTracing = true,
            TracesSampleRate = 0.0
        };

        Assert.False(sut.IsPerformanceMonitoringEnabled);
    }

    [Fact]
    public void IsPerformanceMonitoringEnabled_EnableTracing_False_TracesSampleRate_One()
    {
        // Edge Case:
        //   Tracing disabled should be treated as disabled regardless of sample rate set.

        var sut = new SentryOptions
        {
            EnableTracing = false,
            TracesSampleRate = 1.0
        };

        Assert.False(sut.IsPerformanceMonitoringEnabled);
    }

    [Fact]
    public void IsPerformanceMonitoringEnabled_EnableTracing_False_TracesSampler_Provided()
    {
        // Edge Case:
        //   Tracing disabled should be treated as disabled regardless of sampler function set.

        var sut = new SentryOptions
        {
            EnableTracing = false,
            TracesSampler = _ => null
        };

        Assert.False(sut.IsPerformanceMonitoringEnabled);
    }

    [Fact]
    public void ProfilesSampleRate_Default_Null()
    {
        var sut = new SentryOptions();
        Assert.Null(sut.ProfilesSampleRate);
    }

    [Fact]
    public void IsProfilingEnabled_Default_False()
    {
        var sut = new SentryOptions();
        Assert.False(sut.IsProfilingEnabled);
    }

    [Fact]
    public void IsProfilingEnabled_EnableTracing_True()
    {
        var sut = new SentryOptions
        {
            EnableTracing = true,
            ProfilesSampleRate = double.Epsilon
        };

        Assert.True(sut.IsProfilingEnabled);
    }

    [Fact]
    public void IsProfilingEnabled_EnableTracing_False()
    {
        var sut = new SentryOptions
        {
            EnableTracing = false,
            ProfilesSampleRate = double.Epsilon
        };

        Assert.False(sut.IsProfilingEnabled);
    }

    [Fact]
    public void IsProfilingEnabled_TracesSampleRate_Zero()
    {
        var sut = new SentryOptions
        {
            TracesSampleRate = 0.0,
            ProfilesSampleRate = double.Epsilon
        };

        Assert.False(sut.IsProfilingEnabled);
    }

    [Fact]
    public void IsProfilingEnabled_ProfilessSampleRate_Zero()
    {
        var sut = new SentryOptions
        {
            TracesSampleRate = double.Epsilon,
            ProfilesSampleRate = 0.0
        };

        Assert.False(sut.IsProfilingEnabled);
    }

    [Fact]
    public void IsProfilingEnabled_TracesSampleRate_GreaterThanZero()
    {
        var sut = new SentryOptions
        {
            TracesSampleRate = double.Epsilon,
            ProfilesSampleRate = double.Epsilon
        };

        Assert.True(sut.IsProfilingEnabled);
    }

    [Fact]
    public void IsProfilingEnabled_TracesSampleRate_LessThanZero()
    {
        var sut = new SentryOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sut.ProfilesSampleRate = -double.Epsilon);
    }

    [Fact]
    public void IsProfilingEnabled_TracesSampleRate_GreaterThanOne()
    {
        var sut = new SentryOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sut.ProfilesSampleRate = 1.0000000000000002);
    }

    [Fact]
    public void CaptureFailedRequests_ByDefault_IsTrue()
    {
        var sut = new SentryOptions();
        Assert.True(sut.CaptureFailedRequests);
    }

    [Fact]
    public void FailedRequestStatusCodes_ByDefault_ShouldIncludeServerErrors()
    {
        var sut = new SentryOptions();
        Assert.Contains((500, 599), sut.FailedRequestStatusCodes);
    }

    [Fact]
    public void FailedRequestTargets_ByDefault_MatchesAnyUrl()
    {
        var sut = new SentryOptions();
        Assert.Contains(".*", sut.FailedRequestTargets);
    }

    [Fact]
    public void IsSentryRequest_WithNullUri_ReturnsFalse()
    {
        var sut = new SentryOptions();

        var actual = sut.IsSentryRequest((Uri)null);

        Assert.False(actual);
    }

    [Fact]
    public void IsSentryRequest_WithEmptyUri_ReturnsFalse()
    {
        var sut = new SentryOptions();

        var actual = sut.IsSentryRequest(string.Empty);

        Assert.False(actual);
    }

    [Fact]
    public void IsSentryRequest_WithInvalidUri_ReturnsFalse()
    {
        var sut = new SentryOptions
        {
            Dsn = "https://foo.com"
        };

        var actual = sut.IsSentryRequest(new Uri("https://bar.com"));

        Assert.False(actual);
    }

    [Fact]
    public void IsSentryRequest_WithValidUri_ReturnsTrue()
    {
        var sut = new SentryOptions
        {
            Dsn = "https://123@456.ingest.sentry.io/789"
        };

        var actual = sut.IsSentryRequest(new Uri("https://456.ingest.sentry.io/api/789/envelope/"));

        Assert.True(actual);
    }

    [Fact]
    public void ParseDsn_ReturnsParsedDsn()
    {
        var sut = new SentryOptions
        {
            Dsn = "https://123@456.ingest.sentry.io/789"
        };
        var expected = Dsn.Parse(sut.Dsn);

        var actual = sut.ParsedDsn;

        Assert.Equal(expected.Source, actual.Source);
    }

    [Fact]
    public void ParseDsn_DsnIsSetAgain_Resets()
    {
        var sut = new SentryOptions
        {
            Dsn = "https://123@456.ingest.sentry.io/789"
        };

        _ = sut.ParsedDsn;
        Assert.NotNull(sut._parsedDsn); // Sanity check
        sut.Dsn = "some-other-dsn";

        Assert.Null(sut._parsedDsn);
    }

    [Fact]
    public void DisableDuplicateEventDetection_RemovesDisableDuplicateEventDetection()
    {
        var sut = new SentryOptions();
        sut.DisableDuplicateEventDetection();
        Assert.DoesNotContain(sut.EventProcessors,
            p => p.GetType() == typeof(DuplicateEventDetectionEventProcessor));
    }

#if NETFRAMEWORK
    [Fact]
    public void DisableNetFxInstallationsEventProcessor_RemovesDisableNetFxInstallationsEventProcessorEventProcessor()
    {var sut = new SentryOptions();
        sut.DisableNetFxInstallationsIntegration();
        Assert.DoesNotContain(sut.EventProcessors,
            p => p.GetType() == typeof(NetFxInstallationsEventProcessor));
        Assert.DoesNotContain(sut.Integrations,
            p => p.GetType() == typeof(NetFxInstallationsIntegration));
    }
#endif

    [Fact]
    public void DisableAppDomainUnhandledExceptionCapture_RemovesAppDomainUnhandledExceptionIntegration()
    {
        var sut = new SentryOptions();
        sut.DisableAppDomainUnhandledExceptionCapture();
        Assert.DoesNotContain(sut.Integrations,
            p => p is AppDomainUnhandledExceptionIntegration);
    }

    [Fact]
    public void DisableTaskUnobservedTaskExceptionCapture_UnobservedTaskExceptionIntegration()
    {
        var sut = new SentryOptions();
        sut.DisableUnobservedTaskExceptionCapture();
        Assert.DoesNotContain(sut.Integrations,
            p => p is UnobservedTaskExceptionIntegration);
    }

#if NET8_0_OR_GREATER
    [Fact]
    public void DisableSystemDiagnosticsMetricsIntegration_RemovesSystemDiagnosticsMetricsIntegration()
    {
        var sut = new SentryOptions();
        sut.DisableSystemDiagnosticsMetricsIntegration();
        Assert.DoesNotContain(sut.Integrations,
            p => p.GetType() == typeof(SystemDiagnosticsMetricsIntegration));
    }
#endif

    [Fact]
    public void AddIntegration_StoredInOptions()
    {
        var sut = new SentryOptions();
        var expected = Substitute.For<ISdkIntegration>();
        sut.AddIntegration(expected);
        Assert.Contains(sut.Integrations, actual => actual == expected);
    }

    [Fact]
    public void AddInAppExclude_StoredInOptions()
    {
        var sut = new SentryOptions();
        const string expected = "test";
        sut.AddInAppExclude(expected);
        Assert.Contains(sut.InAppExclude!, actual => actual == expected);
    }

    [Fact]
    public void AddInAppInclude_StoredInOptions()
    {
        var sut = new SentryOptions();
        const string expected = "test";
        sut.AddInAppInclude(expected);
        Assert.Contains(sut.InAppInclude!, actual => actual == expected);
    }

    [Fact]
    public void AddExceptionProcessor_StoredInOptions()
    {
        var sut = new SentryOptions();
        var expected = Substitute.For<ISentryEventExceptionProcessor>();
        sut.AddExceptionProcessor(expected);
        Assert.Contains(sut.ExceptionProcessors, actual => actual.Lazy.Value == expected);
    }

    [Fact]
    public void AddExceptionProcessors_StoredInOptions()
    {
        var sut = new SentryOptions();
        var first = Substitute.For<ISentryEventExceptionProcessor>();
        var second = Substitute.For<ISentryEventExceptionProcessor>();
        sut.AddExceptionProcessors(new[] { first, second });
        Assert.Contains(sut.ExceptionProcessors, actual => actual.Lazy.Value == first);
        Assert.Contains(sut.ExceptionProcessors, actual => actual.Lazy.Value == second);
    }

    [Fact]
    public void AddExceptionProcessorProvider_StoredInOptions()
    {
        var sut = new SentryOptions();
        var first = Substitute.For<ISentryEventExceptionProcessor>();
        var second = Substitute.For<ISentryEventExceptionProcessor>();
        sut.AddExceptionProcessorProvider(() => new[] { first, second });
        Assert.Contains(sut.GetAllExceptionProcessors(), actual => actual == first);
        Assert.Contains(sut.GetAllExceptionProcessors(), actual => actual == second);
    }

    [Fact]
    public void AddExceptionProcessor_DoesNotExcludeMainProcessor()
    {
        var sut = new SentryOptions();
        sut.AddExceptionProcessor(Substitute.For<ISentryEventExceptionProcessor>());
        Assert.Contains(sut.ExceptionProcessors, actual => actual.Lazy.Value.GetType() == typeof(MainExceptionProcessor));
    }

    [Fact]
    public void AddExceptionProcessors_DoesNotExcludeMainProcessor()
    {
        var sut = new SentryOptions();
        sut.AddExceptionProcessors(new[] { Substitute.For<ISentryEventExceptionProcessor>() });
        Assert.Contains(sut.ExceptionProcessors, actual => actual.Lazy.Value.GetType() == typeof(MainExceptionProcessor));
    }

    [Fact]
    public void AddExceptionProcessorProvider_DoesNotExcludeMainProcessor()
    {
        var sut = new SentryOptions();
        sut.AddExceptionProcessorProvider(() => new[] { Substitute.For<ISentryEventExceptionProcessor>() });
        Assert.Contains(sut.ExceptionProcessors, actual => actual.Lazy.Value.GetType() == typeof(MainExceptionProcessor));
    }

    [Fact]
    public void GetAllExceptionProcessors_ReturnsMainSentryEventProcessor()
    {
        var sut = new SentryOptions();
        Assert.Contains(sut.GetAllExceptionProcessors(), actual => actual.GetType() == typeof(MainExceptionProcessor));
    }

    [Fact]
    public void GetAllExceptionProcessors_FirstReturned_MainExceptionProcessor()
    {
        var sut = new SentryOptions();
        sut.AddExceptionProcessorProvider(() => new[] { Substitute.For<ISentryEventExceptionProcessor>() });
        sut.AddExceptionProcessors(new[] { Substitute.For<ISentryEventExceptionProcessor>() });
        sut.AddExceptionProcessor(Substitute.For<ISentryEventExceptionProcessor>());

        _ = Assert.IsType<MainExceptionProcessor>(sut.GetAllExceptionProcessors().First());
    }

    [Fact]
    public void AddEventProcessor_StoredInOptions()
    {
        var sut = new SentryOptions();
        var expected = Substitute.For<ISentryEventProcessor>();
        sut.AddEventProcessor(expected);
        Assert.Contains(sut.EventProcessors, actual => actual.Lazy.Value == expected);
    }

    [Fact]
    public void AddEventProcessors_StoredInOptions()
    {
        var sut = new SentryOptions();
        var first = Substitute.For<ISentryEventProcessor>();
        var second = Substitute.For<ISentryEventProcessor>();
        sut.AddEventProcessors(new[] { first, second });
        Assert.Contains(sut.EventProcessors, actual => actual.Lazy.Value == first);
        Assert.Contains(sut.EventProcessors, actual => actual.Lazy.Value == second);
    }

    [Fact]
    public void AddEventProcessorProvider_StoredInOptions()
    {
        var sut = new SentryOptions();
        var first = Substitute.For<ISentryEventProcessor>();
        var second = Substitute.For<ISentryEventProcessor>();
        sut.AddEventProcessorProvider(() => new[] { first, second });
        Assert.Contains(sut.GetAllEventProcessors(), actual => actual == first);
        Assert.Contains(sut.GetAllEventProcessors(), actual => actual == second);
    }

    [Fact]
    public void AddTransactionProcessor_StoredInOptions()
    {
        var sut = new SentryOptions();
        var expected = Substitute.For<ISentryTransactionProcessor>();
        sut.AddTransactionProcessor(expected);
        Assert.Contains(sut.TransactionProcessors!, actual => actual == expected);
    }

    [Fact]
    public void AddTransactionProcessors_StoredInOptions()
    {
        var sut = new SentryOptions();
        var first = Substitute.For<ISentryTransactionProcessor>();
        var second = Substitute.For<ISentryTransactionProcessor>();
        sut.AddTransactionProcessors(new[] { first, second });
        Assert.Contains(sut.TransactionProcessors!, actual => actual == first);
        Assert.Contains(sut.TransactionProcessors!, actual => actual == second);
    }

    [Fact]
    public void AddTransactionProcessorProvider_StoredInOptions()
    {
        var sut = new SentryOptions();
        var first = Substitute.For<ISentryTransactionProcessor>();
        var second = Substitute.For<ISentryTransactionProcessor>();
        sut.AddTransactionProcessorProvider(() => new[] { first, second });
        Assert.Contains(sut.GetAllTransactionProcessors(), actual => actual == first);
        Assert.Contains(sut.GetAllTransactionProcessors(), actual => actual == second);
    }

    [Fact]
    public void AddEventProcessor_DoesNotExcludeMainProcessor()
    {
        var sut = new SentryOptions();
        sut.AddEventProcessor(Substitute.For<ISentryEventProcessor>());
        Assert.Contains(sut.EventProcessors, actual => actual.Lazy.Value.GetType() == typeof(MainSentryEventProcessor));
    }

    [Fact]
    public void AddEventProcessors_DoesNotExcludeMainProcessor()
    {
        var sut = new SentryOptions();
        sut.AddEventProcessors(new[] { Substitute.For<ISentryEventProcessor>() });
        Assert.Contains(sut.EventProcessors, actual => actual.Lazy.Value.GetType() == typeof(MainSentryEventProcessor));
    }

    [Fact]
    public void AddEventProcessorProvider_DoesNotExcludeMainProcessor()
    {
        var sut = new SentryOptions();
        sut.AddEventProcessorProvider(() => new[] { Substitute.For<ISentryEventProcessor>() });
        Assert.Contains(sut.EventProcessors, actual => actual.Lazy.Value.GetType() == typeof(MainSentryEventProcessor));
    }

    [Fact]
    public void GetAllEventProcessors_ReturnsMainSentryEventProcessor()
    {
        var sut = new SentryOptions();
        Assert.Contains(sut.GetAllEventProcessors(), actual => actual.GetType() == typeof(MainSentryEventProcessor));
    }

    [Fact]
    public void GetAllEventProcessors_AddingMore_SecondReturned_MainSentryEventProcessor()
    {
        var sut = new SentryOptions();
        sut.AddEventProcessorProvider(() => new[] { Substitute.For<ISentryEventProcessor>() });
        sut.AddEventProcessors(new[] { Substitute.For<ISentryEventProcessor>() });
        sut.AddEventProcessor(Substitute.For<ISentryEventProcessor>());

        _ = Assert.IsType<MainSentryEventProcessor>(sut.GetAllEventProcessors().Skip(1).First());
    }

    [Fact]
    public void GetAllEventProcessors_NoAdding_SecondReturned_MainSentryEventProcessor()
    {
        var sut = new SentryOptions();
        _ = Assert.IsType<MainSentryEventProcessor>(sut.GetAllEventProcessors().Skip(1).First());
    }

    [Fact]
    public void UseStackTraceFactory_ReplacesStackTraceFactory()
    {
        var sut = new SentryOptions();
        var expected = Substitute.For<ISentryStackTraceFactory>();
        _ = sut.UseStackTraceFactory(expected);

        Assert.Same(expected, sut.SentryStackTraceFactory);
    }

    [Fact]
    public void UseStackTraceFactory_ReplacesStackTraceFactory_InCurrentProcessors()
    {
        var sut = new SentryOptions();
        var eventProcessor = sut.GetAllEventProcessors().OfType<MainSentryEventProcessor>().Single();
        var exceptionProcessor = sut.GetAllExceptionProcessors().OfType<MainExceptionProcessor>().Single();

        var expected = Substitute.For<ISentryStackTraceFactory>();
        _ = sut.UseStackTraceFactory(expected);

        Assert.Same(expected, eventProcessor.SentryStackTraceFactoryAccessor());
        Assert.Same(expected, exceptionProcessor.SentryStackTraceFactoryAccessor());
    }

    [Fact]
    public void UseStackTraceFactory_NotNull()
    {
        var sut = new SentryOptions();
        _ = Assert.Throws<ArgumentNullException>(() => sut.UseStackTraceFactory(null!));
    }

    [Fact]
    public void GetAllEventProcessors_AddingMore_FirstReturned_DuplicateDetectionProcessor()
    {
        var sut = new SentryOptions();
        sut.AddEventProcessorProvider(() => new[] { Substitute.For<ISentryEventProcessor>() });
        sut.AddEventProcessors(new[] { Substitute.For<ISentryEventProcessor>() });
        sut.AddEventProcessor(Substitute.For<ISentryEventProcessor>());

        _ = Assert.IsType<DuplicateEventDetectionEventProcessor>(sut.GetAllEventProcessors().First());
    }

    [Fact]
    public void GetAllEventProcessors_NoAdding_FirstReturned_DuplicateDetectionProcessor()
    {
        var sut = new SentryOptions();
        _ = Assert.IsType<DuplicateEventDetectionEventProcessor>(sut.GetAllEventProcessors().First());
    }

    [Fact]
    public void Integrations_Includes_AppDomainUnhandledExceptionIntegration()
    {
        var sut = new SentryOptions();
        Assert.Contains(sut.Integrations, i => i.GetType() == typeof(AppDomainUnhandledExceptionIntegration));
    }

    [Fact]
    public void Integrations_Includes_AppDomainProcessExitIntegration()
    {
        var sut = new SentryOptions();
        Assert.Contains(sut.Integrations, i => i.GetType() == typeof(AppDomainProcessExitIntegration));
    }

    [Fact]
    public void Integrations_Includes_TaskUnobservedTaskExceptionIntegration()
    {
        var sut = new SentryOptions();
        Assert.Contains(sut.Integrations, i => i.GetType() == typeof(UnobservedTaskExceptionIntegration));
    }

    [Theory]
    [InlineData("Microsoft")]
    [InlineData("System")]
    [InlineData("FSharp")]
    [InlineData("Giraffe")]
    [InlineData("Newtonsoft.Json")]
    public void Integrations_Includes_MajorSystemPrefixes(string expected)
    {
        var sut = new SentryOptions();
        Assert.Contains(sut.InAppExclude!, e => e == expected);
    }
}
