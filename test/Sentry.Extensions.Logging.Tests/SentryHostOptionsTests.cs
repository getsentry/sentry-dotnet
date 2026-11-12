namespace Sentry.Extensions.Logging.Tests;

public class SentryHostOptionsTests
{
    private class TestHostOptions : SentryHostOptions;

    private readonly TestHostOptions _sut = new();
    private readonly FakeSettingLocator _settings;

    public SentryHostOptionsTests()
    {
        _settings = _sut.FakeSettings();
    }

    [Theory]
    [InlineData("foo", "Production", "foo")]
    [InlineData("Production", "Production", "Production")]
    [InlineData(null, "Production", "production")]
    [InlineData("", "Production", "production")]
    [InlineData(null, "Development", "development")]
    [InlineData("", "Development", "development")]
    [InlineData(null, "Staging", "staging")]
    [InlineData("", "Staging", "staging")]
    [InlineData(null, "production", "production")]
    [InlineData(null, "development", "development")]
    [InlineData(null, "staging", "staging")]
    [InlineData(null, "Preview", "Preview")]
    public void SetEnvironment_EnvironmentOption_TakesPrecedenceOverHostEnvironment(string environment, string hostEnvironmentName, string expectedEnvironment)
    {
        _sut.Environment = environment;

        _sut.SetEnvironment(hostEnvironmentName);

        Assert.Equal(expectedEnvironment, _sut.Environment);
    }

    [Theory]
    [InlineData("Production", true, "production")]
    [InlineData("Development", true, "development")]
    [InlineData("Staging", true, "staging")]
    [InlineData("Production", false, "Production")]
    [InlineData("Development", false, "Development")]
    [InlineData("Staging", false, "Staging")]
    public void SetEnvironment_AdjustStandardEnvironmentNameCasing_AffectsEnvironment(string hostEnvironmentName, bool adjustStandardEnvironmentNameCasing, string expectedEnvironment)
    {
        _sut.AdjustStandardEnvironmentNameCasing = adjustStandardEnvironmentNameCasing;

        _sut.SetEnvironment(hostEnvironmentName);

        Assert.Equal(expectedEnvironment, _sut.Environment);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("Production")]
    public void SetEnvironment_SentryEnvironmentVariable_TakesPrecedenceOverHostEnvironment(string environment)
    {
        _settings.EnvironmentVariables[Internal.Constants.EnvironmentEnvironmentVariable] = environment;

        _sut.SetEnvironment("Staging");

        Assert.Equal(environment, _sut.Environment);
    }
}
