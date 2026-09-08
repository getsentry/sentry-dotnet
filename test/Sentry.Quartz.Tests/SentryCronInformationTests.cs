namespace Sentry.Quartz.Tests;

public class SentryCronInformationTests
{
    [Fact]
    public void Ctor_JobWithoutAttribute_DoesNotWriteStatus_AndUsesTypeNameAsSlug()
    {
        var info = new SentryCronInformation(new PlainJob());

        info.ShouldWriteStatusToSentry.Should().BeFalse();
        info.MonitorSlug.Should().Be(nameof(PlainJob));
    }

    [Fact]
    public void Ctor_JobWithAttributeWithoutSlug_WritesStatus_AndUsesTypeNameAsSlug()
    {
        var info = new SentryCronInformation(new MonitoredJob());

        info.ShouldWriteStatusToSentry.Should().BeTrue();
        info.MonitorSlug.Should().Be(nameof(MonitoredJob));
    }

    [Fact]
    public void Ctor_JobWithAttributeAndSlug_WritesStatus_AndUsesProvidedSlug()
    {
        var info = new SentryCronInformation(new CustomSlugJob());

        info.ShouldWriteStatusToSentry.Should().BeTrue();
        info.MonitorSlug.Should().Be("custom-slug");
    }

    [Fact]
    public void WarningShownForSecondsParameterIssue_DefaultsToFalse()
    {
        var info = new SentryCronInformation(new MonitoredJob());

        info.WarningShownForSecondsParameterIssue.Should().BeFalse();
    }
}
