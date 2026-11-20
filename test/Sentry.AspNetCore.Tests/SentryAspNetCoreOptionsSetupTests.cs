using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sentry.Internal;

namespace Sentry.AspNetCore.Tests;

public class SentryAspNetCoreOptionsSetupTests
{
    private class Fixture
    {
        public Dictionary<string, string> Configuration { get; set; } = new();

        public SentryAspNetCoreOptionsSetup GetSut()
            => new(new ConfigurationBuilder().AddInMemoryCollection(Configuration).Build());
    }

    private readonly Fixture _fixture = new();
    private readonly SentryAspNetCoreOptions _target = new();

    [Fact]
    public void Configure_RegistersTraceIgnoreStatusCodeTransactionProcessor()
    {
        var sut = _fixture.GetSut();
        sut.Configure(_target);
        Assert.Contains(_target.GetAllTransactionProcessors(), p => p is TraceIgnoreStatusCodeTransactionProcessor);
    }

    [Fact]
    public void Filters_KestrelApplicationEvent_NoException_Filtered()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        sut.Configure(_target);

        //Assert
        Assert.Contains(_target.Logging.Filters, f => f.Filter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Critical, 13, null));
    }

    [Fact]
    public void Filters_KestrelApplicationEvent_WithException_Filtered()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        sut.Configure(_target);

        // Assert
        Assert.Contains(_target.Logging.Filters, f => f.Filter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Critical, 13, new Exception()));
    }

    [Fact]
    public void Filters_KestrelEventId1_WithException_NotFiltered()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        sut.Configure(_target);

        // Assert
        Assert.DoesNotContain(_target.Logging.Filters, f => f.Filter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Trace, 1, null));
    }
}
