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
        public void GetCurrent_WithEnvironmentVariable_VersionOfEnvironmentVariable()
        {
            const string expectedVersion = "the version";
            EnvironmentVariableGuard.WithVariable(
                Sentry.Internal.Constants.ReleaseEnvironmentVariable,
                expectedVersion,
                () =>
                {
                    Assert.Equal(expectedVersion, ReleaseLocator.GetCurrent());
                });
        }


#if NET461
        [SkippableFact]
#else
        [Fact]
#endif
        public void GetCurrent_WithoutEnvironmentVariable_VersionOfEntryAssembly()
        {
#if NET461
            Skip.If(Runtime.Current.IsMono(), "GetEntryAssembly returning null on Mono.");
#endif
            var ass = Assembly.GetEntryAssembly();

            EnvironmentVariableGuard.WithVariable(
                Sentry.Internal.Constants.ReleaseEnvironmentVariable,
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
