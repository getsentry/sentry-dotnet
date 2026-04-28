using Microsoft.CodeAnalysis;

namespace Sentry.Compiler.Extensions;

internal static class SymbolDisplayFormats
{
    internal static SymbolDisplayFormat FullNameFormat { get; } = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
    );
}
