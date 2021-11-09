using System.Reflection;
using Sentry.Testing;

namespace Sentry.Tests.Internals;

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

#if NET461
        [SkippableFact]
#else
    [Fact]
#endif
    public void ResolveFromEnvironment_WithoutEnvironmentVariable_VersionOfEntryAssembly()
    {
        var ass = Assembly.GetEntryAssembly();
#if NET461
            Skip.If(ass == null, "GetEntryAssembly can return null on net461. eg on Mono or in certain test runners.");
#endif

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
