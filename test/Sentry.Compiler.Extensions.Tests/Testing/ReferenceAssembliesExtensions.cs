using Microsoft.CodeAnalysis.Testing;

namespace Sentry.Compiler.Extensions.Tests.Testing;

internal static class ReferenceAssembliesExtensions
{
    extension(ReferenceAssemblies)
    {
        internal static ReferenceAssemblies Current
        {
            get
            {
#if NET11_0
                // Microsoft.CodeAnalysis.Testing does not define ReferenceAssemblies.Net.Net110
                // yet - the newest it ships (1.1.4) is Net100. Declaring one ourselves needs a
                // NuGet.Packaging reference plus a hard-coded Microsoft.NETCore.App.Ref preview
                // version to bump every preview, so compile the analyzer test snippets against
                // the .NET 10 reference assemblies instead. The analyzers under test don't use
                // any .NET 11 API, so this only affects the snippets, not what we're asserting.
                // TODO: switch to ReferenceAssemblies.Net.Net110 once the package ships it.
                return ReferenceAssemblies.Net.Net100;
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
