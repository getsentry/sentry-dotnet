using Sentry.Testing;

namespace Sentry.Tests.Internals;

public class DistributionLocatorTests
{
    [Fact]
    public void ResolveFromEnvironment_Default_Null()
    {
        var options = new SentryOptions();

        string result = null;
        EnvironmentVariableGuard.WithVariable(
            Internal.Constants.DistributionEnvironmentVariable,
            null,
            () => result = DistributionLocator.Resolve(options));

        Assert.Null(result);
    }

    [Fact]
    public void ResolveFromEnvironment_From_Options()
    {
        const string expectedDistribution = "qwerty1234";
        var options = new SentryOptions
        {
            Distribution = expectedDistribution
        };

        string result = null;
        EnvironmentVariableGuard.WithVariable(
            Internal.Constants.DistributionEnvironmentVariable,
            null,
            () => result = DistributionLocator.Resolve(options));

        Assert.Equal(expectedDistribution, result);
    }

    [Fact]
    public void ResolveFromEnvironment_WithEnvironmentVariable_VersionOfEnvironmentVariable()
    {
        const string expectedDistribution = "qwerty1234";
        string result = null;

        EnvironmentVariableGuard.WithVariable(
            Internal.Constants.DistributionEnvironmentVariable,
            expectedDistribution,
            () => result = DistributionLocator.LocateFromEnvironment());

        Assert.Equal(expectedDistribution, result);
    }
}
