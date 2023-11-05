namespace Sentry.Tests;

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
    public void CaptureFailedRequests_ByDefault_IsFalse()
    {
        var sut = new SentryOptions();
        Assert.False(sut.CaptureFailedRequests, "CaptureFailedRequests should be false by default to protect potentially PII (Privately Identifiable Information)");
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
}
