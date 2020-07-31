using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Sentry.Tests.Helpers.Reflection
{
    internal static class AssemblyCreationHelper
    {
        public static Assembly CreateAssemblyWithDsnAttribute(string dsn)
        {
            var assemblyBuilder = (AssemblyBuilder)CreateAssembly();

            var dsnAttribute = typeof(DsnAttribute);
            var ctor = dsnAttribute.GetConstructor(new[] { typeof(string) });
            var attributeBuilder = new CustomAttributeBuilder(ctor, new object[] { dsn });
            assemblyBuilder.SetCustomAttribute(attributeBuilder);

            return assemblyBuilder;
        }

        public static Assembly CreateAssembly(AssemblyName asmName = null)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                asmName ?? new AssemblyName(Guid.NewGuid().ToString()),
                AssemblyBuilderAccess.RunAndCollect);

            return assemblyBuilder;
        }

        public static Assembly CreateWithInformationalVersion(string version, AssemblyName asmName = null)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                asmName ?? new AssemblyName(Guid.NewGuid().ToString()),
                AssemblyBuilderAccess.RunAndCollect);

            var infoAttribute = typeof(AssemblyInformationalVersionAttribute);
            var ctor = infoAttribute.GetConstructor(new[] { typeof(string) });
            var attributeBuilder = new CustomAttributeBuilder(ctor, new object[] { version });
            assemblyBuilder.SetCustomAttribute(attributeBuilder);

            return assemblyBuilder;
        }
    }
}
