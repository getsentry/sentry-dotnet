using Microsoft.Extensions.Options;
using Quartz;

namespace Sentry.Quartz.Tests;

public class SentryMetricsMiddlewareTests
{
    private readonly IHub _hub = Substitute.For<IHub>();
    private readonly InMemorySentryMetricEmitter _metrics = new();
    private readonly SentryMetricsOptions _options = new();

    public SentryMetricsMiddlewareTests() => _hub.Metrics.Returns(_metrics);

    private SentryMetricsMiddleware CreateSut() => new(_hub, Options.Create(_options));

    private static IJobExecutionContext CreateContext(JobDataMap? jobDataMap = null)
    {
        var jobDetail = Substitute.For<IJobDetail>();
        jobDetail.Key.Returns(new JobKey("MyJob", "MyGroup"));
        jobDetail.JobDataMap.Returns(jobDataMap ?? new JobDataMap());

        var context = Substitute.For<IJobExecutionContext>();
        context.JobDetail.Returns(jobDetail);
        return context;
    }

    [Fact]
    public async Task Invoke_EmitsDistributionMetric_WithDefaultNameBasedOnJobKey()
    {
        var sut = CreateSut();
        var context = CreateContext();

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        _metrics.Entries.Should().ContainSingle();
        var entry = _metrics.Entries[0];
        entry.Type.Should().Be(SentryMetricType.Distribution);
        entry.Name.Should().Be("quartz.job.duration.MyGroup.MyJob");
        entry.Unit.Should().Be("millisecond");
    }

    [Fact]
    public async Task Invoke_LogMetricsExplicitlyFalse_DoesNotEmitMetric()
    {
        var jobDataMap = new JobDataMap { { "LogMetrics", false } };
        var sut = CreateSut();
        var context = CreateContext(jobDataMap);

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        _metrics.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Invoke_LogMetricsExplicitlyTrue_EmitsMetric()
    {
        var jobDataMap = new JobDataMap { { "LogMetrics", true } };
        var sut = CreateSut();
        var context = CreateContext(jobDataMap);

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        _metrics.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task Invoke_CustomResolveMetricsName_UsesResolvedName()
    {
        _options.ResolveMetricsName = jobDetail => $"custom.{jobDetail.Key.Name}";
        var sut = CreateSut();
        var context = CreateContext();

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        _metrics.Entries.Should().ContainSingle(e => e.Name == "custom.MyJob");
    }

    [Fact]
    public async Task Invoke_AdditionalAttributes_AreAttachedToTheMetric()
    {
        _options.AdditionalAttributes = _ => new Dictionary<string, object> { ["foo"] = "bar" };
        var sut = CreateSut();
        var context = CreateContext();

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        _metrics.Entries.Should().ContainSingle();
        _metrics.Entries[0].Attributes.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
    }

    [Fact]
    public async Task Invoke_JobThrows_StillEmitsMetric_AndRethrows()
    {
        var sut = CreateSut();
        var context = CreateContext();
        var exception = new InvalidOperationException("boom");

        var act = () => sut.Invoke(context, MiddlewareTestHelpers.Next(exception), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>();
        _metrics.Entries.Should().ContainSingle();
    }
}
