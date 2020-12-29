#if !NETFX
using Sentry.PlatformAbstractions;
using Xunit;

namespace Sentry.Tests.PlatformAbstractions
{
    public class FrameworkInfoNetStandardTests
    {
        [Fact]
        public void GetLatest_Returns_Null()
        {
            var latest = FrameworkInfo.GetLatest(4);
            Assert.Null(latest);
        }

        [Fact]
        public void GetInstallations_Returns_Empty()
        {
            var allInstallations = FrameworkInfo.GetInstallations();
            Assert.Empty(allInstallations);
        }
    }
}
#endif
