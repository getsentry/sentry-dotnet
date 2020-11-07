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
            //Arrange
            FrameworkInstallation frameworkInstallation = null;
            //Act
            var version = frameworkInstallation.GetVersionNumber();

            //Assert
            Assert.Null(version);
        }

        [Fact]
        public void GetVersionNumber_NullShortVersionAndNullVersion_NullVersion()
        {
            //Arrange
            var frameworkInstallation = new FrameworkInstallation();

            //Act
            var version = frameworkInstallation.GetVersionNumber();

            //Assert
            Assert.Null(version);
        }

        [Fact]
        public void GetVersionNumber_ValidShortVersion_ShortVersion()
        {
            //Arrange
            var frameworkInstallation = new FrameworkInstallation();
            var expectedShortVersion = "v1.2.3";
            frameworkInstallation.ShortName = expectedShortVersion;

            //Act
            var version = frameworkInstallation.GetVersionNumber();

            //Assert
            Assert.Equal(expectedShortVersion, version);
        }


        [Fact]
        public void GetVersionNumber_ValidVersionAndNullShortVersion_NullVersion()
        {
            //Arrange
            var frameworkInstallation = new FrameworkInstallation();
            frameworkInstallation.Version = new Version("1.2");

            //Act
            var version = frameworkInstallation.GetVersionNumber();

            //Assert
            Assert.Equal("v1.2", version);
        }

        [Fact]
        public void GetVersionNumber_ValidMinorMajorVersionAndNullShortVersion_NullVersion()
        {
            //Arrange
            var frameworkInstallation = new FrameworkInstallation();
            frameworkInstallation.Version = new Version(1,2);

            //Act
            var version = frameworkInstallation.GetVersionNumber();

            //Assert
            Assert.Equal("v1.2", version);
        }
    }
}
