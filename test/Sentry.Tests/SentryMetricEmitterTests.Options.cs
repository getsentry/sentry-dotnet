#nullable enable

namespace Sentry.Tests;

public partial class SentryMetricEmitterTests
{
    [Fact]
    public void EnableMetrics_IsObsoleteAndAlwaysEnabled()
    {
        var options = new SentryOptions();

#pragma warning disable CS0618 // Type or member is obsolete
        options.EnableMetrics.Should().BeTrue();

        options.EnableMetrics = false;

        options.EnableMetrics.Should().BeTrue();
#pragma warning restore CS0618
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
