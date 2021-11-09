using Sentry.Testing;

namespace Sentry.Tests.Internals;

public class EnvironmentLocatorTests
{
    [Fact]
    public void Locate_WithEnvironmentVariable_ReturnsEnvironmentVariableValue()
    {
        const string expectedVersion = "the environment name";
        EnvironmentVariableGuard.WithVariable(
            Sentry.Internal.Constants.EnvironmentEnvironmentVariable,
            expectedVersion,
            () =>
            {
                Assert.Equal(expectedVersion, EnvironmentLocator.LocateFromEnvironmentVariable());
            });
    }

    [Fact]
    public void Locate_WithoutEnvironmentVariable_ReturnsNull()
    {
        EnvironmentVariableGuard.WithVariable(
            Sentry.Internal.Constants.ReleaseEnvironmentVariable,
            null,
            () =>
            {
                Assert.Null(EnvironmentLocator.LocateFromEnvironmentVariable());
            });
    }
}
