using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Sentry.Tests.Helpers.Reflection
{
    internal class AssemblyCreationHelper
    {
        public static Assembly CreateAssemblyWithDsnAttribute(string dsn)
        {
            var assemblyBuilder = (AssemblyBuilder)CreateAssemblyWithoutDsnAttribute();

            var dsnAttrib = typeof(DsnAttribute);
            var ctor = dsnAttrib.GetConstructor(new[] { typeof(string) });
            var attributeBuilder = new CustomAttributeBuilder(ctor, new object[] { dsn });
            assemblyBuilder.SetCustomAttribute(attributeBuilder);

            return assemblyBuilder;
        }

        public static Assembly CreateAssemblyWithoutDsnAttribute()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(Guid.NewGuid().ToString()),
                AssemblyBuilderAccess.RunAndCollect);

            return assemblyBuilder;
        }
    }
}
