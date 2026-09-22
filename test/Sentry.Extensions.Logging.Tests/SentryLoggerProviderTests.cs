using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggerProviderTests
{
    private class Fixture
    {
        public IHub Hub { get; set; } = Substitute.For<IHub>();
        public SentryLoggingOptions SentryLoggingOptions { get; set; } = new();
        public SentryLoggerProvider GetSut() => new(Hub, new MockClock(), SentryLoggingOptions);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Type_CustomAttributes_HasProviderAliasAttribute()
    {
        var type = typeof(SentryLoggerProvider);

        type.GetCustomAttributes<ProviderAliasAttribute>().Should()
            .ContainSingle().Which
            .Alias.Should().Be("Sentry");
    }

    [Fact]
    public void CreateLogger_LoggerType_SentryLogger()
    {
        var sut = _fixture.GetSut();

        _ = Assert.IsType<SentryLogger>(sut.CreateLogger("category"));
    }

    [Fact]
    public void CreateLogger_Category_AsProvided()
    {
        var expectedCategory = nameof(SentryLoggerProviderTests);

        var sut = _fixture.GetSut();

        var actual = (SentryLogger)sut.CreateLogger(expectedCategory);

        Assert.Equal(expectedCategory, actual.CategoryName);
    }
}
