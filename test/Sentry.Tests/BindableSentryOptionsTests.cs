#if !NETFRAMEWORK
using Microsoft.Extensions.Configuration;

namespace Sentry.Tests;

public class BindableSentryOptionsTests : BindableTests<SentryOptions>
{
    [Fact]
    public void BindableProperties_MatchOptionsProperties()
    {
        var actual = GetPropertyNames<BindableSentryOptions>();
        AssertPropertiesMatchOptions(actual);
    }

    [Fact]
    public void ApplyTo_SetsOptionsFromConfig()
    {
        // Arrange
        var actual = new SentryOptions();
        var bindable = new BindableSentryOptions();

        // Act
        Fixture.Config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        AssertContainsExpectedPropertyValues(actual);
    }

    [Fact]
    public void ApplyTo_TraceIgnoreStatusCodesNotConfigured_KeepsDefault()
    {
        // Arrange
        var actual = new SentryOptions();
        var bindable = new BindableSentryOptions();

        // Act
        new ConfigurationBuilder().Build().Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        actual.TraceIgnoreStatusCodes.Should().Equal(new SentryOptions().TraceIgnoreStatusCodes);
    }

    [Fact]
    public void ApplyTo_TraceIgnoreStatusCodesConfigured_ReplacesDefault()
    {
        // Arrange
        var actual = new SentryOptions();
        var bindable = new BindableSentryOptions();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["TraceIgnoreStatusCodes:0"] = "404",
                ["TraceIgnoreStatusCodes:1"] = "500"
            })
            .Build();

        // Act
        config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        actual.TraceIgnoreStatusCodes.Should().Equal(new HttpStatusCodeRange(404), new HttpStatusCodeRange(500));
    }
}
#endif
