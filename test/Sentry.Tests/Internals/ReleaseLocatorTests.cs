using System.Reflection;
using Sentry.Internal;
using Sentry.Reflection;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class ReleaseLocatorTests
    {
        [Fact]
        public void GetCurrent_WithEnvironmentVariable_VersionOfEnvironmentVariable()
        {
            const string expectedVersion = "the version";
            EnvironmentVariableGuard.WithVariable(
                Constants.ReleaseEnvironmentVariable,
                expectedVersion,
                () =>
                {
                    Assert.Equal(expectedVersion, ReleaseLocator.GetCurrent());
                });
        }

        [Fact]
        public void GetCurrent_WithoutEnvironmentVariable_VersionOfEntryAssembly()
        {
            EnvironmentVariableGuard.WithVariable(
                Constants.ReleaseEnvironmentVariable,
                null,
                () =>
                {
                    Assert.Equal(Assembly.GetEntryAssembly()?.GetNameAndVersion().Version, ReleaseLocator.GetCurrent());
                });
        }
    }
}
