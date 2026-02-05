using Microsoft.CodeAnalysis.Testing;

namespace Sentry.Compiler.Extensions.Tests;

internal static class TargetFramework
{
    internal static ReferenceAssemblies ReferenceAssemblies
    {
        get
        {
#if NET8_0
            return ReferenceAssemblies.Net.Net80;
#elif NET9_0
            return ReferenceAssemblies.Net.Net90;
#elif NET10_0
            return ReferenceAssemblies.Net.Net100;
#else
#warning Target Framework not implemented.
            throw new NotImplementedException();
#endif
        }
    }
}
