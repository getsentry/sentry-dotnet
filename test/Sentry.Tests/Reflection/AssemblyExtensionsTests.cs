using System;
using System.Reflection;
using Sentry.Reflection;
using Sentry.Tests.Helpers.Reflection;
using Xunit;

namespace Sentry.Tests.Reflection
{
    public class AssemblyExtensionsTests
    {
        [Fact]
        public void GetNameAndVersion_NoInformationalAttribute_ReturnsAssemblyNameData()
        {
            var asmName = new AssemblyName
            {
                Name = Guid.NewGuid().ToString(),
                Version = new Version(1, 2, 3, 4)
            };

            var actual = AssemblyCreationHelper.CreateAssembly(asmName).GetNameAndVersion();

            Assert.Equal(asmName.Name, actual.Name);
            Assert.Equal(asmName.Version.ToString(), actual.Version);
        }

        [Fact]
        public void GetNameAndVersion_WithInformationalAttribute_ReturnsAssemblyInformationalVersion()
        {
            const string expectedVersion = "1.0.0-preview2";
            var asmName = new AssemblyName
            {
                Name = Guid.NewGuid().ToString(),
                Version = new Version(1, 2, 3, 4)
            };

            var actual = AssemblyCreationHelper.CreateWithInformationalVersion(expectedVersion, asmName)
                .GetNameAndVersion();

            Assert.Equal(asmName.Name, actual.Name);
            Assert.Equal(expectedVersion, actual.Version);
        }
    }
}
