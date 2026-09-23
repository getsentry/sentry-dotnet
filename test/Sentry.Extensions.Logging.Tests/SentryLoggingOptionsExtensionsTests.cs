using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggingOptionsExtensionsTests
{
    private class TestHostOptions : SentryHostOptions;

    [Fact]
    public void AddLogEntryFilter_LoggingOptions_AddsFilter()
    {
        var sut = new SentryLoggingOptions();
        var filter = Substitute.For<ILogEntryFilter>();

        sut.AddLogEntryFilter(filter);

        sut.Filters.Should().ContainSingle().Which.Should().BeSameAs(filter);
    }

    [Fact]
    public void AddLogEntryFilter_HostOptions_AddsFilterToLoggingOptions()
    {
        var sut = new TestHostOptions();
        var filter = Substitute.For<ILogEntryFilter>();

        sut.AddLogEntryFilter(filter);

        sut.Logging.Filters.Should().ContainSingle().Which.Should().BeSameAs(filter);
    }

    [Fact]
    public void AddLogEntryFilter_HostOptionsDelegate_AddsFilterToLoggingOptions()
    {
        var sut = new TestHostOptions();

        sut.AddLogEntryFilter((_, _, _, _) => true);

        sut.Logging.Filters.Should().ContainSingle().Which.Should().BeOfType<DelegateLogEntryFilter>();
    }

    [Fact]
    public void MinimumLevels_HostOptions_PassThroughToLoggingOptions()
    {
        var sut = new TestHostOptions
        {
            MinimumBreadcrumbLevel = LogLevel.Debug,
            MinimumEventLevel = LogLevel.Critical,
        };

        sut.Logging.MinimumBreadcrumbLevel.Should().Be(LogLevel.Debug);
        sut.Logging.MinimumEventLevel.Should().Be(LogLevel.Critical);
    }
}
