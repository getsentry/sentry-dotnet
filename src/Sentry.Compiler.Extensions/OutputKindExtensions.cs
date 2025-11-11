using Microsoft.CodeAnalysis;

namespace Sentry.Compiler.Extensions;

internal static class OutputKindExtensions
{
    internal static bool IsExe(this OutputKind outputKind)
    {
        return outputKind switch
        {
            OutputKind.ConsoleApplication => true,
            OutputKind.WindowsApplication => true,
            OutputKind.DynamicallyLinkedLibrary => false,
            OutputKind.NetModule => false,
            OutputKind.WindowsRuntimeMetadata => false,
            OutputKind.WindowsRuntimeApplication => true,
            _ => false,
        };
    }
}
