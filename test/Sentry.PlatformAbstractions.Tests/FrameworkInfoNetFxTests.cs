#if NETFX
using System;
using NUnit.Framework;

namespace Sentry.PlatformAbstractions.Tests
{
    public class FrameworkInfoNetFxTests
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
        public void GetInstallations_NotEmpty()
        {
            var allInstallations = FrameworkInfo.GetInstallations();
            Assert.IsNotEmpty(allInstallations);
        }

        [Test]
        public void GetInstallations_AllReleasesAreMappedToVersion()
        {
            var allInstallations = FrameworkInfo.GetInstallations();
            foreach (var installation in allInstallations)
            {
                if (installation.Release != null)
                {
                    Assert.NotNull(installation.Version,
                        $"Release {installation.Release} has no version mapped");
                }
            }
        }
    }
}
#endif
