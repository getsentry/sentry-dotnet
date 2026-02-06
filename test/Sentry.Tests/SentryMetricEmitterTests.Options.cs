#nullable enable

namespace Sentry.Tests;

public partial class SentryMetricEmitterTests
{
    [Fact]
    public void EnableMetrics_Default_True()
    {
        var options = new SentryOptions();

        options.Experimental.EnableMetrics.Should().BeTrue();
    }

    [Fact]
    public void BeforeSendMetric_Default_Null()
    {
        var options = new SentryOptions();

        options.Experimental.BeforeSendMetricInternal.Should().BeNull();
    }

    [Fact]
    public void BeforeSendMetric_Set_NotNull()
    {
        _fixture.Options.Experimental.SetBeforeSendMetric(static (SentryMetric metric) => metric);

        _fixture.Options.Experimental.BeforeSendMetricInternal.Should().NotBeNull();
    }

    [Fact]
    public void BeforeSendMetric_SetNull_Null()
    {
        _fixture.Options.Experimental.SetBeforeSendMetric(null!);

        _fixture.Options.Experimental.BeforeSendMetricInternal.Should().BeNull();
    }
}
