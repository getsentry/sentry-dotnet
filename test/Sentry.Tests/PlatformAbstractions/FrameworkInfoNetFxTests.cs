#if NETFX
using Sentry.PlatformAbstractions;
using Xunit;
using System;

namespace Sentry.Tests.PlatformAbstractions
{
    public class FrameworkInfoNetFxTests
    {
        [SkippableFact]
        public void GetLatest_NotNull()
        {
            Skip.If(RuntimeInfo.GetRuntime().IsMono());
            var latest = FrameworkInfo.GetLatest(Environment.Version.Major);
            Assert.NotNull(latest);
        }

        [SkippableFact]
        public void GetInstallations_NotEmpty()
        {
            Skip.If(RuntimeInfo.GetRuntime().IsMono());
            var allInstallations = FrameworkInfo.GetInstallations();
            Assert.NotEmpty(allInstallations);
        }

        [SkippableFact]
        public void GetInstallations_AllReleasesAreMappedToVersion()
        {
            Skip.If(RuntimeInfo.GetRuntime().IsMono());
            var allInstallations = FrameworkInfo.GetInstallations();
            foreach (var installation in allInstallations)
            {
                if (installation.Release != null)
                {
                    // Release has no version mapped
                    Assert.NotNull(installation.Version);
                }
            }
        }
    }
}
#endif
