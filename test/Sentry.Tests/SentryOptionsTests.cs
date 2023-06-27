namespace Sentry.Tests;

public class SentryOptionsTests
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
    public void IsTracingEnabled_Default_False()
    {
        var sut = new SentryOptions();
        Assert.False(sut.IsTracingEnabled);
    }

    [Fact]
    public void IsTracingEnabled_EnableTracing_True()
    {
        var sut = new SentryOptions
        {
            EnableTracing = true
        };

        Assert.True(sut.IsTracingEnabled);
    }

    [Fact]
    public void IsTracingEnabled_EnableTracing_False()
    {
        var sut = new SentryOptions
        {
            EnableTracing = false
        };

        Assert.False(sut.IsTracingEnabled);
    }

    [Fact]
    public void IsTracingEnabled_TracesSampleRate_Zero()
    {
        var sut = new SentryOptions
        {
            TracesSampleRate = 0.0
        };

        Assert.False(sut.IsTracingEnabled);
    }

    [Fact]
    public void IsTracingEnabled_TracesSampleRate_GreaterThanZero()
    {
        var sut = new SentryOptions
        {
            TracesSampleRate = double.Epsilon
        };

        Assert.True(sut.IsTracingEnabled);
    }

    [Fact]
    public void IsTracingEnabled_TracesSampleRate_LessThanZero()
    {
        var sut = new SentryOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sut.TracesSampleRate = -double.Epsilon);
    }

    [Fact]
    public void IsTracingEnabled_TracesSampleRate_GreaterThanOne()
    {
        var sut = new SentryOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sut.TracesSampleRate = 1.0000000000000002);
    }

    [Fact]
    public void IsTracingEnabled_TracesSampler_Provided()
    {
        var sut = new SentryOptions
        {
            TracesSampler = _ => null
        };

        Assert.True(sut.IsTracingEnabled);
    }

    [Fact]
    public void IsTracingEnabled_EnableTracing_True_TracesSampleRate_Zero()
    {
        // Edge Case:
        //   Tracing enabled, but sample rate set to zero, and no sampler function, should be treated as disabled.

        var sut = new SentryOptions
        {
            EnableTracing = true,
            TracesSampleRate = 0.0
        };

        Assert.False(sut.IsTracingEnabled);
    }

    [Fact]
    public void IsTracingEnabled_EnableTracing_False_TracesSampleRate_One()
    {
        // Edge Case:
        //   Tracing disabled should be treated as disabled regardless of sample rate set.

        var sut = new SentryOptions
        {
            EnableTracing = false,
            TracesSampleRate = 1.0
        };

        Assert.False(sut.IsTracingEnabled);
    }

    [Fact]
    public void IsTracingEnabled_EnableTracing_False_TracesSampler_Provided()
    {
        // Edge Case:
        //   Tracing disabled should be treated as disabled regardless of sampler function set.

        var sut = new SentryOptions
        {
            EnableTracing = false,
            TracesSampler = _ => null
        };

        Assert.False(sut.IsTracingEnabled);
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
    public void IdleTimeout_ByDefault_IsNull()
    {
        var sut = new SentryOptions();
        sut.IdleTimeout.IsNull();
    }
}
