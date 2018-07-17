using Sentry.Internal;
using Sentry.Tests.Helpers;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class EnvironmentLocatorTests
    {
        [Fact]
        public void GetCurrent_WithEnvironmentVariable_ReturnsEnvironmentVariableValue()
        {
            const string expectedVersion = "the environment name";
            EnvironmentVariableGuard.WithVariable(
                Constants.EnvironmentEnvironmentVariable,
                expectedVersion,
                () =>
                {
                    Assert.Equal(expectedVersion, EnvironmentLocator.GetCurrent());
                });
        }

        [Fact]
        public void GetCurrent_WithoutEnvironmentVariable_ReturnsNull()
        {
            EnvironmentVariableGuard.WithVariable(
                Constants.ReleaseEnvironmentVariable,
                null,
                () =>
                {
                    Assert.Null(EnvironmentLocator.GetCurrent());
                });
        }
    }
}
