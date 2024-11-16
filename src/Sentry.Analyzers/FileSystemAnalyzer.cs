using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sentry.Analyzers;

/// <summary>
/// Analyzer that issues a warning if file system access is done outside IFileSystem wrapper implementations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FileSystemAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "SN0001";

    private const string Title = "Use IFileSystem wrapper";
    private const string MessageFormat = "Route file system access through IFileSystem wrapper";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category,
        DiagnosticSeverity.Warning, isEnabledByDefault: true);

    /// <summary>
    /// Returns a set of descriptors for the diagnostics that this analyzer is capable of producing.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <summary>
    /// Called once at session start to register actions in the analysis context.
    /// </summary>
    /// <param name="context"></param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(
            AnalyzeNode,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var memberSymbol = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol;

        if (memberSymbol is not { ContainingType.Name: ("Directory" or "File" or "FileInfo" or "DirectoryInfo") })
        {
            return;
        }

        if (ContainingClassImplementsInterface(context, "Sentry.Internal.IFileSystem"))
        {
            // Allow direct file system access in IFileSystem implementations
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
    }

    private static bool ContainingClassImplementsInterface(SyntaxNodeAnalysisContext context, string interfaceName)
    {
        if (!TryFindClassDeclarationForNode(context, out var classDeclarationSyntax))
        {
            return false;
        }

        var namedTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax, context.CancellationToken);
        if (namedTypeSymbol == null)
        {
            return false;
        }

        return namedTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == interfaceName);
    }

    private static bool TryFindClassDeclarationForNode(
        SyntaxNodeAnalysisContext context,
        [NotNullWhen(true)] out ClassDeclarationSyntax? classDeclarationSyntax)
    {
        var parentClassNode = context.Node;

        // Traverse parent nodes until class declaration is found
        while (parentClassNode is not ClassDeclarationSyntax && parentClassNode != null)
        {
            parentClassNode = parentClassNode.Parent;
        }

        if (parentClassNode is not ClassDeclarationSyntax classDeclaration)
        {
            classDeclarationSyntax = null;
            return false;
        }

        classDeclarationSyntax = classDeclaration;
        return true;
    }
}
