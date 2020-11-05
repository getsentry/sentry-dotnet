using System;
using Sentry.PlatformAbstractions;
using Xunit;

namespace Sentry.Tests.PlatformAbstractions
{
    public class FrameworkInstallationExtensionsTests
    {
        [Fact]
        public void GetVersionNumber_NullFrameworkInstallation_NullVersion()
        {
            FrameworkInstallation frameworkInstallation = null;
            //Arrange

            //Act
            var version = frameworkInstallation.GetVersionNumber();

            //Assert
            Assert.Null(version);
        }

        [Fact]
        public void GetVersionNumber_NullShortVersionAndNullVersion_NullVersion()
        {
            var frameworkInstallation = new FrameworkInstallation();
            //Arrange

            //Act
            var version = frameworkInstallation.GetVersionNumber();

            //Assert
            Assert.Null(version);
        }

        [Fact]
        public void GetVersionNumber_ValidShortVersion_ShortVersion()
        {
            var frameworkInstallation = new FrameworkInstallation();
            var expectedShortVersion = "v1.2.3";
            frameworkInstallation.ShortName = expectedShortVersion;
            //Arrange

            //Act
            var version = frameworkInstallation.GetVersionNumber();

            //Assert
            Assert.Equal(expectedShortVersion, version);
        }


        [Fact]
        public void GetVersionNumber_ValidVersionAndNullShortVersion_NullVersion()
        {
            var frameworkInstallation = new FrameworkInstallation();
            frameworkInstallation.Version = new Version("1.2");
            //Arrange

            //Act
            var version = frameworkInstallation.GetVersionNumber();

            //Assert
            Assert.Equal("1.2", version);
        }

        [Fact]
        public void GetVersionNumber_ValidMinorMajorVersionAndNullShortVersion_NullVersion()
        {
            var frameworkInstallation = new FrameworkInstallation();
            frameworkInstallation.Version = new Version(1,2);
            //Arrange

            //Act
            var version = frameworkInstallation.GetVersionNumber();

            //Assert
            Assert.Equal("1.2", version);
        }
    }
}
