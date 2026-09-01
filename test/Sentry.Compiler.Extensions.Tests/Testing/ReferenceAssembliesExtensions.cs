using Microsoft.CodeAnalysis.Testing;

namespace Sentry.Compiler.Extensions.Tests.Testing;

internal static class ReferenceAssembliesExtensions
{
#if NET11_0
    // Microsoft.CodeAnalysis.Testing has no ReferenceAssemblies.Net.Net110 yet - the newest it
    // ships (1.1.4) is Net100 - so declare it here the same way the package declares its own
    // entries. Falling back to Net100 does not work: the snippets reference a net11.0-built
    // Sentry.dll, which pulls System.Runtime 11.0.0.0 and fails with CS1705 against .NET 10
    // reference assemblies. Keep the version in step with global.json, and drop this once the
    // package catches up.
    private static readonly ReferenceAssemblies Net110 = new(
        "net11.0",
        new PackageIdentity("Microsoft.NETCore.App.Ref", "11.0.0-preview.7.26381.103"),
        Path.Combine("ref", "net11.0"));
#endif

    extension(ReferenceAssemblies)
    {
        internal static ReferenceAssemblies Current
        {
            get
            {
#if NET11_0
                return Net110;
#elif NET10_0
                return ReferenceAssemblies.Net.Net100;
#else
#warning Target Framework not implemented.
                throw new UnreachableException();
#endif
            }
        }
    }
}
