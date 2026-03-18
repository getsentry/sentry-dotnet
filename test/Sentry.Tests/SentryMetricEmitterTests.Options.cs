#nullable enable

namespace Sentry.Tests;

public partial class SentryMetricEmitterTests
{
    [Fact]
    public void EnableMetrics_Default_True()
    {
        var options = new SentryOptions();

        options.EnableMetrics.Should().BeTrue();
    }

    [Fact]
    public void BeforeSendMetric_Default_Null()
    {
        var options = new SentryOptions();

        options.BeforeSendMetricInternal.Should().BeNull();
    }

    [Fact]
    public void BeforeSendMetric_Set_NotNull()
    {
        _fixture.Options.SetBeforeSendMetric(static (SentryMetric metric) => metric);

        _fixture.Options.BeforeSendMetricInternal.Should().NotBeNull();
    }

    [Fact]
    public void BeforeSendMetric_SetNull_Null()
    {
        _fixture.Options.SetBeforeSendMetric(null!);

        _fixture.Options.BeforeSendMetricInternal.Should().BeNull();
    }
}
