using System;
using System.Reflection;
using System.Reflection.Emit;
using Sentry.Reflection;
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

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName,
                AssemblyBuilderAccess.RunAndCollect);

            var actual = assemblyBuilder.GetNameAndVersion();
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

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName,
                AssemblyBuilderAccess.RunAndCollect);

            var infoAttrib = typeof(AssemblyInformationalVersionAttribute);
            var ctor = infoAttrib.GetConstructor(new[] { typeof(string) });
            var attributeBuilder = new CustomAttributeBuilder(ctor, new object[] { expectedVersion });
            assemblyBuilder.SetCustomAttribute(attributeBuilder);


            var actual = assemblyBuilder.GetNameAndVersion();
            Assert.Equal(asmName.Name, actual.Name);
            Assert.Equal(expectedVersion, actual.Version);
        }
    }
}
