using System;
using System.Reflection;
using Sentry.Internal;
using Sentry.Tests.Helpers.Reflection;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class ApplicationVersionLocatorTests
    {
        [Theory]
        [InlineData("5fd7a6cda8444965bade9ccfd3df9882")]
        [InlineData("1.0")]
        [InlineData("1.0 - 5fd7a6c")]
        [InlineData("1.0-beta")]
        [InlineData("2.1.0.0")]
        [InlineData("2.1.0.0-preview1")]
        public void GetCurrent_ValidVersion_ReturnsVersion(string expectedVersion)
        {
            var name = "dynamic-assembly";
            var asm = AssemblyCreationHelper.CreateWithInformationalVersion(expectedVersion, new AssemblyName(name));
            var actual = ApplicationVersionLocator.GetCurrent(asm);
            Assert.Equal($"{name}@{expectedVersion}", actual);
        }

        [Theory]
        [InlineData("")]
        public void GetCurrent_InvalidCases_ReturnsNull(string version)
        {
            var asm = AssemblyCreationHelper.CreateWithInformationalVersion(version);
            var actual = ApplicationVersionLocator.GetCurrent(asm);
            Assert.Null(actual);
        }

        [Fact]
        public void GetCurrent_NoAsmInformationalVersion_ReturnsAsmVersion()
        {
            const string expectedName = "foo";
            const string expectedVersion = "2.1.0.0";
            var asmName = new AssemblyName(expectedName)
            {
                Version = Version.Parse(expectedVersion)
            };

            var asm = AssemblyCreationHelper.CreateAssembly(asmName);
            var actual = ApplicationVersionLocator.GetCurrent(asm);

            Assert.Equal(expectedName + '@' + expectedVersion, actual);
        }
    }
}
