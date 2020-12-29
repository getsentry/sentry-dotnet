using System.Reflection;
using Sentry.Internal;
using Sentry.PlatformAbstractions;
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
            Skip.If(Runtime.Current.IsMono(), "GetEntryAssembly returning null on Mono.");

            var ass = Assembly.GetEntryAssembly();

            EnvironmentVariableGuard.WithVariable(
                Constants.ReleaseEnvironmentVariable,
                null,
                () =>
                {
                    Assert.Equal(
                        $"{ass!.GetName().Name}@{ass!.GetNameAndVersion().Version}",
                        ReleaseLocator.GetCurrent()
                    );
                });
        }
    }
}
