#if NETFX
using System;
using NUnit.Framework;

namespace Sentry.PlatformAbstractions.Tests
{
    public class FrameworkInfoTests
    {
        [SetUp]
        public void TestSetUp()
        {
            if (RuntimeInfo.GetRuntime().IsMono())
            {
                Assert.Ignore("Test only relevant under .NET Framework");
            }
        }

        [Test]
        public void GetLatest_NotNull()
        {
            var latest = FrameworkInfo.GetLatest(Environment.Version.Major);
            Assert.NotNull(latest);
        }

        [Test]
        public void GetInstalledVersions_NotEmpty()
        {
            var allInstallations = FrameworkInfo.GetInstalledVersions();
            Assert.IsNotEmpty(allInstallations);
        }

        [Test]
        public void GetInstalledVersions_AllReleasesAreMappedToVersion()
        {
            var allInstallations = FrameworkInfo.GetInstalledVersions();
            foreach (var installation in allInstallations)
            {
                if (installation.Release != null)
                {
                    Assert.NotNull(installation.Version);
                }
            }
        }
    }
}
#endif
