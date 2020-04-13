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

            var dsnAttrib = typeof(DsnAttribute);
            var ctor = dsnAttrib.GetConstructor(new[] { typeof(string) });
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

            var infoAttrib = typeof(AssemblyInformationalVersionAttribute);
            var ctor = infoAttrib.GetConstructor(new[] { typeof(string) });
            var attributeBuilder = new CustomAttributeBuilder(ctor, new object[] { version });
            assemblyBuilder.SetCustomAttribute(attributeBuilder);

            return assemblyBuilder;
        }
    }
}
