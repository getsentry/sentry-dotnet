using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sentry.Tests.Helpers.Reflection
{
    internal static class AssemblyExtensions
    {
        public static IEnumerable<Type> GetTypes(this Assembly asm, string namespacePrefix)
        {
            return asm
                .GetTypes()
                .Where(t => t.Namespace.StartsWith(namespacePrefix));
        }
    }
}
