using System.Reflection;
using Sentry.Internal;
#if NET461
using Sentry.PlatformAbstractions;
#endif
using Sentry.Reflection;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class ReleaseLocatorTests
    {
        [Fact]
        public void ResolveFromEnvironment_WithEnvironmentVariable_VersionOfEnvironmentVariable()
        {
            const string expectedVersion = "the version";
            EnvironmentVariableGuard.WithVariable(
                Sentry.Internal.Constants.ReleaseEnvironmentVariable,
                expectedVersion,
                () =>
                {
                    Assert.Equal(expectedVersion, ReleaseLocator.LocateFromEnvironment());
                });
        }

        [Fact]
        public void ResolveFromEnvironment_WithoutEnvironmentVariable_VersionOfEntryAssembly()
        {
            var ass = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            EnvironmentVariableGuard.WithVariable(
                Sentry.Internal.Constants.ReleaseEnvironmentVariable,
                null,
                () =>
                {
                    Assert.Equal(
                        $"{ass!.GetName().Name}@{ass!.GetNameAndVersion().Version}",
                        ReleaseLocator.LocateFromEnvironment());
                });
        }
    }
}
