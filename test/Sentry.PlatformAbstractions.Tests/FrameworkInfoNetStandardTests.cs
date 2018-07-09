#if !NETFX
using NUnit.Framework;

namespace Sentry.PlatformAbstractions.Tests
{
    public class FrameworkInfoNetStandardTests
    {
        [Test]
        public void GetLatest_Returns_Null()
        {
            var latest = FrameworkInfo.GetLatest(4);
            Assert.Null(latest);
        }

        [Test]
        public void GetInstallations_Returns_Empty()
        {
            var allInstallations = FrameworkInfo.GetInstallations();
            Assert.IsEmpty(allInstallations);
        }
    }
}
#endif
